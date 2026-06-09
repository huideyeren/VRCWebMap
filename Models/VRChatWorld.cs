namespace VrcWebMap.Backend.Models;

/// <summary>
/// スポットに紐づく VRChat ワールド情報です。
/// </summary>
/// <param name="Id">このレコードの主キーです。</param>
/// <param name="SpotId">関連するスポットの ID です。</param>
/// <param name="RegisteredByUserId">このワールド情報を登録したユーザーの ID です。</param>
/// <param name="VRChatWorldId">VRChat 側のワールド ID です。</param>
/// <param name="Name">VRChat ワールド名です。</param>
/// <param name="RecommendedCapacity">推奨収容人数です。</param>
/// <param name="Capacity">最大収容人数です。</param>
/// <param name="Description">VRChat ワールドの説明です。</param>
/// <param name="PC">PC 対応の有無です。</param>
/// <param name="Android">Android 対応の有無です。</param>
/// <param name="IOS">iOS 対応の有無です。</param>
/// <param name="IsPrivate">プライベートワールドかどうかを示します。既定値は <c>false</c> です。</param>
public sealed record VRChatWorld(
    Guid Id,
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
    bool IsPrivate = false)
{
    /// <summary>
    /// VRChat 公式サイトのワールドページ URL です。
    /// </summary>
    public Uri WorldPageUrl => new($"https://vrchat.com/home/world/{VRChatWorldId}/info");

    /// <summary>
    /// ポータル用 JSON で使う公開状態です。
    /// </summary>
    public string ReleaseStatus => IsPrivate ? "private" : "public";
}
