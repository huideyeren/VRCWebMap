namespace VRCWebMapBackend.Contracts.Spots;

/// <summary>
/// スポットを削除するユースケースの契約です。
/// </summary>
public static class DeleteSpot
{
    /// <summary>
    /// 削除対象のスポットを指定する入力です。
    /// </summary>
    /// <param name="Id">削除するスポットの ID です。</param>
    public sealed record Request(Guid Id);

    /// <summary>
    /// 削除されたスポット ID を返すレスポンスです。
    /// </summary>
    /// <param name="Id">削除されたスポットの ID です。</param>
    public sealed record Response(Guid Id);
}
