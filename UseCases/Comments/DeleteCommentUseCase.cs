using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Comments;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.Comments;

[KawaUseCase(
    "comments.delete",
    Summary = "Delete comment",
    Description = "スポットに紐づくコメントを削除します。削除は管理者のみ実行できます。",
    Version = "v1",
    Tags = new[] { "Comments" })]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "コメントが見つかりません。")]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "コメントを削除する権限がありません。")]
public sealed class DeleteCommentUseCase(
    ISpotRepository spots,
    ICurrentActorAccessor currentActor)
    : IUseCase<DeleteComment.Request, DeleteComment.Response>
{
    public Task<KawaResult<DeleteComment.Response>> ExecuteAsync(DeleteComment.Request request, CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Task.FromResult(KawaResult<DeleteComment.Response>.Failure(actorError));
        }

        if (!spots.TryGetComment(request.Id, out var existing))
        {
            return Task.FromResult(KawaResult<DeleteComment.Response>.Failure(new KawaError(KawaErrorKind.NotFound, "コメントが見つかりません。")));
        }

        if (!SpotAuthorization.CanDelete(actor!.IsAdmin))
        {
            return Task.FromResult(KawaResult<DeleteComment.Response>.Failure(new KawaError(KawaErrorKind.Forbidden, "コメントを削除する権限がありません。")));
        }

        spots.DeleteComment(request.Id);
        return Task.FromResult(KawaResult<DeleteComment.Response>.Success(new DeleteComment.Response(request.Id)));
    }
}
