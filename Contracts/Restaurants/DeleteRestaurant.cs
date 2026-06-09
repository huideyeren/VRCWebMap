namespace VrcWebMap.Backend.Contracts.Restaurants;

public static class DeleteRestaurant
{
    public sealed record Request(Guid Id, string ActorUserId, bool ActorIsAdmin);

    public sealed record Response(Guid Id);
}
