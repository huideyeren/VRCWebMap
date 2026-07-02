using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Comments;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Resources;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.Comments;

[KawaUseCase(
    "comments.create",
    Summary = "Create comment",
    Description = "指定されたスポットにコメントを追加します。",
    Version = "v1",
    Tags = new[] { "Comments" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "コメントの入力値が不正です。")]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "スポットが見つかりません。")]
/// <summary>
/// スポットにコメントを追加するユースケースです。
/// </summary>
public sealed class CreateCommentUseCase(
    ISpotRepository spots,
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
    : IUseCase<CreateComment.Request, CreateComment.Response>
{
    /// <summary>
    /// 指定されたスポットにコメントを追加します。
    /// </summary>
    public Task<KawaResult<CreateComment.Response>> ExecuteAsync(
        CreateComment.Request request,
        CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Task.FromResult(KawaResult<CreateComment.Response>.Failure(actorError));
        }

        if (request.SpotId == Guid.Empty)
        {
            var error = new KawaError(KawaErrorKind.Validation, "スポット ID は必須です。");
            return Task.FromResult(KawaResult<CreateComment.Response>.Failure(error));
        }

        if (!spots.Exists(request.SpotId))
        {
            var error = new KawaError(KawaErrorKind.NotFound, "スポットが見つかりません。");
            return Task.FromResult(KawaResult<CreateComment.Response>.Failure(error));
        }

        if (string.IsNullOrWhiteSpace(request.Comments))
        {
            var error = new KawaError(KawaErrorKind.Validation, "コメント本文は必須です。");
            return Task.FromResult(KawaResult<CreateComment.Response>.Failure(error));
        }

        var comment = new Comment(
            Guid.NewGuid(),
            request.SpotId,
            actor!.DiscordUserId,
            request.Comments.Trim());

        spots.UpsertComment(comment);

        var mapper = new PublicResourceMapper(users.List(), actor);
        var response = new CreateComment.Response(mapper.ToComment(comment));
        return Task.FromResult(KawaResult<CreateComment.Response>.Success(response));
    }
}
