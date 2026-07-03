using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.Spots;

[KawaUseCase(
    "spots.delete",
    Summary = "Delete spot",
    Description = "指定されたスポット管理レコードを削除します。",
    Version = "v1",
    Tags = new[] { "Spot Management" })]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "スポットが見つかりません。")]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "スポットを削除する権限がありません。")]
[KawaErrorResponse(KawaErrorKind.Conflict, Description = "スポットに関連データがあるため削除できません。")]
/// <summary>
/// スポットを削除するユースケースです。
/// </summary>
public sealed class DeleteSpotUseCase(
    ISpotRepository spots,
    ICurrentActorAccessor currentActor)
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
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Task.FromResult(KawaResult<DeleteSpot.Response>.Failure(actorError));
        }

        if (!spots.TryGet(request.Id, out var spot))
        {
            var error = new KawaError(KawaErrorKind.NotFound, "スポットが見つかりません。");
            return Task.FromResult(KawaResult<DeleteSpot.Response>.Failure(error));
        }

        if (!SpotAuthorization.CanDelete(actor!.IsAdmin))
        {
            var error = new KawaError(KawaErrorKind.Forbidden, "スポットを削除する権限がありません。");
            return Task.FromResult(KawaResult<DeleteSpot.Response>.Failure(error));
        }

        if (HasRelatedData(request.Id))
        {
            var error = new KawaError(KawaErrorKind.Conflict, "スポットに関連データがあるため削除できません。先に関連データを削除してください。");
            return Task.FromResult(KawaResult<DeleteSpot.Response>.Failure(error));
        }

        spots.Delete(request.Id);

        var response = new DeleteSpot.Response(request.Id);
        return Task.FromResult(KawaResult<DeleteSpot.Response>.Success(response));
    }

    private bool HasRelatedData(Guid spotId) =>
        spots.ListWorlds().Any(world => world.SpotId == spotId) ||
        spots.ListPlaceInfos().Any(placeInfo => placeInfo.SpotId == spotId) ||
        spots.ListWebLinks().Any(webLink => webLink.SpotId == spotId) ||
        spots.ListComments().Any(comment => comment.SpotId == spotId);
}
