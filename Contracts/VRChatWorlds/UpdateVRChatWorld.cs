namespace VrcWebMap.Backend.Contracts.VRChatWorlds;

/// <summary>
/// スポットに紐づく VRChat ワールド情報を更新するユースケースの契約です。
/// </summary>
public static class UpdateVRChatWorld
{
    /// <summary>
    /// VRChat ワールド情報更新に必要な入力です。
    /// </summary>
    /// <param name="Id">更新する VRChat ワールド情報の ID です。</param>
    /// <param name="VRChatWorldId">更新後の VRChat world ID です。</param>
    /// <param name="Name">更新後の VRChat ワールド名です。</param>
    /// <param name="RecommendedCapacity">更新後の推奨収容人数です。</param>
    /// <param name="Capacity">更新後の最大収容人数です。</param>
    /// <param name="Description">更新後の VRChat ワールド説明です。</param>
    /// <param name="PC">PC 対応の場合は <c>true</c> です。</param>
    /// <param name="Android">Android 対応の場合は <c>true</c> です。</param>
    /// <param name="IOS">iOS 対応の場合は <c>true</c> です。</param>
    /// <param name="IsPrivate">VRChat 上の release status が private の場合は <c>true</c> です。</param>
    public sealed record Request(
        Guid Id,
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
    /// 更新された VRChat ワールド情報を返すレスポンスです。
    /// </summary>
    /// <param name="World">更新された VRChat ワールド情報です。</param>
    public sealed record Response(VRChatWorldData World);
}
