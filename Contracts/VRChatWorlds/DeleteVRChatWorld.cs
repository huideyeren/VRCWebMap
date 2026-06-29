namespace VrcWebMap.Backend.Contracts.VRChatWorlds;

/// <summary>
/// スポットに紐づく VRChat ワールド情報を削除するユースケースの契約です。
/// </summary>
public static class DeleteVRChatWorld
{
    /// <summary>
    /// 削除対象の VRChat ワールド情報と操作ユーザーを指定する入力です。
    /// </summary>
    /// <param name="Id">削除する VRChat ワールド情報の ID です。</param>
    public sealed record Request(Guid Id);

    /// <summary>
    /// 削除された VRChat ワールド情報 ID を返すレスポンスです。
    /// </summary>
    /// <param name="Id">削除された VRChat ワールド情報の ID です。</param>
    public sealed record Response(Guid Id);
}
