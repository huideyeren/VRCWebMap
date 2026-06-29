using System.Security.Claims;
using System.Security.Cryptography;
using Kawa.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using VrcWebMap.Backend.Contracts.Users;
using VrcWebMap.Backend.Models;
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
        var environment = endpoints.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        endpoints.MapGet("/auth/discord/login", LoginAsync)
            .WithName("DiscordLogin")
            .WithTags("Authentication")
            .WithSummary("Start Discord OAuth login")
            .WithDescription("Discord OAuth の認可画面へ redirect します。Discord 設定が不足している場合は 503 ProblemDetails を返します。")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        endpoints.MapGet("/auth/discord/callback", CallbackAsync)
            .WithName("DiscordCallback")
            .WithTags("Authentication")
            .WithSummary("Handle Discord OAuth callback")
            .WithDescription("Discord OAuth code を検証し、対象 guild 参加を server-side に確認して cookie session を作成します。")
            .Produces(StatusCodes.Status302Found)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        endpoints.MapPost("/auth/logout", logoutHandler)
            .WithName("Logout")
            .WithTags("Authentication")
            .WithSummary("Logout current user")
            .WithDescription("現在の cookie session を破棄します。未ログインでも呼び出せます。")
            .Produces<AuthSession.LogoutResponse>(StatusCodes.Status200OK);
        endpoints.MapGet("/auth/me", Me)
            .WithName("CurrentUser")
            .WithTags("Authentication")
            .WithSummary("Get current user")
            .WithDescription("現在の cookie session に紐づく Discord ユーザー情報を返します。未ログインの場合は 401 を返します。")
            .Produces<AuthSession.CurrentUserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        if (environment.IsDevelopment())
        {
            endpoints.MapGet("/auth/dev/app", DevelopmentApp)
                .WithName("DevelopmentApp")
                .WithTags("Development Authentication")
                .WithSummary("Get development app links")
                .WithDescription("Development 環境で利用できる Swagger、ReDoc、OpenAPI JSON の URL を返します。Production では登録されません。")
                .Produces<AuthSession.DevelopmentAppResponse>(StatusCodes.Status200OK);
            endpoints.MapGet("/auth/dev/users", DevelopmentUsers)
                .WithName("DevelopmentUsers")
                .WithTags("Development Authentication")
                .WithSummary("List development sample users")
                .WithDescription("Discord OAuth を使えない Development 環境向けに、管理者と一般ユーザーのサンプルログイン情報を返します。Production では登録されません。")
                .Produces<AuthSession.DevelopmentUserResponse[]>(StatusCodes.Status200OK);
            endpoints.MapGet("/auth/dev/login/{userKind}", DevelopmentLoginAsync)
                .WithName("DevelopmentLogin")
                .WithTags("Development Authentication")
                .WithSummary("Login as a development sample user")
                .WithDescription("`admin` または `user` を指定して開発用サンプルユーザーとしてログインします。Production では登録されません。")
                .Produces(StatusCodes.Status302Found)
                .Produces(StatusCodes.Status404NotFound);
        }

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
        var result = await registerUser.ExecuteAsync(
            new RegisterDiscordUser.Request(
                currentUser.Id,
                currentUser.Username,
                currentUser.GlobalName,
                currentUser.Avatar,
                options.Value.RequiredGuildId,
                isGuildMember),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error?.Kind == KawaErrorKind.Forbidden
                ? Results.Forbid()
                : Results.BadRequest(result.Error?.Message);
        }

        await SignInAsync(httpContext, result.Value!.User);

        return Results.Redirect("/");
    }

    private static AuthSession.DevelopmentUserResponse[] DevelopmentUsers() =>
    [
        new(
            UserId: "dev-admin-user",
            Username: "dev-admin",
            DisplayName: "開発用マップ管理者",
            IsAdmin: true,
            LoginUrl: "/auth/dev/login/admin"),
        new(
            UserId: "dev-general-user",
            Username: "dev-user",
            DisplayName: "開発用一般ユーザー",
            IsAdmin: false,
            LoginUrl: "/auth/dev/login/user")
    ];

    private static AuthSession.DevelopmentAppResponse DevelopmentApp() =>
        new(
            IsDevelopment: true,
            SwaggerUrl: "/openapi/swagger",
            ReDocUrl: "/openapi/redoc",
            OpenApiUrl: "/openapi/v1.json");

    private static async Task<IResult> DevelopmentLoginAsync(
        HttpContext httpContext,
        string userKind,
        IOptions<DiscordOptions> options,
        IUseCase<RegisterDiscordUser.Request, RegisterDiscordUser.Response> registerUser,
        CancellationToken cancellationToken)
    {
        var sampleUser = userKind switch
        {
            "admin" => new DevelopmentSampleUser(
                DiscordUserId: "dev-admin-user",
                Username: "dev-admin",
                GlobalName: "開発用マップ管理者"),
            "user" => new DevelopmentSampleUser(
                DiscordUserId: "dev-general-user",
                Username: "dev-user",
                GlobalName: "開発用一般ユーザー"),
            _ => null
        };

        if (sampleUser is null)
        {
            return Results.NotFound();
        }

        var requiredGuildId = string.IsNullOrWhiteSpace(options.Value.RequiredGuildId)
            ? "development-guild"
            : options.Value.RequiredGuildId;
        var result = await registerUser.ExecuteAsync(
            new RegisterDiscordUser.Request(
                sampleUser.DiscordUserId,
                sampleUser.Username,
                sampleUser.GlobalName,
                AvatarHash: null,
                requiredGuildId,
                IsRequiredGuildMember: true),
            cancellationToken);

        if (result.IsFailure)
        {
            return Results.BadRequest(result.Error?.Message);
        }

        await SignInAsync(httpContext, result.Value!.User);
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

    private static async Task SignInAsync(HttpContext httpContext, DiscordUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.DiscordUserId),
            new(ClaimTypes.Name, user.GlobalName ?? user.Username),
            new("discord_user_id", user.DiscordUserId),
            new("discord_username", user.Username)
        };

        if (user.IsAdmin)
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

    private sealed record DevelopmentSampleUser(
        string DiscordUserId,
        string Username,
        string GlobalName);
}
