using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Comments;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.UseCases.Comments;

[KawaUseCase(
    "comments.update",
    Summary = "Update comment",
    Description = "スポットに紐づくコメント本文を更新します。管理者またはコメント登録者本人のみ実行できます。",
    Version = "v1",
    Tags = new[] { "Comments" })]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "コメントが見つかりません。")]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "コメントを変更する権限がありません。")]
public sealed class UpdateCommentUseCase(ISpotRepository spots)
    : IUseCase<UpdateComment.Request, UpdateComment.Response>
{
    public Task<KawaResult<UpdateComment.Response>> ExecuteAsync(UpdateComment.Request request, CancellationToken cancellationToken = default)
    {
        if (!spots.TryGetComment(request.Id, out var existing))
        {
            return Task.FromResult(KawaResult<UpdateComment.Response>.Failure(new KawaError(KawaErrorKind.NotFound, "コメントが見つかりません。")));
        }

        if (!SpotAuthorization.CanMutate(existing.RegisteredByUserId, request.ActorUserId, request.ActorIsAdmin))
        {
            return Task.FromResult(KawaResult<UpdateComment.Response>.Failure(new KawaError(KawaErrorKind.Forbidden, "コメントを変更する権限がありません。")));
        }

        var comment = new Comment(existing.Id, existing.SpotId, existing.RegisteredByUserId, request.Comments.Trim());
        spots.UpsertComment(comment);
        return Task.FromResult(KawaResult<UpdateComment.Response>.Success(new UpdateComment.Response(comment)));
    }
}
