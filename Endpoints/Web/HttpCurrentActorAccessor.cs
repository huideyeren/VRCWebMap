using System.Security.Claims;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Endpoints.Web;

/// <summary>
/// Cookie sessionの不変IDからDBの最新ユーザー状態を解決するWeb transport adapterです。
/// </summary>
public sealed class HttpCurrentActorAccessor(
    IHttpContextAccessor httpContextAccessor,
    IDiscordUserRepository users)
    : ICurrentActorAccessor
{
    public CurrentActor? GetCurrent()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var discordUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(discordUserId) ||
            !users.TryGetByDiscordUserId(discordUserId, out var user))
        {
            return null;
        }

        return new CurrentActor(
            user.DiscordUserId,
            user.IsAdmin,
            !string.IsNullOrWhiteSpace(user.VRChatDisplayName));
    }
}
