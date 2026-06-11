using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using VrcWebMap.Backend.Options;

namespace VrcWebMap.Backend.Services;

/// <summary>
/// Discord OAuth とユーザー情報取得を担当する transport adapter 用 client です。
/// </summary>
public sealed class DiscordApiClient(HttpClient http, IOptions<DiscordOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly DiscordOptions options = options.Value;

    public Uri CreateAuthorizationUri(string state)
    {
        var query = new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = options.ClientId,
            ["scope"] = "identify guilds.members.read",
            ["redirect_uri"] = options.RedirectUri,
            ["state"] = state,
            ["prompt"] = "consent"
        };

        var builder = new UriBuilder("https://discord.com/oauth2/authorize")
        {
            Query = string.Join("&", query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value ?? string.Empty)}"))
        };

        return builder.Uri;
    }

    public async Task<DiscordTokenResponse?> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = options.RedirectUri,
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret
            })
        };

        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<DiscordTokenResponse>(stream, JsonOptions, cancellationToken);
    }

    public async Task<DiscordCurrentUser?> GetCurrentUserAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateBearerRequest(HttpMethod.Get, "https://discord.com/api/v10/users/@me", accessToken);
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<DiscordCurrentUser>(stream, JsonOptions, cancellationToken);
    }

    public async Task<DiscordGuildMember?> GetRequiredGuildMemberAsync(string accessToken, CancellationToken cancellationToken)
    {
        var guildId = options.RequiredGuildId;
        using var request = CreateBearerRequest(HttpMethod.Get, $"https://discord.com/api/v10/users/@me/guilds/{Uri.EscapeDataString(guildId)}/member", accessToken);
        using var response = await http.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<DiscordGuildMember>(stream, JsonOptions, cancellationToken);
    }

    public async Task<bool> HasAdminRoleAsync(DiscordGuildMember member, CancellationToken cancellationToken)
    {
        if (member.Roles.Length == 0 ||
            string.IsNullOrWhiteSpace(options.BotToken) ||
            string.IsNullOrWhiteSpace(options.AdminRoleName))
        {
            return false;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://discord.com/api/v10/guilds/{Uri.EscapeDataString(options.RequiredGuildId)}/roles");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bot", options.BotToken);

        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var roles = await JsonSerializer.DeserializeAsync<DiscordGuildRole[]>(stream, JsonOptions, cancellationToken) ?? [];
        var adminRoleIds = roles
            .Where(role => string.Equals(role.Name, options.AdminRoleName, StringComparison.Ordinal))
            .Select(role => role.Id)
            .ToHashSet(StringComparer.Ordinal);

        return member.Roles.Any(adminRoleIds.Contains);
    }

    private static HttpRequestMessage CreateBearerRequest(HttpMethod method, string uri, string accessToken)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }
}

public sealed record DiscordTokenResponse(
    [property: JsonPropertyName("access_token")]
    string AccessToken,
    [property: JsonPropertyName("token_type")]
    string TokenType,
    [property: JsonPropertyName("expires_in")]
    int ExpiresIn,
    [property: JsonPropertyName("refresh_token")]
    string? RefreshToken,
    [property: JsonPropertyName("scope")]
    string Scope);

public sealed record DiscordCurrentUser(
    string Id,
    string Username,
    [property: JsonPropertyName("global_name")]
    string? GlobalName,
    string? Avatar);

public sealed record DiscordGuildMember(
    string[] Roles);

public sealed record DiscordGuildRole(
    string Id,
    string Name);
