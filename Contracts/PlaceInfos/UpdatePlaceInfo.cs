using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.PlaceInfos;

/// <summary>
/// スポットに紐づく場所情報を更新するユースケースの契約です。
/// </summary>
public static class UpdatePlaceInfo
{
    /// <summary>
    /// 場所情報更新に必要な入力です。
    /// </summary>
    /// <param name="Id">更新する場所情報の ID です。</param>
    /// <param name="Name">更新後の場所名です。</param>
    /// <param name="Address">更新後の所在地です。</param>
    /// <param name="BusinessInformation">更新後の Markdown 対応営業情報です。</param>
    public sealed record Request(
        Guid Id,
        string Name,
        string Address,
        string BusinessInformation);

    /// <summary>
    /// 更新された場所情報を返すレスポンスです。
    /// </summary>
    /// <param name="PlaceInfo">更新された場所情報です。</param>
    public sealed record Response(PlaceInfo PlaceInfo);
}
