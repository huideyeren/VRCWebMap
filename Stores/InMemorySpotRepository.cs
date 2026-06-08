using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using VRCWebMapBackend.Models;
using VRCWebMapBackend.UseCases.Spots;

namespace VRCWebMapBackend.Stores;

/// <summary>
/// 試作用のインメモリスポットリポジトリです。
/// </summary>
public sealed class InMemorySpotRepository : ISpotRepository
{
    private readonly ConcurrentDictionary<Guid, Spot> spots = new();

    /// <summary>
    /// サンプルデータを持つリポジトリを初期化します。
    /// </summary>
    public InMemorySpotRepository()
    {
        var sampleSpot = new Spot(
            Guid.NewGuid(),
            "VRChat World Hub",
            35.681236,
            139.767125,
            "イベント会場と常設ワールドを管理するサンプルスポット");

        spots[sampleSpot.Id] = sampleSpot;
    }

    /// <inheritdoc />
    public Spot[] List() => spots.Values.OrderBy(spot => spot.Name).ToArray();

    /// <inheritdoc />
    public bool TryGet(Guid id, [NotNullWhen(true)] out Spot? spot) =>
        spots.TryGetValue(id, out spot);

    /// <inheritdoc />
    public bool Exists(Guid id) => spots.ContainsKey(id);

    /// <inheritdoc />
    public void Upsert(Spot spot) => spots[spot.Id] = spot;

    /// <inheritdoc />
    public bool Delete(Guid id) => spots.TryRemove(id, out _);
}
