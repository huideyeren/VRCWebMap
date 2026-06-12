using System.Diagnostics.CodeAnalysis;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.Tests.TestDoubles;

internal sealed class FakeSpotRepository : ISpotRepository
{
    private readonly Dictionary<Guid, Spot> spots = [];
    private readonly Dictionary<Guid, VRChatWorld> worlds = [];
    private readonly Dictionary<Guid, PlaceInfo> placeInfos = [];
    private readonly Dictionary<Guid, WebLink> webLinks = [];
    private readonly Dictionary<Guid, Comment> comments = [];

    public List<Spot> SavedSpots { get; } = [];
    public List<VRChatWorld> SavedWorlds { get; } = [];
    public List<PlaceInfo> SavedPlaceInfos { get; } = [];
    public List<WebLink> SavedWebLinks { get; } = [];
    public List<Comment> SavedComments { get; } = [];

    public List<Guid> DeletedSpotIds { get; } = [];

    public FakeSpotRepository(params Spot[] initialSpots)
    {
        foreach (var spot in initialSpots)
        {
            spots[spot.Id] = spot;
        }
    }

    public Spot[] List() => spots.Values.OrderBy(spot => spot.Name).ToArray();

    public VRChatWorld[] ListWorlds() => worlds.Values.OrderBy(world => world.Name).ToArray();

    public bool TryGetWorld(Guid id, [NotNullWhen(true)] out VRChatWorld? world) =>
        worlds.TryGetValue(id, out world);

    public PlaceInfo[] ListPlaceInfos() => placeInfos.Values.OrderBy(placeInfo => placeInfo.Name).ToArray();

    public bool TryGetPlaceInfo(Guid id, [NotNullWhen(true)] out PlaceInfo? placeInfo) =>
        placeInfos.TryGetValue(id, out placeInfo);

    public WebLink[] ListWebLinks() => webLinks.Values.OrderBy(webLink => webLink.SiteName).ToArray();

    public bool TryGetWebLink(Guid id, [NotNullWhen(true)] out WebLink? webLink) =>
        webLinks.TryGetValue(id, out webLink);

    public Comment[] ListComments() => comments.Values.OrderBy(comment => comment.Id).ToArray();

    public bool TryGetComment(Guid id, [NotNullWhen(true)] out Comment? comment) =>
        comments.TryGetValue(id, out comment);

    public void AddWorld(VRChatWorld world) => worlds[world.Id] = world;

    public void UpsertWorld(VRChatWorld world)
    {
        worlds[world.Id] = world;
        SavedWorlds.Add(world);
    }

    public bool DeleteWorld(Guid id) => worlds.Remove(id);

    public void UpsertPlaceInfo(PlaceInfo placeInfo)
    {
        placeInfos[placeInfo.Id] = placeInfo;
        SavedPlaceInfos.Add(placeInfo);
    }

    public bool DeletePlaceInfo(Guid id) => placeInfos.Remove(id);

    public void UpsertWebLink(WebLink webLink)
    {
        webLinks[webLink.Id] = webLink;
        SavedWebLinks.Add(webLink);
    }

    public bool DeleteWebLink(Guid id) => webLinks.Remove(id);

    public void UpsertComment(Comment comment)
    {
        comments[comment.Id] = comment;
        SavedComments.Add(comment);
    }

    public bool DeleteComment(Guid id) => comments.Remove(id);

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

    public void DeleteRelatedData(Guid spotId)
    {
        foreach (var world in worlds.Values.Where(world => world.SpotId == spotId).ToArray())
        {
            worlds.Remove(world.Id);
        }

        foreach (var placeInfo in placeInfos.Values.Where(placeInfo => placeInfo.SpotId == spotId).ToArray())
        {
            placeInfos.Remove(placeInfo.Id);
        }

        foreach (var webLink in webLinks.Values.Where(webLink => webLink.SpotId == spotId).ToArray())
        {
            webLinks.Remove(webLink.Id);
        }

        foreach (var comment in comments.Values.Where(comment => comment.SpotId == spotId).ToArray())
        {
            comments.Remove(comment.Id);
        }
    }
}
