using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.Spots;

/// <summary>
/// スポット一覧を取得するユースケースの契約です。
/// </summary>
public static class ListSpots
{
    /// <summary>
    /// スポット一覧取得の入力です。
    /// </summary>
    /// <param name="Query">将来の検索条件用の任意クエリです。</param>
    public sealed record Request(string? Query = null);

    /// <summary>
    /// スポット一覧を返すレスポンスです。
    /// </summary>
    /// <param name="Spots">管理対象のスポット一覧です。</param>
    public sealed record Response(Spot[] Spots);
}
