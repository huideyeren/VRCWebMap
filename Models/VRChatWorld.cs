namespace VrcWebMap.Backend.Models;

/// <summary>
/// スポットに紐づく VRChat ワールド情報です。
/// </summary>
/// <param name="Id">このレコードの主キーです。</param>
/// <param name="SpotId">関連するスポットの ID です。地図外ワールドでは <c>null</c> です。</param>
/// <param name="RegisteredByUserId">このワールド情報を登録したユーザーの ID です。</param>
/// <param name="VRChatWorldId">VRChat 側のワールド ID です。</param>
/// <param name="Name">VRChat ワールド名です。</param>
/// <param name="RecommendedCapacity">推奨収容人数です。</param>
/// <param name="Capacity">最大収容人数です。</param>
/// <param name="Description">VRChat ワールドの説明です。</param>
/// <param name="PC">PC 対応の有無です。</param>
/// <param name="Android">Android 対応の有無です。</param>
/// <param name="IOS">iOS 対応の有無です。</param>
/// <param name="IsPrivate">VRChat 上の release status が private の場合は <c>true</c> です。WPPLS の閲覧権限には使用しません。</param>
/// <param name="PortalCategoryId">地図外ワールドが属するポータルカテゴリ ID です。通常のSpotワールドでは <c>null</c> です。</param>
public sealed record VRChatWorld(
    Guid Id,
    Guid? SpotId,
    string RegisteredByUserId,
    string VRChatWorldId,
    string Name,
    int RecommendedCapacity,
    int Capacity,
    string Description,
    bool PC,
    bool Android,
    bool IOS,
    bool IsPrivate = false,
    Guid? PortalCategoryId = null)
{
    /// <summary>
    /// VRChat 公式サイトのワールドページ URL です。
    /// </summary>
    public Uri WorldPageUrl => new($"https://vrchat.com/home/world/{VRChatWorldId}/info");

    /// <summary>
    /// WPPLS に出力する VRChat 上の release status です。
    /// </summary>
    public string ReleaseStatus => IsPrivate ? "private" : "public";
}
