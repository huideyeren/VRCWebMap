using System.Security.Claims;
using System.Security.Cryptography;
using Kawa.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using VrcWebMap.Backend.Contracts.Users;
using VrcWebMap.Backend.Options;
using VrcWebMap.Backend.Services;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Endpoints.Web;

/// <summary>
/// Discord OAuth を使う認証 endpoint です。
/// </summary>
public static class DiscordAuthEndpoints
{
    private const string DiscordOAuthStateCookieName = "vrcwebmap.discord_oauth_state";

    public static IEndpointRouteBuilder MapDiscordAuth(this IEndpointRouteBuilder endpoints)
    {
        Delegate logoutHandler = (HttpContext httpContext) => LogoutAsync(httpContext);

        endpoints.MapGet("/auth/discord/login", LoginAsync).WithName("DiscordLogin");
        endpoints.MapGet("/auth/discord/callback", CallbackAsync).WithName("DiscordCallback");
        endpoints.MapPost("/auth/logout", logoutHandler).WithName("Logout");
        endpoints.MapGet("/auth/me", Me).WithName("CurrentUser");

        return endpoints;
    }

    private static IResult LoginAsync(
        HttpContext httpContext,
        DiscordApiClient discord,
        IOptions<DiscordOptions> options)
    {
        if (!IsConfigured(options.Value))
        {
            return Results.Problem("Discord OAuth settings are not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var state = CreateState();
        httpContext.Response.Cookies.Append(
            DiscordOAuthStateCookieName,
            state,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(10)
            });

        return Results.Redirect(discord.CreateAuthorizationUri(state).ToString());
    }

    private static async Task<IResult> CallbackAsync(
        HttpContext httpContext,
        string? code,
        string? state,
        DiscordApiClient discord,
        IOptions<DiscordOptions> options,
        IUseCase<RegisterDiscordUser.Request, RegisterDiscordUser.Response> registerUser,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured(options.Value))
        {
            return Results.Problem("Discord OAuth settings are not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(state) ||
            !httpContext.Request.Cookies.TryGetValue(DiscordOAuthStateCookieName, out var expectedState) ||
            !CryptographicOperations.FixedTimeEquals(ConvertState(expectedState), ConvertState(state)))
        {
            return Results.BadRequest("Invalid Discord OAuth callback state.");
        }

        httpContext.Response.Cookies.Delete(DiscordOAuthStateCookieName);

        var token = await discord.ExchangeCodeAsync(code, cancellationToken);
        if (token is null)
        {
            return Results.Problem("Failed to exchange Discord OAuth code.", statusCode: StatusCodes.Status502BadGateway);
        }

        var currentUser = await discord.GetCurrentUserAsync(token.AccessToken, cancellationToken);
        if (currentUser is null)
        {
            return Results.Problem("Failed to fetch Discord user.", statusCode: StatusCodes.Status502BadGateway);
        }

        var guildMember = await discord.GetRequiredGuildMemberAsync(token.AccessToken, cancellationToken);
        var isGuildMember = guildMember is not null;
        var isAdmin = guildMember is not null && await discord.HasAdminRoleAsync(guildMember, cancellationToken);
        var result = await registerUser.ExecuteAsync(
            new RegisterDiscordUser.Request(
                currentUser.Id,
                currentUser.Username,
                currentUser.GlobalName,
                currentUser.Avatar,
                options.Value.RequiredGuildId,
                isGuildMember,
                isAdmin),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error?.Kind == KawaErrorKind.Forbidden
                ? Results.Forbid()
                : Results.BadRequest(result.Error?.Message);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.Value!.User.DiscordUserId),
            new(ClaimTypes.Name, result.Value.User.GlobalName ?? result.Value.User.Username),
            new("discord_user_id", result.Value.User.DiscordUserId),
            new("discord_username", result.Value.User.Username)
        };

        if (result.Value.User.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            });

        return Results.Redirect("/");
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok(new AuthSession.LogoutResponse(LoggedOut: true));
    }

    private static IResult Me(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new AuthSession.CurrentUserResponse(
            user.FindFirstValue("discord_user_id") ?? string.Empty,
            user.FindFirstValue("discord_username") ?? string.Empty,
            user.Identity.Name,
            user.IsInRole("Admin")));
    }

    private static bool IsConfigured(DiscordOptions options) =>
        !string.IsNullOrWhiteSpace(options.ClientId) &&
        !string.IsNullOrWhiteSpace(options.ClientSecret) &&
        !string.IsNullOrWhiteSpace(options.RedirectUri) &&
        !string.IsNullOrWhiteSpace(options.RequiredGuildId);

    private static string CreateState()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static byte[] ConvertState(string state)
    {
        try
        {
            return Convert.FromBase64String(state);
        }
        catch (FormatException)
        {
            return [];
        }
    }
}
