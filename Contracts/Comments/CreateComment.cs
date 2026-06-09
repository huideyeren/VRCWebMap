using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.Comments;

/// <summary>
/// スポットにコメントを追加するユースケースの契約です。
/// </summary>
public static class CreateComment
{
    /// <summary>
    /// コメントの登録入力です。
    /// </summary>
    public sealed record Request(
        Guid SpotId,
        string RegisteredByUserId,
        string Comments);

    /// <summary>
    /// 登録されたコメントを返すレスポンスです。
    /// </summary>
    /// <param name="Comment">登録されたコメントです。</param>
    public sealed record Response(Comment Comment);
}
