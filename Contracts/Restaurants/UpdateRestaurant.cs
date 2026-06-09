using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.Restaurants;

public static class UpdateRestaurant
{
    public sealed record Request(
        Guid Id,
        string ActorUserId,
        bool ActorIsAdmin,
        string Name,
        string Address,
        Uri? Url,
        Uri? GurunaviUrl,
        Uri? TabelogUrl,
        Uri? RettyUrl,
        Uri? XUrl,
        Uri? InstagramUrl,
        TimeOnly OpenTime,
        TimeOnly CloseTime,
        string ClosedOn);

    public sealed record Response(Restaurant Restaurant);
}
