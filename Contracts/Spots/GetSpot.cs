using VrcWebMap.Backend.Contracts.Comments;
using VrcWebMap.Backend.Contracts.PlaceInfos;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Contracts.WebLinks;

namespace VrcWebMap.Backend.Contracts.Spots;

/// <summary>
/// 指定されたスポットを取得するユースケースの契約です。
/// </summary>
public static class GetSpot
{
    /// <summary>
    /// 取得対象のスポットを指定する入力です。
    /// </summary>
    /// <param name="Id">取得するスポットの ID です。</param>
    public sealed record Request(Guid Id);

    /// <summary>
    /// 取得したスポットと関連データを返すレスポンスです。
    /// </summary>
    /// <param name="Spot">取得したスポットです。</param>
    /// <param name="VRChatWorlds">スポットに紐づく VRChat ワールド情報です。</param>
    /// <param name="PlaceInfos">スポットに紐づく場所情報です。</param>
    /// <param name="WebLinks">スポットに紐づく Web サイト情報です。</param>
    /// <param name="Comments">スポットに紐づくコメントです。</param>
    public sealed record Response(
        SpotData Spot,
        VRChatWorldData[] VRChatWorlds,
        PlaceInfoData[] PlaceInfos,
        WebLinkData[] WebLinks,
        CommentData[] Comments);
}
