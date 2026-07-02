using Kawa.Abstractions;
using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.UseCases.PortalCategories;

/// <summary>
/// 地域カテゴリとの衝突を避けながら、カテゴリ名を検証・正規化します。
/// </summary>
public static class PortalCategoryName
{
    public static string Normalize(string name) =>
        name.Trim().ToUpperInvariant();

    public static KawaError? Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new KawaError(KawaErrorKind.Validation, "カテゴリ名は必須です。");
        }

        var normalized = Normalize(name);
        if (AreaCategoryDisplayNames.All.Any(
                area => string.Equals(
                    Normalize(area.Name),
                    normalized,
                    StringComparison.Ordinal)))
        {
            return new KawaError(
                KawaErrorKind.Validation,
                "地域カテゴリと同じ名前は使用できません。");
        }

        return null;
    }
}
