using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.PortalWorlds;
using VrcWebMap.Backend.UseCases.PortalCategories;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.PortalWorlds;

[KawaUseCase("portal-worlds.update", Summary = "Update portal world", Version = "v1", Tags = new[] { "Portal Worlds" })]
public sealed class UpdatePortalWorldUseCase(
    IPortalCategoryRepository categories,
    ISpotRepository spots,
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
    : IUseCase<UpdatePortalWorld.Request, UpdatePortalWorld.Response>
{
    public Task<KawaResult<UpdatePortalWorld.Response>> ExecuteAsync(
        UpdatePortalWorld.Request request,
        CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Failure(actorError);
        }

        if (!TryGetPortalWorldAndCategory(request.Id, out var existing, out var category))
        {
            return Failure(KawaErrorKind.NotFound, "地図外ワールドが見つかりません。");
        }

        if (!PortalCategoryAuthorization.CanMutate(category!, actor!))
        {
            return Failure(KawaErrorKind.Forbidden, "地図外ワールドを変更する権限がありません。");
        }

        var validationError = PortalWorldValidation.Validate(
            request.VRChatWorldId,
            request.Name,
            request.RecommendedCapacity,
            request.Capacity,
            request.Description);
        if (validationError is not null)
        {
            return Failure(validationError);
        }

        var updated = existing! with
        {
            VRChatWorldId = request.VRChatWorldId.Trim(),
            Name = request.Name.Trim(),
            RecommendedCapacity = request.RecommendedCapacity,
            Capacity = request.Capacity,
            Description = request.Description.Trim(),
            PC = request.PC,
            Android = request.Android,
            IOS = request.IOS,
            IsPrivate = request.IsPrivate
        };
        spots.UpsertWorld(updated);

        var data = PortalWorldResultMapper.ToData(updated, category!, users, actor!);
        return Task.FromResult(
            KawaResult<UpdatePortalWorld.Response>.Success(new(data)));
    }

    private bool TryGetPortalWorldAndCategory(
        Guid id,
        out Models.VRChatWorld? world,
        out Models.PortalCategory? category)
    {
        category = null;
        if (!spots.TryGetWorld(id, out world) ||
            world.SpotId is not null ||
            world.PortalCategoryId is not Guid categoryId)
        {
            return false;
        }

        return categories.TryGet(categoryId, out category);
    }

    private static Task<KawaResult<UpdatePortalWorld.Response>> Failure(KawaError error) =>
        Task.FromResult(KawaResult<UpdatePortalWorld.Response>.Failure(error));

    private static Task<KawaResult<UpdatePortalWorld.Response>> Failure(
        KawaErrorKind kind,
        string message) =>
        Failure(new KawaError(kind, message));
}
