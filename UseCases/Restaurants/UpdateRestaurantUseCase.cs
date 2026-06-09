using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Restaurants;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.UseCases.Restaurants;

[KawaUseCase("restaurants.update", Summary = "Update restaurant", Version = "v1", Tags = new[] { "Restaurants" })]
public sealed class UpdateRestaurantUseCase(ISpotRepository spots)
    : IUseCase<UpdateRestaurant.Request, UpdateRestaurant.Response>
{
    public Task<KawaResult<UpdateRestaurant.Response>> ExecuteAsync(UpdateRestaurant.Request request, CancellationToken cancellationToken = default)
    {
        if (!spots.TryGetRestaurant(request.Id, out var existing))
        {
            return Task.FromResult(KawaResult<UpdateRestaurant.Response>.Failure(new KawaError(KawaErrorKind.NotFound, "飲食店情報が見つかりません。")));
        }

        if (!SpotAuthorization.CanMutate(existing.RegisteredByUserId, request.ActorUserId, request.ActorIsAdmin))
        {
            return Task.FromResult(KawaResult<UpdateRestaurant.Response>.Failure(new KawaError(KawaErrorKind.Forbidden, "飲食店情報を変更する権限がありません。")));
        }

        var restaurant = new Restaurant(
            existing.Id,
            existing.SpotId,
            existing.RegisteredByUserId,
            request.Name.Trim(),
            request.Address.Trim(),
            request.Url,
            request.GurunaviUrl,
            request.TabelogUrl,
            request.RettyUrl,
            request.XUrl,
            request.InstagramUrl,
            request.OpenTime,
            request.CloseTime,
            request.ClosedOn.Trim());

        spots.UpsertRestaurant(restaurant);
        return Task.FromResult(KawaResult<UpdateRestaurant.Response>.Success(new UpdateRestaurant.Response(restaurant)));
    }
}
