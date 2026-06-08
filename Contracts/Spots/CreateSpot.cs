using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.Spots;

/// <summary>
/// スポットを新規作成するユースケースの契約です。
/// </summary>
public static class CreateSpot
{
    /// <summary>
    /// スポット作成に必要な入力です。
    /// </summary>
    /// <param name="Name">スポット名です。</param>
    /// <param name="Latitude">スポットの緯度です。</param>
    /// <param name="Longitude">スポットの経度です。</param>
    /// <param name="Description">Markdown を想定したスポット説明です。</param>
    public sealed record Request(
        string Name,
        double Latitude,
        double Longitude,
        string Description);

    /// <summary>
    /// 作成されたスポットを返すレスポンスです。
    /// </summary>
    /// <param name="Spot">作成されたスポットです。</param>
    public sealed record Response(Spot Spot);
}
