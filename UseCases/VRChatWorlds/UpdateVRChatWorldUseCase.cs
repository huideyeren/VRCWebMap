using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Resources;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.VRChatWorlds;

[KawaUseCase(
    "vrchat-worlds.update",
    Summary = "Update VRChat world",
    Description = "スポットに紐づく VRChat ワールド情報を更新します。VRChat world ID、収容人数、対応プラットフォーム、公開状態を変更できます。",
    Version = "v1",
    Tags = new[] { "VRChat Worlds" })]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "VRChat ワールド情報が見つかりません。")]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "VRChat ワールド情報を変更する権限がありません。")]
public sealed class UpdateVRChatWorldUseCase(
    ISpotRepository spots,
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
    : IUseCase<UpdateVRChatWorld.Request, UpdateVRChatWorld.Response>
{
    public Task<KawaResult<UpdateVRChatWorld.Response>> ExecuteAsync(UpdateVRChatWorld.Request request, CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Task.FromResult(KawaResult<UpdateVRChatWorld.Response>.Failure(actorError));
        }

        if (!spots.TryGetWorld(request.Id, out var existing) ||
            existing.SpotId is null ||
            existing.PortalCategoryId is not null)
        {
            return Task.FromResult(KawaResult<UpdateVRChatWorld.Response>.Failure(new KawaError(KawaErrorKind.NotFound, "VRChat ワールド情報が見つかりません。")));
        }

        if (!SpotAuthorization.CanMutate(existing.RegisteredByUserId, actor!.DiscordUserId, actor.IsAdmin))
        {
            return Task.FromResult(KawaResult<UpdateVRChatWorld.Response>.Failure(new KawaError(KawaErrorKind.Forbidden, "VRChat ワールド情報を変更する権限がありません。")));
        }

        var world = existing with
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

        spots.UpsertWorld(world);
        var mapper = new PublicResourceMapper(users.List(), actor);
        return Task.FromResult(KawaResult<UpdateVRChatWorld.Response>.Success(
            new UpdateVRChatWorld.Response(mapper.ToVRChatWorld(world))));
    }
}
