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
    private readonly ConcurrentDictionary<Guid, Restaurant> restaurants = new();
    private readonly ConcurrentDictionary<Guid, Comment> comments = new();

    /// <summary>
    /// サンプルデータを持つリポジトリを初期化します。
    /// </summary>
    public InMemorySpotRepository()
    {
        var sampleSpot = new Spot(
            Guid.NewGuid(),
            "sample-discord-user",
            "VRChat World Hub",
            35.681236,
            139.767125,
            AreaCodes.Japan.Tokyo,
            "イベント会場と常設ワールドを管理するサンプルスポット");

        spots[sampleSpot.Id] = sampleSpot;

        var sampleWorld = new VRChatWorld(
            Guid.NewGuid(),
            sampleSpot.Id,
            "sample-discord-user",
            "wrld_7fd023b0-c563-41f5-8b54-e8e01879d7f7",
            "VketReal inVR 2023S Akiba",
            40,
            80,
            "秋葉原で開かれた VketReal 2023 Summer 会場のフォトグラメトリです。",
            PC: true,
            Android: true,
            IOS: true);

        worlds[sampleWorld.Id] = sampleWorld;
    }

    /// <inheritdoc />
    public Spot[] List() => spots.Values.OrderBy(spot => spot.Name).ToArray();

    /// <inheritdoc />
    public VRChatWorld[] ListWorlds() => worlds.Values.OrderBy(world => world.Name).ToArray();

    /// <inheritdoc />
    public bool TryGetWorld(Guid id, [NotNullWhen(true)] out VRChatWorld? world) =>
        worlds.TryGetValue(id, out world);

    /// <inheritdoc />
    public Restaurant[] ListRestaurants() => restaurants.Values.OrderBy(restaurant => restaurant.Name).ToArray();

    /// <inheritdoc />
    public bool TryGetRestaurant(Guid id, [NotNullWhen(true)] out Restaurant? restaurant) =>
        restaurants.TryGetValue(id, out restaurant);

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
    public void UpsertRestaurant(Restaurant restaurant) => restaurants[restaurant.Id] = restaurant;

    /// <inheritdoc />
    public bool DeleteRestaurant(Guid id) => restaurants.TryRemove(id, out _);

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

        foreach (var restaurant in restaurants.Values.Where(restaurant => restaurant.SpotId == spotId))
        {
            restaurants.TryRemove(restaurant.Id, out _);
        }

        foreach (var comment in comments.Values.Where(comment => comment.SpotId == spotId))
        {
            comments.TryRemove(comment.Id, out _);
        }
    }
}
