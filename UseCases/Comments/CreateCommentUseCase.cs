using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Comments;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Spots;

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
public sealed class CreateCommentUseCase(ISpotRepository spots)
    : IUseCase<CreateComment.Request, CreateComment.Response>
{
    /// <summary>
    /// 指定されたスポットにコメントを追加します。
    /// </summary>
    public Task<KawaResult<CreateComment.Response>> ExecuteAsync(
        CreateComment.Request request,
        CancellationToken cancellationToken = default)
    {
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

        if (string.IsNullOrWhiteSpace(request.RegisteredByUserId) ||
            string.IsNullOrWhiteSpace(request.Comments))
        {
            var error = new KawaError(KawaErrorKind.Validation, "登録者 ID とコメント本文は必須です。");
            return Task.FromResult(KawaResult<CreateComment.Response>.Failure(error));
        }

        var comment = new Comment(
            Guid.NewGuid(),
            request.SpotId,
            request.RegisteredByUserId.Trim(),
            request.Comments.Trim());

        spots.UpsertComment(comment);

        var response = new CreateComment.Response(comment);
        return Task.FromResult(KawaResult<CreateComment.Response>.Success(response));
    }
}
