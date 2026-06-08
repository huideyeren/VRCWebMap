namespace VRCWebMapBackend.Models;

/// <summary>
/// 地図上に表示・管理する基本地点です。
/// </summary>
/// <param name="Id">スポットの主キーです。</param>
/// <param name="Name">スポット名です。</param>
/// <param name="Latitude">スポットの緯度です。</param>
/// <param name="Longitude">スポットの経度です。</param>
/// <param name="Description">Markdown を想定したスポット説明です。</param>
public sealed record Spot(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    string Description);
