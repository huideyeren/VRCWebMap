using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;

namespace VrcWebMap.Backend.UseCases.Spots;

[KawaUseCase(
    "spots.delete",
    Summary = "Delete spot",
    Description = "指定されたスポット管理レコードを削除します。",
    Version = "v1",
    Tags = new[] { "Spot Management" })]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "スポットが見つかりません。")]
/// <summary>
/// スポットを削除するユースケースです。
/// </summary>
public sealed class DeleteSpotUseCase(ISpotRepository spots)
    : IUseCase<DeleteSpot.Request, DeleteSpot.Response>
{
    /// <summary>
    /// 指定された ID のスポットを削除します。
    /// </summary>
    /// <param name="request">削除対象のスポット ID を含むリクエストです。</param>
    /// <param name="cancellationToken">キャンセル通知です。</param>
    /// <returns>削除されたスポット ID、または未検出エラーを返します。</returns>
    public Task<KawaResult<DeleteSpot.Response>> ExecuteAsync(
        DeleteSpot.Request request,
        CancellationToken cancellationToken = default)
    {
        if (!spots.Delete(request.Id))
        {
            var error = new KawaError(KawaErrorKind.NotFound, "スポットが見つかりません。");
            return Task.FromResult(KawaResult<DeleteSpot.Response>.Failure(error));
        }

        var response = new DeleteSpot.Response(request.Id);
        return Task.FromResult(KawaResult<DeleteSpot.Response>.Success(response));
    }
}
