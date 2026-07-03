using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.PortalWorlds;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.PortalCategories;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.PortalWorlds;

[KawaUseCase("portal-worlds.create", Summary = "Create portal world", Version = "v1", Tags = new[] { "Portal Worlds" })]
public sealed class CreatePortalWorldUseCase(
    IPortalCategoryRepository categories,
    ISpotRepository spots,
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
    : IUseCase<CreatePortalWorld.Request, CreatePortalWorld.Response>
{
    public Task<KawaResult<CreatePortalWorld.Response>> ExecuteAsync(
        CreatePortalWorld.Request request,
        CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Failure(actorError);
        }

        if (!categories.TryGet(request.PortalCategoryId, out var category))
        {
            return Failure(KawaErrorKind.NotFound, "カテゴリが見つかりません。");
        }

        if (!PortalCategoryAuthorization.CanMutate(category, actor!))
        {
            return Failure(KawaErrorKind.Forbidden, "カテゴリへワールドを登録する権限がありません。");
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

        var world = new VRChatWorld(
            Guid.NewGuid(),
            SpotId: null,
            RegisteredByUserId: actor!.DiscordUserId,
            VRChatWorldId: request.VRChatWorldId.Trim(),
            Name: request.Name.Trim(),
            RecommendedCapacity: request.RecommendedCapacity,
            Capacity: request.Capacity,
            Description: request.Description.Trim(),
            PC: request.PC,
            Android: request.Android,
            IOS: request.IOS,
            IsPrivate: request.IsPrivate,
            PortalCategoryId: category.Id);
        spots.UpsertWorld(world);

        var data = PortalWorldResultMapper.ToData(world, category, users, actor);
        return Task.FromResult(
            KawaResult<CreatePortalWorld.Response>.Success(new(data)));
    }

    private static Task<KawaResult<CreatePortalWorld.Response>> Failure(KawaError error) =>
        Task.FromResult(KawaResult<CreatePortalWorld.Response>.Failure(error));

    private static Task<KawaResult<CreatePortalWorld.Response>> Failure(
        KawaErrorKind kind,
        string message) =>
        Failure(new KawaError(kind, message));
}
