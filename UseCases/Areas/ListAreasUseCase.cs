using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Areas;
using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.UseCases.Areas;

[KawaUseCase(
    "areas.list",
    Summary = "List areas",
    Description = "Spot に紐づけられるエリア定義一覧を返します。",
    Version = "v1",
    Tags = new[] { "Area Definitions" })]
/// <summary>
/// エリア定義一覧を取得するユースケースです。
/// </summary>
public sealed class ListAreasUseCase
    : IUseCase<ListAreas.Request, ListAreas.Response>
{
    /// <summary>
    /// 現在サポートするエリア定義一覧を取得します。
    /// </summary>
    /// <param name="request">一覧取得リクエストです。</param>
    /// <param name="cancellationToken">キャンセル通知です。</param>
    /// <returns>エリア定義一覧を返します。</returns>
    public Task<KawaResult<ListAreas.Response>> ExecuteAsync(
        ListAreas.Request request,
        CancellationToken cancellationToken = default)
    {
        var response = new ListAreas.Response(AreaDefinitions.All);
        return Task.FromResult(KawaResult<ListAreas.Response>.Success(response));
    }
}
