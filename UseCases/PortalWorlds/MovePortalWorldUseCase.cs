using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.PortalWorlds;
using VrcWebMap.Backend.UseCases.PortalCategories;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.PortalWorlds;

[KawaUseCase("portal-worlds.move", Summary = "Move portal world", Version = "v1", Tags = new[] { "Portal Worlds" })]
public sealed class MovePortalWorldUseCase(
    IPortalCategoryRepository categories,
    ISpotRepository spots,
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
    : IUseCase<MovePortalWorld.Request, MovePortalWorld.Response>
{
    public Task<KawaResult<MovePortalWorld.Response>> ExecuteAsync(
        MovePortalWorld.Request request,
        CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Failure(actorError);
        }

        if (!spots.TryGetWorld(request.Id, out var world) ||
            world.SpotId is not null ||
            world.PortalCategoryId is not Guid sourceId ||
            !categories.TryGet(sourceId, out var source))
        {
            return Failure(KawaErrorKind.NotFound, "地図外ワールドが見つかりません。");
        }

        if (!categories.TryGet(request.DestinationPortalCategoryId, out var destination))
        {
            return Failure(KawaErrorKind.NotFound, "移動先カテゴリが見つかりません。");
        }

        if (!PortalCategoryAuthorization.CanMutate(source, actor!) ||
            !PortalCategoryAuthorization.CanMutate(destination, actor!))
        {
            return Failure(KawaErrorKind.Forbidden, "カテゴリ間でワールドを移動する権限がありません。");
        }

        var moved = world with { PortalCategoryId = destination.Id };
        spots.UpsertWorld(moved);
        var data = PortalWorldResultMapper.ToData(moved, destination, users, actor!);
        return Task.FromResult(
            KawaResult<MovePortalWorld.Response>.Success(new(data)));
    }

    private static Task<KawaResult<MovePortalWorld.Response>> Failure(KawaError error) =>
        Task.FromResult(KawaResult<MovePortalWorld.Response>.Failure(error));

    private static Task<KawaResult<MovePortalWorld.Response>> Failure(
        KawaErrorKind kind,
        string message) =>
        Failure(new KawaError(kind, message));
}
