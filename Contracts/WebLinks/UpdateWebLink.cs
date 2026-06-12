using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.WebLinks;

public static class UpdateWebLink
{
    public sealed record Request(
        Guid Id,
        string ActorUserId,
        bool ActorIsAdmin,
        string SiteName,
        Uri Url);

    public sealed record Response(WebLink WebLink);
}
