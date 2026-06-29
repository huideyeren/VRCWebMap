using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.Comments;

/// <summary>
/// スポットに紐づくコメントを更新するユースケースの契約です。
/// </summary>
public static class UpdateComment
{
    /// <summary>
    /// コメント更新に必要な入力です。
    /// </summary>
    /// <param name="Id">更新するコメントの ID です。</param>
    /// <param name="Comments">更新後の Markdown コメント本文です。</param>
    public sealed record Request(Guid Id, string Comments);

    /// <summary>
    /// 更新されたコメントを返すレスポンスです。
    /// </summary>
    /// <param name="Comment">更新されたコメントです。</param>
    public sealed record Response(Comment Comment);
}
