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
    public sealed record Request;

    /// <summary>
    /// WorldData.json 形式のレスポンスです。
    /// </summary>
    /// <param name="ReverseCategorys">カテゴリ表示順を反転するかどうかです。</param>
    /// <param name="ShowPrivateWorld">private release のワールドを選択可能にするかどうかです。常に <c>true</c> です。</param>
    /// <param name="Categorys">地域カテゴリごとのワールド一覧です。</param>
    public sealed record Response(
        [property: JsonPropertyName("ReverseCategorys")]
        bool ReverseCategorys,
        [property: JsonPropertyName("ShowPrivateWorld")]
        bool ShowPrivateWorld,
        [property: JsonPropertyName("Categorys")]
        Category[] Categorys);

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
    /// <param name="ID">WPPLS が使用する <c>wrld_...</c> 形式の VRChat world ID です。</param>
    /// <param name="Name">ポータルに表示するワールド名です。</param>
    /// <param name="RecommendedCapacity">推奨収容人数です。</param>
    /// <param name="Capacity">最大収容人数です。</param>
    /// <param name="Description">ポータルに表示するワールド説明です。</param>
    /// <param name="Platform">対応プラットフォームです。</param>
    /// <param name="ReleaseStatus">VRChat 上の release status です。<c>public</c> または <c>private</c> を出力します。</param>
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
        string ReleaseStatus);

    /// <summary>
    /// VRChat ワールドの対応プラットフォームです。
    /// </summary>
    /// <param name="PC">PC 対応の場合は <c>true</c> です。</param>
    /// <param name="Android">Android 対応の場合は <c>true</c> です。</param>
    /// <param name="IOS">iOS 対応の場合は <c>true</c> です。</param>
    public sealed record Platform(
        [property: JsonPropertyName("PC")]
        bool PC,
        [property: JsonPropertyName("Android")]
        bool Android,
        [property: JsonPropertyName("iOS")]
        bool IOS);

}
