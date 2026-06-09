using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.UseCases.VRChatWorlds;

[KawaUseCase("vrchat-worlds.delete", Summary = "Delete VRChat world", Version = "v1", Tags = new[] { "VRChat Worlds" })]
public sealed class DeleteVRChatWorldUseCase(ISpotRepository spots)
    : IUseCase<DeleteVRChatWorld.Request, DeleteVRChatWorld.Response>
{
    public Task<KawaResult<DeleteVRChatWorld.Response>> ExecuteAsync(DeleteVRChatWorld.Request request, CancellationToken cancellationToken = default)
    {
        if (!spots.TryGetWorld(request.Id, out var existing))
        {
            return Task.FromResult(KawaResult<DeleteVRChatWorld.Response>.Failure(new KawaError(KawaErrorKind.NotFound, "VRChat ワールド情報が見つかりません。")));
        }

        if (!SpotAuthorization.CanMutate(existing.RegisteredByUserId, request.ActorUserId, request.ActorIsAdmin))
        {
            return Task.FromResult(KawaResult<DeleteVRChatWorld.Response>.Failure(new KawaError(KawaErrorKind.Forbidden, "VRChat ワールド情報を削除する権限がありません。")));
        }

        spots.DeleteWorld(request.Id);
        return Task.FromResult(KawaResult<DeleteVRChatWorld.Response>.Success(new DeleteVRChatWorld.Response(request.Id)));
    }
}
