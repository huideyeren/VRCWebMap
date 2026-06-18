using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.Stores;

/// <summary>
/// 試作用のインメモリスポットリポジトリです。
/// </summary>
public sealed class InMemorySpotRepository : ISpotRepository
{
    private readonly ConcurrentDictionary<Guid, Spot> spots = new();
    private readonly ConcurrentDictionary<Guid, VRChatWorld> worlds = new();
    private readonly ConcurrentDictionary<Guid, PlaceInfo> placeInfos = new();
    private readonly ConcurrentDictionary<Guid, WebLink> webLinks = new();
    private readonly ConcurrentDictionary<Guid, Comment> comments = new();

    /// <inheritdoc />
    public Spot[] List() => spots.Values.OrderBy(spot => spot.Name).ToArray();

    /// <inheritdoc />
    public VRChatWorld[] ListWorlds() => worlds.Values.OrderBy(world => world.Name).ToArray();

    /// <inheritdoc />
    public bool TryGetWorld(Guid id, [NotNullWhen(true)] out VRChatWorld? world) =>
        worlds.TryGetValue(id, out world);

    /// <inheritdoc />
    public PlaceInfo[] ListPlaceInfos() => placeInfos.Values.OrderBy(placeInfo => placeInfo.Name).ToArray();

    /// <inheritdoc />
    public bool TryGetPlaceInfo(Guid id, [NotNullWhen(true)] out PlaceInfo? placeInfo) =>
        placeInfos.TryGetValue(id, out placeInfo);

    /// <inheritdoc />
    public WebLink[] ListWebLinks() => webLinks.Values.OrderBy(webLink => webLink.SiteName).ToArray();

    /// <inheritdoc />
    public bool TryGetWebLink(Guid id, [NotNullWhen(true)] out WebLink? webLink) =>
        webLinks.TryGetValue(id, out webLink);

    /// <inheritdoc />
    public Comment[] ListComments() => comments.Values.OrderBy(comment => comment.Id).ToArray();

    /// <inheritdoc />
    public bool TryGetComment(Guid id, [NotNullWhen(true)] out Comment? comment) =>
        comments.TryGetValue(id, out comment);

    /// <inheritdoc />
    public void UpsertWorld(VRChatWorld world) => worlds[world.Id] = world;

    /// <inheritdoc />
    public bool DeleteWorld(Guid id) => worlds.TryRemove(id, out _);

    /// <inheritdoc />
    public void UpsertPlaceInfo(PlaceInfo placeInfo) => placeInfos[placeInfo.Id] = placeInfo;

    /// <inheritdoc />
    public bool DeletePlaceInfo(Guid id) => placeInfos.TryRemove(id, out _);

    /// <inheritdoc />
    public void UpsertWebLink(WebLink webLink) => webLinks[webLink.Id] = webLink;

    /// <inheritdoc />
    public bool DeleteWebLink(Guid id) => webLinks.TryRemove(id, out _);

    /// <inheritdoc />
    public void UpsertComment(Comment comment) => comments[comment.Id] = comment;

    /// <inheritdoc />
    public bool DeleteComment(Guid id) => comments.TryRemove(id, out _);

    /// <inheritdoc />
    public bool TryGet(Guid id, [NotNullWhen(true)] out Spot? spot) =>
        spots.TryGetValue(id, out spot);

    /// <inheritdoc />
    public bool Exists(Guid id) => spots.ContainsKey(id);

    /// <inheritdoc />
    public void Upsert(Spot spot) => spots[spot.Id] = spot;

    /// <inheritdoc />
    public bool Delete(Guid id) => spots.TryRemove(id, out _);

    /// <inheritdoc />
    public void DeleteRelatedData(Guid spotId)
    {
        foreach (var world in worlds.Values.Where(world => world.SpotId == spotId))
        {
            worlds.TryRemove(world.Id, out _);
        }

        foreach (var placeInfo in placeInfos.Values.Where(placeInfo => placeInfo.SpotId == spotId))
        {
            placeInfos.TryRemove(placeInfo.Id, out _);
        }

        foreach (var webLink in webLinks.Values.Where(webLink => webLink.SpotId == spotId))
        {
            webLinks.TryRemove(webLink.Id, out _);
        }

        foreach (var comment in comments.Values.Where(comment => comment.SpotId == spotId))
        {
            comments.TryRemove(comment.Id, out _);
        }
    }
}
