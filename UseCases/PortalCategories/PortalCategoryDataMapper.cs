using VrcWebMap.Backend.Contracts.PortalCategories;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Resources;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.PortalCategories;

/// <summary>
/// カテゴリと配下ワールドを、現在ユーザー向けの公開DTOへ変換します。
/// </summary>
internal sealed class PortalCategoryDataMapper(
    ISpotRepository spots,
    IDiscordUserRepository users,
    CurrentActor? actor)
{
    private readonly PublicResourceMapper resources = new(users.List(), actor);

    public PortalCategoryData ToData(PortalCategory category)
    {
        var canEdit = actor is not null &&
            PortalCategoryAuthorization.CanMutate(category, actor);
        var worlds = spots.ListWorlds()
            .Where(world => world.PortalCategoryId == category.Id)
            .OrderBy(world => world.Name, StringComparer.Ordinal)
            .Select(world => resources.ToVRChatWorld(world, canEdit))
            .ToArray();

        return new PortalCategoryData(
            category.Id,
            category.Name,
            category.Visibility,
            resources.ResolveDisplayName(category.RegisteredByUserId),
            category.OwnerUserId is null
                ? null
                : resources.ResolveDisplayName(category.OwnerUserId),
            canEdit,
            worlds);
    }
}
