using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.PortalCategories;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.PortalCategories;

[KawaUseCase("portal-categories.update", Summary = "Update portal category", Version = "v1", Tags = new[] { "Portal Categories" })]
public sealed class UpdatePortalCategoryUseCase(
    IPortalCategoryRepository categories,
    ISpotRepository spots,
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
    : IUseCase<UpdatePortalCategory.Request, UpdatePortalCategory.Response>
{
    public Task<KawaResult<UpdatePortalCategory.Response>> ExecuteAsync(
        UpdatePortalCategory.Request request,
        CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Failure(actorError);
        }

        if (!categories.TryGet(request.Id, out var existing))
        {
            return Failure(KawaErrorKind.NotFound, "カテゴリが見つかりません。");
        }

        if (!PortalCategoryAuthorization.CanMutate(existing, actor!))
        {
            return Failure(KawaErrorKind.Forbidden, "カテゴリを変更する権限がありません。");
        }

        var nameError = PortalCategoryName.Validate(request.Name);
        if (nameError is not null)
        {
            return Failure(nameError);
        }

        var normalizedName = PortalCategoryName.Normalize(request.Name);
        if (categories.TryGetByNormalizedName(normalizedName, out var duplicate) &&
            duplicate.Id != request.Id)
        {
            return Failure(KawaErrorKind.Conflict, "同じ名前のカテゴリがすでに存在します。");
        }

        var updated = existing with
        {
            Name = request.Name.Trim(),
            NormalizedName = normalizedName
        };
        categories.Upsert(updated);

        var data = new PortalCategoryDataMapper(spots, users, actor).ToData(updated);
        return Task.FromResult(
            KawaResult<UpdatePortalCategory.Response>.Success(new(data)));
    }

    private static Task<KawaResult<UpdatePortalCategory.Response>> Failure(KawaError error) =>
        Task.FromResult(KawaResult<UpdatePortalCategory.Response>.Failure(error));

    private static Task<KawaResult<UpdatePortalCategory.Response>> Failure(
        KawaErrorKind kind,
        string message) =>
        Failure(new KawaError(kind, message));
}
