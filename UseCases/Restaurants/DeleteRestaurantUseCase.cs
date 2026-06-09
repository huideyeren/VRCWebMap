using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Restaurants;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.UseCases.Restaurants;

[KawaUseCase("restaurants.delete", Summary = "Delete restaurant", Version = "v1", Tags = new[] { "Restaurants" })]
public sealed class DeleteRestaurantUseCase(ISpotRepository spots)
    : IUseCase<DeleteRestaurant.Request, DeleteRestaurant.Response>
{
    public Task<KawaResult<DeleteRestaurant.Response>> ExecuteAsync(DeleteRestaurant.Request request, CancellationToken cancellationToken = default)
    {
        if (!spots.TryGetRestaurant(request.Id, out var existing))
        {
            return Task.FromResult(KawaResult<DeleteRestaurant.Response>.Failure(new KawaError(KawaErrorKind.NotFound, "飲食店情報が見つかりません。")));
        }

        if (!SpotAuthorization.CanMutate(existing.RegisteredByUserId, request.ActorUserId, request.ActorIsAdmin))
        {
            return Task.FromResult(KawaResult<DeleteRestaurant.Response>.Failure(new KawaError(KawaErrorKind.Forbidden, "飲食店情報を削除する権限がありません。")));
        }

        spots.DeleteRestaurant(request.Id);
        return Task.FromResult(KawaResult<DeleteRestaurant.Response>.Success(new DeleteRestaurant.Response(request.Id)));
    }
}
