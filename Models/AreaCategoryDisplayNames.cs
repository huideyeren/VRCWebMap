namespace VrcWebMap.Backend.Models;

/// <summary>
/// 地域カテゴリの表示名と表示順を一元管理します。
/// </summary>
public static class AreaCategoryDisplayNames
{
    /// <summary>
    /// 地域カテゴリの表示用メタデータです。
    /// </summary>
    /// <param name="Category">地域カテゴリです。</param>
    /// <param name="Name">日本語の表示名です。</param>
    /// <param name="Order">0から始まる表示順です。</param>
    public sealed record Entry(
        AreaCategory Category,
        string Name,
        int Order);

    /// <summary>
    /// WPPLSと地図UIで共通利用する地域カテゴリ順です。
    /// </summary>
    public static readonly Entry[] All =
    [
        new(AreaCategory.Hokkaido, "北海道", 0),
        new(AreaCategory.Tohoku, "東北", 1),
        new(AreaCategory.Kanto, "関東", 2),
        new(AreaCategory.Chubu, "中部", 3),
        new(AreaCategory.Kansai, "関西", 4),
        new(AreaCategory.Chugoku, "中国", 5),
        new(AreaCategory.Shikoku, "四国", 6),
        new(AreaCategory.KyushuOkinawa, "九州・沖縄", 7),
        new(AreaCategory.Asia, "アジア", 8),
        new(AreaCategory.Europe, "ヨーロッパ", 9),
        new(AreaCategory.Africa, "アフリカ", 10),
        new(AreaCategory.Oceania, "オセアニア", 11),
        new(AreaCategory.NorthAmerica, "北アメリカ", 12),
        new(AreaCategory.SouthAmerica, "南アメリカ", 13),
        new(AreaCategory.Antarctica, "南極", 14)
    ];

    /// <summary>
    /// 地域カテゴリの日本語表示名を返します。
    /// </summary>
    public static string Get(AreaCategory category) =>
        Find(category)?.Name ?? category.ToString();

    /// <summary>
    /// 地域カテゴリの表示順を返します。
    /// </summary>
    public static int OrderOf(AreaCategory category) =>
        Find(category)?.Order ?? int.MaxValue;

    private static Entry? Find(AreaCategory category) =>
        All.FirstOrDefault(entry => entry.Category == category);
}
