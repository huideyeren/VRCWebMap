using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.Areas;

/// <summary>
/// エリア定義一覧を取得するユースケースの契約です。
/// </summary>
public static class ListAreas
{
    /// <summary>
    /// エリア定義一覧取得の入力です。
    /// </summary>
    public sealed record Request;

    /// <summary>
    /// 地図UIで利用する地域カテゴリ情報を含むエリア定義です。
    /// </summary>
    public sealed record Item(
        int AreaCode,
        string AreaName,
        AreaCategory Category,
        string CategoryName,
        int CategoryOrder);

    /// <summary>
    /// エリア定義一覧を返すレスポンスです。
    /// </summary>
    /// <param name="Areas">現在サポートするエリア定義一覧です。</param>
    public sealed record Response(Item[] Areas);
}
