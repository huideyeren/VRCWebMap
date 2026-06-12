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
    public sealed record Request(
        Guid SpotId,
        string RegisteredByUserId,
        string Name,
        string Address,
        string BusinessInformation);

    /// <summary>
    /// 登録された場所情報を返すレスポンスです。
    /// </summary>
    /// <param name="PlaceInfo">登録された場所情報です。</param>
    public sealed record Response(PlaceInfo PlaceInfo);
}
