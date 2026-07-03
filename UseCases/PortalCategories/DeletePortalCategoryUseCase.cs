using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.PortalCategories;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.PortalCategories;

[KawaUseCase("portal-categories.delete", Summary = "Delete portal category", Version = "v1", Tags = new[] { "Portal Categories" })]
public sealed class DeletePortalCategoryUseCase(
    IPortalCategoryRepository categories,
    ISpotRepository spots,
    ICurrentActorAccessor currentActor)
    : IUseCase<DeletePortalCategory.Request, DeletePortalCategory.Response>
{
    public Task<KawaResult<DeletePortalCategory.Response>> ExecuteAsync(
        DeletePortalCategory.Request request,
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
            return Failure(KawaErrorKind.Forbidden, "カテゴリを削除する権限がありません。");
        }

        if (spots.ListWorlds().Any(world => world.PortalCategoryId == request.Id))
        {
            return Failure(
                KawaErrorKind.Conflict,
                "カテゴリにワールドがあるため削除できません。先にワールドを削除してください。");
        }

        categories.Delete(request.Id);
        return Task.FromResult(
            KawaResult<DeletePortalCategory.Response>.Success(new(request.Id)));
    }

    private static Task<KawaResult<DeletePortalCategory.Response>> Failure(KawaError error) =>
        Task.FromResult(KawaResult<DeletePortalCategory.Response>.Failure(error));

    private static Task<KawaResult<DeletePortalCategory.Response>> Failure(
        KawaErrorKind kind,
        string message) =>
        Failure(new KawaError(kind, message));
}
