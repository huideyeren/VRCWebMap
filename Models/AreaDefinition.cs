namespace VrcWebMap.Backend.Models;

/// <summary>
/// エリアコードと表示名、VRChat ワールドポータルで利用する地域カテゴリの定義です。
/// </summary>
/// <param name="AreaCode">都道府県コードまたは 100 番台の海外エリアコードです。</param>
/// <param name="AreaName">エリアの表示名です。</param>
/// <param name="Category">VRChat ワールドポータルで利用する地域カテゴリです。</param>
public sealed record AreaDefinition(
    int AreaCode,
    string AreaName,
    AreaCategory Category);

/// <summary>
/// VRChat ワールドポータルで利用する地域カテゴリです。
/// </summary>
public enum AreaCategory
{
    /// <summary>北海道です。</summary>
    Hokkaido,
    /// <summary>東北です。</summary>
    Tohoku,
    /// <summary>関東です。</summary>
    Kanto,
    /// <summary>中部です。</summary>
    Chubu,
    /// <summary>関西です。</summary>
    Kansai,
    /// <summary>中国です。</summary>
    Chugoku,
    /// <summary>四国です。</summary>
    Shikoku,
    /// <summary>九州・沖縄です。</summary>
    KyushuOkinawa,
    /// <summary>アジアです。</summary>
    Asia,
    /// <summary>ヨーロッパです。</summary>
    Europe,
    /// <summary>アフリカです。</summary>
    Africa,
    /// <summary>オセアニアです。</summary>
    Oceania,
    /// <summary>北アメリカです。</summary>
    NorthAmerica,
    /// <summary>南アメリカです。</summary>
    SouthAmerica,
    /// <summary>南極です。</summary>
    Antarctica
}
