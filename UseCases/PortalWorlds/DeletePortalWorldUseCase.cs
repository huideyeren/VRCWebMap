using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.PortalWorlds;
using VrcWebMap.Backend.UseCases.PortalCategories;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.PortalWorlds;

[KawaUseCase("portal-worlds.delete", Summary = "Delete portal world", Version = "v1", Tags = new[] { "Portal Worlds" })]
public sealed class DeletePortalWorldUseCase(
    IPortalCategoryRepository categories,
    ISpotRepository spots,
    ICurrentActorAccessor currentActor)
    : IUseCase<DeletePortalWorld.Request, DeletePortalWorld.Response>
{
    public Task<KawaResult<DeletePortalWorld.Response>> ExecuteAsync(
        DeletePortalWorld.Request request,
        CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Failure(actorError);
        }

        if (!spots.TryGetWorld(request.Id, out var world) ||
            world.SpotId is not null ||
            world.PortalCategoryId is not Guid categoryId ||
            !categories.TryGet(categoryId, out var category))
        {
            return Failure(KawaErrorKind.NotFound, "地図外ワールドが見つかりません。");
        }

        if (!PortalCategoryAuthorization.CanMutate(category, actor!))
        {
            return Failure(KawaErrorKind.Forbidden, "地図外ワールドを削除する権限がありません。");
        }

        spots.DeleteWorld(request.Id);
        return Task.FromResult(
            KawaResult<DeletePortalWorld.Response>.Success(new(request.Id)));
    }

    private static Task<KawaResult<DeletePortalWorld.Response>> Failure(KawaError error) =>
        Task.FromResult(KawaResult<DeletePortalWorld.Response>.Failure(error));

    private static Task<KawaResult<DeletePortalWorld.Response>> Failure(
        KawaErrorKind kind,
        string message) =>
        Failure(new KawaError(kind, message));
}
