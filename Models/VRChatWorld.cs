namespace VRCWebMapBackend.Models;

/// <summary>
/// スポットに紐づく VRChat ワールド情報です。
/// </summary>
/// <param name="Id">このレコードの主キーです。</param>
/// <param name="SpotsId">関連するスポットの ID です。</param>
/// <param name="VRChatWorldId">VRChat 側のワールド ID です。</param>
/// <param name="Name">VRChat ワールド名です。</param>
/// <param name="RecommendedCapacity">推奨収容人数です。</param>
/// <param name="Capacity">最大収容人数です。</param>
/// <param name="IsPrivate">プライベートワールドかどうかを示します。</param>
/// <param name="Description">VRChat ワールドの説明です。</param>
/// <param name="PC">PC 対応の有無です。</param>
/// <param name="Android">Android 対応の有無です。</param>
/// <param name="IOS">iOS 対応の有無です。</param>
public sealed record VRChatWorld(
    Guid Id,
    Guid SpotsId,
    string VRChatWorldId,
    string Name,
    int RecommendedCapacity,
    int Capacity,
    bool IsPrivate,
    string Description,
    bool PC,
    bool Android,
    bool IOS);
