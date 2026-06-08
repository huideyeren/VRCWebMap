using System.Diagnostics.CodeAnalysis;
using VRCWebMapBackend.Models;
using VRCWebMapBackend.UseCases.Spots;

namespace VRCWebMapBackend.Tests.TestDoubles;

internal sealed class FakeSpotRepository : ISpotRepository
{
    private readonly Dictionary<Guid, Spot> spots = [];

    public List<Spot> SavedSpots { get; } = [];

    public List<Guid> DeletedSpotIds { get; } = [];

    public FakeSpotRepository(params Spot[] initialSpots)
    {
        foreach (var spot in initialSpots)
        {
            spots[spot.Id] = spot;
        }
    }

    public Spot[] List() => spots.Values.OrderBy(spot => spot.Name).ToArray();

    public bool TryGet(Guid id, [NotNullWhen(true)] out Spot? spot) =>
        spots.TryGetValue(id, out spot);

    public bool Exists(Guid id) => spots.ContainsKey(id);

    public void Upsert(Spot spot)
    {
        spots[spot.Id] = spot;
        SavedSpots.Add(spot);
    }

    public bool Delete(Guid id)
    {
        DeletedSpotIds.Add(id);
        return spots.Remove(id);
    }
}
