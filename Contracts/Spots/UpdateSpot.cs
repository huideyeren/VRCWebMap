using VRCWebMapBackend.Models;

namespace VRCWebMapBackend.Contracts.Spots;

/// <summary>
/// 既存スポットを更新するユースケースの契約です。
/// </summary>
public static class UpdateSpot
{
    /// <summary>
    /// スポット更新に必要な入力です。
    /// </summary>
    /// <param name="Id">更新するスポットの ID です。</param>
    /// <param name="Name">更新後のスポット名です。</param>
    /// <param name="Latitude">更新後の緯度です。</param>
    /// <param name="Longitude">更新後の経度です。</param>
    /// <param name="Description">更新後の Markdown 説明です。</param>
    public sealed record Request(
        Guid Id,
        string Name,
        double Latitude,
        double Longitude,
        string Description);

    /// <summary>
    /// 更新されたスポットを返すレスポンスです。
    /// </summary>
    /// <param name="Spot">更新されたスポットです。</param>
    public sealed record Response(Spot Spot);
}
