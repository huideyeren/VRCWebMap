namespace VrcWebMap.Backend.Models;

/// <summary>
/// エリアコードと表示名、分類の定義です。
/// </summary>
/// <param name="AreaCode">都道府県コードまたは 100 番台の海外エリアコードです。</param>
/// <param name="AreaName">エリアの表示名です。</param>
/// <param name="Category">エリアの大分類です。</param>
public sealed record AreaDefinition(
    int AreaCode,
    string AreaName,
    AreaCategory Category);

/// <summary>
/// エリアの大分類です。
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
    /// <summary>近畿です。</summary>
    Kinki,
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
