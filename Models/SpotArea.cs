namespace VRCWebMapBackend.Models;

/// <summary>
/// スポットとエリア定義を紐づける関連です。
/// </summary>
/// <param name="SpotsId">関連するスポットの ID です。</param>
/// <param name="AreaCode">関連するエリアコードです。</param>
public sealed record SpotArea(
    Guid SpotsId,
    int AreaCode);
