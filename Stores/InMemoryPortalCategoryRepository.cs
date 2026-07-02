using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.PortalCategories;

namespace VrcWebMap.Backend.Stores;

/// <summary>
/// 試作用のインメモリポータルカテゴリリポジトリです。
/// </summary>
public sealed class InMemoryPortalCategoryRepository : IPortalCategoryRepository
{
    private readonly ConcurrentDictionary<Guid, PortalCategory> categories = new();

    public PortalCategory[] List() =>
        categories.Values
            .OrderBy(category => category.Name, StringComparer.Ordinal)
            .ToArray();

    public bool TryGet(Guid id, [NotNullWhen(true)] out PortalCategory? category) =>
        categories.TryGetValue(id, out category);

    public bool TryGetByNormalizedName(
        string normalizedName,
        [NotNullWhen(true)] out PortalCategory? category)
    {
        category = categories.Values.FirstOrDefault(
            candidate => string.Equals(
                candidate.NormalizedName,
                normalizedName,
                StringComparison.Ordinal));
        return category is not null;
    }

    public void Upsert(PortalCategory category) =>
        categories[category.Id] = category;

    public bool Delete(Guid id) =>
        categories.TryRemove(id, out _);
}
