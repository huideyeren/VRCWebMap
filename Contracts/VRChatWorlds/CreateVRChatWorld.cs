using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.VRChatWorlds;

/// <summary>
/// スポットに VRChat ワールド情報を追加するユースケースの契約です。
/// </summary>
public static class CreateVRChatWorld
{
    /// <summary>
    /// VRChat ワールド情報の登録入力です。
    /// </summary>
    public sealed record Request(
        Guid SpotId,
        string RegisteredByUserId,
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
    public sealed record Response(VRChatWorld World);
}
