using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.VRChatWorlds;

[KawaUseCase(
    "vrchat-worlds.delete",
    Summary = "Delete VRChat world",
    Description = "スポットに紐づく VRChat ワールド情報を削除します。削除は管理者のみ実行できます。",
    Version = "v1",
    Tags = new[] { "VRChat Worlds" })]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "VRChat ワールド情報が見つかりません。")]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "VRChat ワールド情報を削除する権限がありません。")]
public sealed class DeleteVRChatWorldUseCase(
    ISpotRepository spots,
    ICurrentActorAccessor currentActor)
    : IUseCase<DeleteVRChatWorld.Request, DeleteVRChatWorld.Response>
{
    public Task<KawaResult<DeleteVRChatWorld.Response>> ExecuteAsync(DeleteVRChatWorld.Request request, CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Task.FromResult(KawaResult<DeleteVRChatWorld.Response>.Failure(actorError));
        }

        if (!spots.TryGetWorld(request.Id, out var existing))
        {
            return Task.FromResult(KawaResult<DeleteVRChatWorld.Response>.Failure(new KawaError(KawaErrorKind.NotFound, "VRChat ワールド情報が見つかりません。")));
        }

        if (!SpotAuthorization.CanDelete(actor!.IsAdmin))
        {
            return Task.FromResult(KawaResult<DeleteVRChatWorld.Response>.Failure(new KawaError(KawaErrorKind.Forbidden, "VRChat ワールド情報を削除する権限がありません。")));
        }

        spots.DeleteWorld(request.Id);
        return Task.FromResult(KawaResult<DeleteVRChatWorld.Response>.Success(new DeleteVRChatWorld.Response(request.Id)));
    }
}
