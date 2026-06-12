using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.PlaceInfos;

public static class UpdatePlaceInfo
{
    public sealed record Request(
        Guid Id,
        string ActorUserId,
        bool ActorIsAdmin,
        string Name,
        string Address,
        string BusinessInformation);

    public sealed record Response(PlaceInfo PlaceInfo);
}
