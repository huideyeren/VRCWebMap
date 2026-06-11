using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.Stores;

/// <summary>
/// PostgreSQL を使うスポットリポジトリです。
/// </summary>
public sealed class PostgreSqlSpotRepository(AppDbContext db) : ISpotRepository
{
    public Spot[] List() =>
        db.Spots
            .AsNoTracking()
            .OrderBy(spot => spot.Name)
            .ToArray();

    public VRChatWorld[] ListWorlds() =>
        db.VRChatWorlds
            .AsNoTracking()
            .OrderBy(world => world.Name)
            .ToArray();

    public bool TryGetWorld(Guid id, [NotNullWhen(true)] out VRChatWorld? world)
    {
        world = db.VRChatWorlds.AsNoTracking().FirstOrDefault(candidate => candidate.Id == id);
        return world is not null;
    }

    public Restaurant[] ListRestaurants() =>
        db.Restaurants
            .AsNoTracking()
            .OrderBy(restaurant => restaurant.Name)
            .ToArray();

    public bool TryGetRestaurant(Guid id, [NotNullWhen(true)] out Restaurant? restaurant)
    {
        restaurant = db.Restaurants.AsNoTracking().FirstOrDefault(candidate => candidate.Id == id);
        return restaurant is not null;
    }

    public Comment[] ListComments() =>
        db.Comments
            .AsNoTracking()
            .OrderBy(comment => comment.Id)
            .ToArray();

    public bool TryGetComment(Guid id, [NotNullWhen(true)] out Comment? comment)
    {
        comment = db.Comments.AsNoTracking().FirstOrDefault(candidate => candidate.Id == id);
        return comment is not null;
    }

    public void UpsertWorld(VRChatWorld world)
    {
        UpsertEntity(world);
    }

    public bool DeleteWorld(Guid id)
    {
        return DeleteEntity(db.VRChatWorlds, id);
    }

    public void UpsertRestaurant(Restaurant restaurant)
    {
        UpsertEntity(restaurant);
    }

    public bool DeleteRestaurant(Guid id)
    {
        return DeleteEntity(db.Restaurants, id);
    }

    public void UpsertComment(Comment comment)
    {
        UpsertEntity(comment);
    }

    public bool DeleteComment(Guid id)
    {
        return DeleteEntity(db.Comments, id);
    }

    public bool TryGet(Guid id, [NotNullWhen(true)] out Spot? spot)
    {
        spot = db.Spots.AsNoTracking().FirstOrDefault(candidate => candidate.Id == id);
        return spot is not null;
    }

    public bool Exists(Guid id) => db.Spots.AsNoTracking().Any(spot => spot.Id == id);

    public void Upsert(Spot spot)
    {
        UpsertEntity(spot);
    }

    public bool Delete(Guid id)
    {
        return DeleteEntity(db.Spots, id);
    }

    public void DeleteRelatedData(Guid spotId)
    {
        db.VRChatWorlds.Where(world => world.SpotId == spotId).ExecuteDelete();
        db.Restaurants.Where(restaurant => restaurant.SpotId == spotId).ExecuteDelete();
        db.Comments.Where(comment => comment.SpotId == spotId).ExecuteDelete();
    }

    private void UpsertEntity<TEntity>(TEntity entity)
        where TEntity : class
    {
        var id = (Guid)(typeof(TEntity).GetProperty(nameof(Spot.Id))?.GetValue(entity)
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} must expose an Id property."));

        var exists = db.Set<TEntity>()
            .AsNoTracking()
            .Any(candidate => EF.Property<Guid>(candidate, nameof(Spot.Id)) == id);

        if (exists)
        {
            db.Update(entity);
        }
        else
        {
            db.Add(entity);
        }

        db.SaveChanges();
        db.ChangeTracker.Clear();
    }

    private bool DeleteEntity<TEntity>(DbSet<TEntity> set, Guid id)
        where TEntity : class
    {
        var deleted = set.Where(entity => EF.Property<Guid>(entity, nameof(Spot.Id)) == id).ExecuteDelete();
        return deleted > 0;
    }
}
