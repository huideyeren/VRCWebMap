using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.PortalCategories;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.PortalCategories;

[KawaUseCase("portal-categories.list", Summary = "List portal categories", Version = "v1", Tags = new[] { "Portal Categories" })]
public sealed class ListPortalCategoriesUseCase(
    IPortalCategoryRepository categories,
    ISpotRepository spots,
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
    : IUseCase<ListPortalCategories.Request, ListPortalCategories.Response>
{
    public Task<KawaResult<ListPortalCategories.Response>> ExecuteAsync(
        ListPortalCategories.Request request,
        CancellationToken cancellationToken = default)
    {
        var actor = currentActor.GetCurrent();
        var mapper = new PortalCategoryDataMapper(spots, users, actor);
        var visible = categories.List()
            .Where(category => CanView(category, actor))
            .Select(mapper.ToData)
            .ToArray();

        return Task.FromResult(
            KawaResult<ListPortalCategories.Response>.Success(new(visible)));
    }

    private static bool CanView(PortalCategory category, CurrentActor? actor) =>
        category.Visibility == PortalCategoryVisibility.Public ||
        actor?.IsAdmin == true ||
        string.Equals(
            category.OwnerUserId,
            actor?.DiscordUserId,
            StringComparison.Ordinal);
}
