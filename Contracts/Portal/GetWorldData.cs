using System.Text.Json.Serialization;

namespace VrcWebMap.Backend.Contracts.Portal;

/// <summary>
/// VRChat ワールドポータル向け JSON を出力するユースケースの契約です。
/// </summary>
public static class GetWorldData
{
    /// <summary>
    /// ポータル用ワールドデータ出力の入力です。
    /// </summary>
    /// <param name="ShowPrivateWorld">private ワールドを出力に含めるかどうかです。</param>
    public sealed record Request(bool ShowPrivateWorld = true);

    /// <summary>
    /// WorldData.json 形式のレスポンスです。
    /// </summary>
    /// <param name="ReverseCategorys">カテゴリ表示順を反転するかどうかです。</param>
    /// <param name="ShowPrivateWorld">private ワールドを出力に含めるかどうかです。</param>
    /// <param name="Categorys">地域カテゴリごとのワールド一覧です。</param>
    /// <param name="Roles">private ワールド制御用のロール一覧です。</param>
    public sealed record Response(
        [property: JsonPropertyName("ReverseCategorys")]
        bool ReverseCategorys,
        [property: JsonPropertyName("ShowPrivateWorld")]
        bool ShowPrivateWorld,
        [property: JsonPropertyName("Categorys")]
        Category[] Categorys,
        [property: JsonPropertyName("Roles")]
        Role[] Roles);

    /// <summary>
    /// 地域カテゴリと、そのカテゴリに属するワールド一覧です。
    /// </summary>
    /// <param name="CategoryName">地域カテゴリ名です。</param>
    /// <param name="Worlds">地域カテゴリに属する VRChat ワールド一覧です。</param>
    public sealed record Category(
        [property: JsonPropertyName("Category")]
        string CategoryName,
        [property: JsonPropertyName("Worlds")]
        World[] Worlds);

    /// <summary>
    /// ポータル JSON に出力する VRChat ワールドです。
    /// </summary>
    public sealed record World(
        [property: JsonPropertyName("ID")]
        string ID,
        [property: JsonPropertyName("Name")]
        string Name,
        [property: JsonPropertyName("RecommendedCapacity")]
        int RecommendedCapacity,
        [property: JsonPropertyName("Capacity")]
        int Capacity,
        [property: JsonPropertyName("Description")]
        string Description,
        [property: JsonPropertyName("Platform")]
        Platform Platform,
        [property: JsonPropertyName("ReleaseStatus")]
        string ReleaseStatus,
        [property: JsonPropertyName("WorldPageUrl")]
        Uri WorldPageUrl);

    /// <summary>
    /// VRChat ワールドの対応プラットフォームです。
    /// </summary>
    public sealed record Platform(
        [property: JsonPropertyName("PC")]
        bool PC,
        [property: JsonPropertyName("Android")]
        bool Android,
        [property: JsonPropertyName("iOS")]
        bool IOS);

    /// <summary>
    /// private ワールド制御用ロールです。
    /// </summary>
    public sealed record Role(
        [property: JsonPropertyName("RoleName")]
        string RoleName,
        [property: JsonPropertyName("DisplayNames")]
        string[] DisplayNames);
}
