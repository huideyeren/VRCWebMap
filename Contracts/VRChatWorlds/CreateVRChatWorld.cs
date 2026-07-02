namespace VrcWebMap.Backend.Contracts.VRChatWorlds;

/// <summary>
/// スポットに VRChat ワールド情報を追加するユースケースの契約です。
/// </summary>
public static class CreateVRChatWorld
{
    /// <summary>
    /// VRChat ワールド情報の登録入力です。
    /// </summary>
    /// <param name="SpotId">VRChat ワールド情報を追加するスポットの ID です。</param>
    /// <param name="VRChatWorldId">VRChat 側の world ID です。</param>
    /// <param name="Name">VRChat ワールド名です。</param>
    /// <param name="RecommendedCapacity">推奨収容人数です。</param>
    /// <param name="Capacity">最大収容人数です。</param>
    /// <param name="Description">VRChat ワールドの説明です。</param>
    /// <param name="PC">PC 対応の場合は <c>true</c> です。</param>
    /// <param name="Android">Android 対応の場合は <c>true</c> です。</param>
    /// <param name="IOS">iOS 対応の場合は <c>true</c> です。</param>
    /// <param name="IsPrivate">private ワールドとしてポータル JSON に出力する場合は <c>true</c> です。</param>
    public sealed record Request(
        Guid SpotId,
        string VRChatWorldId,
        string Name,
        int RecommendedCapacity,
        int Capacity,
        string Description,
        bool PC,
        bool Android,
        bool IOS,
        bool IsPrivate = false);

    /// <summary>
    /// 登録された VRChat ワールド情報を返すレスポンスです。
    /// </summary>
    /// <param name="World">登録された VRChat ワールド情報です。</param>
    public sealed record Response(VRChatWorldData World);
}
