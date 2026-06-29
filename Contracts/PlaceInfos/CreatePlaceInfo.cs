using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.PlaceInfos;

/// <summary>
/// スポットに場所情報を追加するユースケースの契約です。
/// </summary>
public static class CreatePlaceInfo
{
    /// <summary>
    /// 場所情報の登録入力です。
    /// </summary>
    /// <param name="SpotId">場所情報を追加するスポットの ID です。</param>
    /// <param name="Name">場所名です。</param>
    /// <param name="Address">所在地です。</param>
    /// <param name="BusinessInformation">営業時間、昼夜営業、定休日、臨時休業などを自由に記述できる Markdown 対応テキストです。</param>
    public sealed record Request(
        Guid SpotId,
        string Name,
        string Address,
        string BusinessInformation);

    /// <summary>
    /// 登録された場所情報を返すレスポンスです。
    /// </summary>
    /// <param name="PlaceInfo">登録された場所情報です。</param>
    public sealed record Response(PlaceInfo PlaceInfo);
}
