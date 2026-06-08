using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;

namespace VrcWebMap.Backend.UseCases.Spots;

[KawaUseCase(
    "spots.list",
    Summary = "List spots",
    Description = "管理対象のスポット一覧を返します。",
    Version = "v1",
    Tags = new[] { "Spot Management" })]
/// <summary>
/// スポット一覧を取得するユースケースです。
/// </summary>
public sealed class ListSpotsUseCase(ISpotRepository spots)
    : IUseCase<ListSpots.Request, ListSpots.Response>
{
    /// <summary>
    /// 管理対象のスポット一覧を取得します。
    /// </summary>
    /// <param name="request">一覧取得リクエストです。</param>
    /// <param name="cancellationToken">キャンセル通知です。</param>
    /// <returns>スポット一覧を返します。</returns>
    public Task<KawaResult<ListSpots.Response>> ExecuteAsync(
        ListSpots.Request request,
        CancellationToken cancellationToken = default)
    {
        var response = new ListSpots.Response(spots.List());
        return Task.FromResult(KawaResult<ListSpots.Response>.Success(response));
    }
}
