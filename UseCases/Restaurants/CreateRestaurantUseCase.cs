using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Restaurants;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.UseCases.Restaurants;

[KawaUseCase(
    "restaurants.create",
    Summary = "Create restaurant",
    Description = "指定されたスポットに飲食店情報を追加します。",
    Version = "v1",
    Tags = new[] { "Restaurants" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "飲食店情報の入力値が不正です。")]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "スポットが見つかりません。")]
/// <summary>
/// スポットに飲食店情報を追加するユースケースです。
/// </summary>
public sealed class CreateRestaurantUseCase(ISpotRepository spots)
    : IUseCase<CreateRestaurant.Request, CreateRestaurant.Response>
{
    /// <summary>
    /// 指定されたスポットに飲食店情報を追加します。
    /// </summary>
    public Task<KawaResult<CreateRestaurant.Response>> ExecuteAsync(
        CreateRestaurant.Request request,
        CancellationToken cancellationToken = default)
    {
        if (!spots.Exists(request.SpotId))
        {
            var error = new KawaError(KawaErrorKind.NotFound, "スポットが見つかりません。");
            return Task.FromResult(KawaResult<CreateRestaurant.Response>.Failure(error));
        }

        if (string.IsNullOrWhiteSpace(request.RegisteredByUserId) ||
            string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Address))
        {
            var error = new KawaError(KawaErrorKind.Validation, "登録者 ID、飲食店名、所在地は必須です。");
            return Task.FromResult(KawaResult<CreateRestaurant.Response>.Failure(error));
        }

        var restaurant = new Restaurant(
            Guid.NewGuid(),
            request.SpotId,
            request.RegisteredByUserId.Trim(),
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

        var response = new CreateRestaurant.Response(restaurant);
        return Task.FromResult(KawaResult<CreateRestaurant.Response>.Success(response));
    }
}
