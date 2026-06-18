namespace VrcWebMap.Backend.Contracts.Comments;

/// <summary>
/// スポットに紐づくコメントを削除するユースケースの契約です。
/// </summary>
public static class DeleteComment
{
    /// <summary>
    /// 削除対象のコメントと操作ユーザーを指定する入力です。
    /// </summary>
    /// <param name="Id">削除するコメントの ID です。</param>
    /// <param name="ActorUserId">削除操作を行うユーザーの ID です。</param>
    /// <param name="ActorIsAdmin">削除操作を行うユーザーが管理者かどうかです。</param>
    public sealed record Request(Guid Id, string ActorUserId, bool ActorIsAdmin);

    /// <summary>
    /// 削除されたコメント ID を返すレスポンスです。
    /// </summary>
    /// <param name="Id">削除されたコメントの ID です。</param>
    public sealed record Response(Guid Id);
}
