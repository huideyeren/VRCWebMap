using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.UseCases.VRChatWorlds;

[KawaUseCase("vrchat-worlds.update", Summary = "Update VRChat world", Version = "v1", Tags = new[] { "VRChat Worlds" })]
public sealed class UpdateVRChatWorldUseCase(ISpotRepository spots)
    : IUseCase<UpdateVRChatWorld.Request, UpdateVRChatWorld.Response>
{
    public Task<KawaResult<UpdateVRChatWorld.Response>> ExecuteAsync(UpdateVRChatWorld.Request request, CancellationToken cancellationToken = default)
    {
        if (!spots.TryGetWorld(request.Id, out var existing))
        {
            return Task.FromResult(KawaResult<UpdateVRChatWorld.Response>.Failure(new KawaError(KawaErrorKind.NotFound, "VRChat ワールド情報が見つかりません。")));
        }

        if (!SpotAuthorization.CanMutate(existing.RegisteredByUserId, request.ActorUserId, request.ActorIsAdmin))
        {
            return Task.FromResult(KawaResult<UpdateVRChatWorld.Response>.Failure(new KawaError(KawaErrorKind.Forbidden, "VRChat ワールド情報を変更する権限がありません。")));
        }

        var world = new VRChatWorld(
            existing.Id,
            existing.SpotId,
            existing.RegisteredByUserId,
            request.VRChatWorldId.Trim(),
            request.Name.Trim(),
            request.RecommendedCapacity,
            request.Capacity,
            request.Description.Trim(),
            request.PC,
            request.Android,
            request.IOS,
            request.IsPrivate);

        spots.UpsertWorld(world);
        return Task.FromResult(KawaResult<UpdateVRChatWorld.Response>.Success(new UpdateVRChatWorld.Response(world)));
    }
}
