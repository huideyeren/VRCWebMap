namespace VrcWebMap.Backend.Contracts.Spots;

/// <summary>
/// スポット一覧を取得するユースケースの契約です。
/// </summary>
public static class ListSpots
{
    /// <summary>
    /// スポット一覧取得の入力です。
    /// </summary>
    /// <param name="Query">スポット名と説明を検索する任意クエリです。空白区切りで複数語を指定できます。</param>
    public sealed record Request(string? Query = null);

    /// <summary>
    /// スポット一覧を返すレスポンスです。
    /// </summary>
    public sealed record Item(
        Guid Id,
        string RegisteredByUserId,
        string Name,
        double Latitude,
        double Longitude,
        int AreaCode,
        string Description,
        bool HasVRChatWorld,
        bool HasPlaceInfo);

    /// <param name="Spots">地図表示用metadataを含むスポット一覧です。</param>
    public sealed record Response(Item[] Spots);
}
