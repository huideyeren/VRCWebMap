using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Comments;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Resources;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.Comments;

[KawaUseCase(
    "comments.update",
    Summary = "Update comment",
    Description = "スポットに紐づくコメント本文を更新します。管理者またはコメント登録者本人のみ実行できます。",
    Version = "v1",
    Tags = new[] { "Comments" })]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "コメントが見つかりません。")]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "コメントを変更する権限がありません。")]
public sealed class UpdateCommentUseCase(
    ISpotRepository spots,
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
    : IUseCase<UpdateComment.Request, UpdateComment.Response>
{
    public Task<KawaResult<UpdateComment.Response>> ExecuteAsync(UpdateComment.Request request, CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Task.FromResult(KawaResult<UpdateComment.Response>.Failure(actorError));
        }

        if (!spots.TryGetComment(request.Id, out var existing))
        {
            return Task.FromResult(KawaResult<UpdateComment.Response>.Failure(new KawaError(KawaErrorKind.NotFound, "コメントが見つかりません。")));
        }

        if (!SpotAuthorization.CanMutate(existing.RegisteredByUserId, actor!.DiscordUserId, actor.IsAdmin))
        {
            return Task.FromResult(KawaResult<UpdateComment.Response>.Failure(new KawaError(KawaErrorKind.Forbidden, "コメントを変更する権限がありません。")));
        }

        var comment = new Comment(existing.Id, existing.SpotId, existing.RegisteredByUserId, request.Comments.Trim());
        spots.UpsertComment(comment);
        var mapper = new PublicResourceMapper(users.List(), actor);
        return Task.FromResult(KawaResult<UpdateComment.Response>.Success(
            new UpdateComment.Response(mapper.ToComment(comment))));
    }
}
