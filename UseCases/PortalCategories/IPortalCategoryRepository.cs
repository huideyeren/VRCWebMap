using System.Diagnostics.CodeAnalysis;
using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.UseCases.PortalCategories;

/// <summary>
/// 地図外ワールド用カテゴリの永続化境界です。
/// </summary>
public interface IPortalCategoryRepository
{
    PortalCategory[] List();

    bool TryGet(Guid id, [NotNullWhen(true)] out PortalCategory? category);

    bool TryGetByNormalizedName(
        string normalizedName,
        [NotNullWhen(true)] out PortalCategory? category);

    void Upsert(PortalCategory category);

    bool Delete(Guid id);
}
