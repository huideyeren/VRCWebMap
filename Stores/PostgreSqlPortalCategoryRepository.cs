using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.PortalCategories;

namespace VrcWebMap.Backend.Stores;

/// <summary>
/// PostgreSQLを使うポータルカテゴリリポジトリです。
/// </summary>
public sealed class PostgreSqlPortalCategoryRepository(AppDbContext db)
    : IPortalCategoryRepository
{
    public PortalCategory[] List() =>
        db.PortalCategories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToArray();

    public bool TryGet(Guid id, [NotNullWhen(true)] out PortalCategory? category)
    {
        category = db.PortalCategories
            .AsNoTracking()
            .FirstOrDefault(candidate => candidate.Id == id);
        return category is not null;
    }

    public bool TryGetByNormalizedName(
        string normalizedName,
        [NotNullWhen(true)] out PortalCategory? category)
    {
        category = db.PortalCategories
            .AsNoTracking()
            .FirstOrDefault(candidate => candidate.NormalizedName == normalizedName);
        return category is not null;
    }

    public void Upsert(PortalCategory category)
    {
        var exists = db.PortalCategories
            .AsNoTracking()
            .Any(candidate => candidate.Id == category.Id);

        if (exists)
        {
            db.Update(category);
        }
        else
        {
            db.Add(category);
        }

        db.SaveChanges();
        db.ChangeTracker.Clear();
    }

    public bool Delete(Guid id) =>
        db.PortalCategories
            .Where(category => category.Id == id)
            .ExecuteDelete() > 0;
}
