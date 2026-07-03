using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Portal;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.PortalCategories;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.Portal;

[KawaUseCase(
    "portal.world-data",
    Summary = "Get portal world data",
    Description = "VRChat ワールドポータル向けの WorldData.json 形式データを返します。",
    Version = "v1",
    Tags = new[] { "Portal" })]
/// <summary>
/// VRChat ワールドポータル向け JSON を作成するユースケースです。
/// </summary>
public sealed class GetWorldDataUseCase(
    ISpotRepository spots,
    IPortalCategoryRepository portalCategories,
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
    : IUseCase<GetWorldData.Request, GetWorldData.Response>
{
    /// <summary>
    /// Spot の地域カテゴリごとに VRChat ワールドをまとめます。
    /// </summary>
    /// <param name="request">入力値を持たない出力要求です。</param>
    /// <param name="cancellationToken">キャンセル通知です。</param>
    /// <returns>WorldData.json 形式のデータを返します。</returns>
    public Task<KawaResult<GetWorldData.Response>> ExecuteAsync(
        GetWorldData.Request request,
        CancellationToken cancellationToken = default)
    {
        var spotById = spots.List().ToDictionary(spot => spot.Id);
        var areaByCode = AreaDefinitions.All.ToDictionary(area => area.AreaCode);
        var worlds = spots.ListWorlds()
            .Where(world =>
                world.SpotId.HasValue &&
                spotById.ContainsKey(world.SpotId.Value))
            .ToArray();

        var regionalCategorys = worlds
            .GroupBy(world => areaByCode[spotById[world.SpotId!.Value].AreaCode].Category)
            .OrderBy(group => AreaCategoryDisplayNames.OrderOf(group.Key))
            .Select(group => new GetWorldData.Category(
                AreaCategoryDisplayNames.Get(group.Key),
                group
                    .OrderBy(world => world.Name, StringComparer.Ordinal)
                    .Select(ToWorld)
                    .ToArray()))
            .ToArray();

        var actor = currentActor.GetCurrent();
        var publicCategorys = portalCategories.List()
            .Where(category => category.Visibility == PortalCategoryVisibility.Public)
            .Select(category => ToPortalCategory(category, PermittedRoles: null))
            .ToArray();

        var displayName = ResolveCurrentDisplayName(actor);
        var personalCategorys = actor is null || displayName is null
            ? []
            : portalCategories.List()
                .Where(category =>
                    category.Visibility == PortalCategoryVisibility.Personal &&
                    string.Equals(
                        category.OwnerUserId,
                        actor.DiscordUserId,
                        StringComparison.Ordinal))
                .Select(category => ToPortalCategory(category, [displayName]))
                .ToArray();

        var roles = personalCategorys.Length == 0
            ? null
            : new[] { new GetWorldData.Role(displayName!, [displayName!]) };
        var categorys = regionalCategorys
            .Concat(publicCategorys)
            .Concat(personalCategorys)
            .ToArray();

        var response = new GetWorldData.Response(
            ReverseCategorys: false,
            ShowPrivateWorld: true,
            Categorys: categorys,
            Roles: roles);

        return Task.FromResult(KawaResult<GetWorldData.Response>.Success(response));
    }

    private static GetWorldData.World ToWorld(VRChatWorld world) =>
        new(
            world.VRChatWorldId,
            world.Name,
            world.RecommendedCapacity,
            world.Capacity,
            world.Description,
            new GetWorldData.Platform(world.PC, world.Android, world.IOS),
            world.ReleaseStatus);

    private GetWorldData.Category ToPortalCategory(
        PortalCategory category,
        string[]? PermittedRoles) =>
        new(
            category.Name,
            spots.ListWorlds()
                .Where(world =>
                    world.SpotId is null &&
                    world.PortalCategoryId == category.Id)
                .OrderBy(world => world.Name, StringComparer.Ordinal)
                .Select(ToWorld)
                .ToArray(),
            PermittedRoles);

    private string? ResolveCurrentDisplayName(CurrentActor? actor)
    {
        if (actor is null ||
            !users.TryGetByDiscordUserId(actor.DiscordUserId, out var user) ||
            string.IsNullOrWhiteSpace(user.VRChatDisplayName))
        {
            return null;
        }

        return user.VRChatDisplayName;
    }

}
