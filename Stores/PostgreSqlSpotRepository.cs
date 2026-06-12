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

    public PlaceInfo[] ListPlaceInfos() =>
        db.PlaceInfos
            .AsNoTracking()
            .OrderBy(placeInfo => placeInfo.Name)
            .ToArray();

    public bool TryGetPlaceInfo(Guid id, [NotNullWhen(true)] out PlaceInfo? placeInfo)
    {
        placeInfo = db.PlaceInfos.AsNoTracking().FirstOrDefault(candidate => candidate.Id == id);
        return placeInfo is not null;
    }

    public WebLink[] ListWebLinks() =>
        db.WebLinks
            .AsNoTracking()
            .OrderBy(webLink => webLink.SiteName)
            .ToArray();

    public bool TryGetWebLink(Guid id, [NotNullWhen(true)] out WebLink? webLink)
    {
        webLink = db.WebLinks.AsNoTracking().FirstOrDefault(candidate => candidate.Id == id);
        return webLink is not null;
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

    public void UpsertPlaceInfo(PlaceInfo placeInfo)
    {
        UpsertEntity(placeInfo);
    }

    public bool DeletePlaceInfo(Guid id)
    {
        return DeleteEntity(db.PlaceInfos, id);
    }

    public void UpsertWebLink(WebLink webLink)
    {
        UpsertEntity(webLink);
    }

    public bool DeleteWebLink(Guid id)
    {
        return DeleteEntity(db.WebLinks, id);
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
        db.PlaceInfos.Where(placeInfo => placeInfo.SpotId == spotId).ExecuteDelete();
        db.WebLinks.Where(webLink => webLink.SpotId == spotId).ExecuteDelete();
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
