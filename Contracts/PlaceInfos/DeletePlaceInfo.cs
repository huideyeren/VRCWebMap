namespace VrcWebMap.Backend.Contracts.PlaceInfos;

public static class DeletePlaceInfo
{
    public sealed record Request(Guid Id, string ActorUserId, bool ActorIsAdmin);

    public sealed record Response(Guid Id);
}
