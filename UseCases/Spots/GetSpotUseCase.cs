using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;

namespace VrcWebMap.Backend.UseCases.Spots;

[KawaUseCase(
    "spots.get",
    Summary = "Get spot",
    Description = "指定されたスポットを返します。",
    Version = "v1",
    Tags = new[] { "Spot Management" })]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "スポットが見つかりません。")]
/// <summary>
/// 指定されたスポットを取得するユースケースです。
/// </summary>
public sealed class GetSpotUseCase(ISpotRepository spots)
    : IUseCase<GetSpot.Request, GetSpot.Response>
{
    /// <summary>
    /// 指定された ID のスポットを取得します。
    /// </summary>
    /// <param name="request">取得対象のスポット ID を含むリクエストです。</param>
    /// <param name="cancellationToken">キャンセル通知です。</param>
    /// <returns>取得したスポット、または未検出エラーを返します。</returns>
    public Task<KawaResult<GetSpot.Response>> ExecuteAsync(
        GetSpot.Request request,
        CancellationToken cancellationToken = default)
    {
        if (!spots.TryGet(request.Id, out var spot))
        {
            var error = new KawaError(KawaErrorKind.NotFound, "スポットが見つかりません。");
            return Task.FromResult(KawaResult<GetSpot.Response>.Failure(error));
        }

        var response = new GetSpot.Response(
            spot,
            spots.ListWorlds().Where(world => world.SpotId == spot.Id).ToArray(),
            spots.ListRestaurants().Where(restaurant => restaurant.SpotId == spot.Id).ToArray(),
            spots.ListComments().Where(comment => comment.SpotId == spot.Id).ToArray());
        return Task.FromResult(KawaResult<GetSpot.Response>.Success(response));
    }
}
