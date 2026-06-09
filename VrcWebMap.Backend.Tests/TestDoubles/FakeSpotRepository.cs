using System.Diagnostics.CodeAnalysis;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.Tests.TestDoubles;

internal sealed class FakeSpotRepository : ISpotRepository
{
    private readonly Dictionary<Guid, Spot> spots = [];
    private readonly Dictionary<Guid, VRChatWorld> worlds = [];
    private readonly Dictionary<Guid, Restaurant> restaurants = [];
    private readonly Dictionary<Guid, Comment> comments = [];

    public List<Spot> SavedSpots { get; } = [];
    public List<VRChatWorld> SavedWorlds { get; } = [];
    public List<Restaurant> SavedRestaurants { get; } = [];
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

    public Restaurant[] ListRestaurants() => restaurants.Values.OrderBy(restaurant => restaurant.Name).ToArray();

    public bool TryGetRestaurant(Guid id, [NotNullWhen(true)] out Restaurant? restaurant) =>
        restaurants.TryGetValue(id, out restaurant);

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

    public void UpsertRestaurant(Restaurant restaurant)
    {
        restaurants[restaurant.Id] = restaurant;
        SavedRestaurants.Add(restaurant);
    }

    public bool DeleteRestaurant(Guid id) => restaurants.Remove(id);

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

        foreach (var restaurant in restaurants.Values.Where(restaurant => restaurant.SpotId == spotId).ToArray())
        {
            restaurants.Remove(restaurant.Id);
        }

        foreach (var comment in comments.Values.Where(comment => comment.SpotId == spotId).ToArray())
        {
            comments.Remove(comment.Id);
        }
    }
}
