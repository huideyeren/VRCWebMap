namespace VrcWebMap.Backend.Contracts.PlaceInfos;

/// <summary>
/// スポットに紐づく場所情報を削除するユースケースの契約です。
/// </summary>
public static class DeletePlaceInfo
{
    /// <summary>
    /// 削除対象の場所情報と操作ユーザーを指定する入力です。
    /// </summary>
    /// <param name="Id">削除する場所情報の ID です。</param>
    /// <param name="ActorUserId">削除操作を行うユーザーの ID です。</param>
    /// <param name="ActorIsAdmin">削除操作を行うユーザーが管理者かどうかです。</param>
    public sealed record Request(Guid Id, string ActorUserId, bool ActorIsAdmin);

    /// <summary>
    /// 削除された場所情報 ID を返すレスポンスです。
    /// </summary>
    /// <param name="Id">削除された場所情報の ID です。</param>
    public sealed record Response(Guid Id);
}
