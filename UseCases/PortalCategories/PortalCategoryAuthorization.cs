using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.PortalCategories;

/// <summary>
/// 公開カテゴリは管理者、個人カテゴリは所有者または管理者に変更を許可します。
/// </summary>
public static class PortalCategoryAuthorization
{
    public static bool CanMutate(PortalCategory category, CurrentActor actor) =>
        actor.IsAdmin ||
        category.Visibility == PortalCategoryVisibility.Personal &&
        string.Equals(
            category.OwnerUserId,
            actor.DiscordUserId,
            StringComparison.Ordinal);
}
