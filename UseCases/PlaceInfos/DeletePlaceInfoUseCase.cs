using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.PlaceInfos;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.UseCases.PlaceInfos;

[KawaUseCase(
    "place-infos.delete",
    Summary = "Delete place info",
    Description = "スポットに紐づく場所情報を削除します。管理者または場所情報の登録者本人のみ実行できます。",
    Version = "v1",
    Tags = new[] { "PlaceInfos" })]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "場所情報が見つかりません。")]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "場所情報を削除する権限がありません。")]
public sealed class DeletePlaceInfoUseCase(ISpotRepository spots)
    : IUseCase<DeletePlaceInfo.Request, DeletePlaceInfo.Response>
{
    public Task<KawaResult<DeletePlaceInfo.Response>> ExecuteAsync(DeletePlaceInfo.Request request, CancellationToken cancellationToken = default)
    {
        if (!spots.TryGetPlaceInfo(request.Id, out var existing))
        {
            return Task.FromResult(KawaResult<DeletePlaceInfo.Response>.Failure(new KawaError(KawaErrorKind.NotFound, "場所情報が見つかりません。")));
        }

        if (!SpotAuthorization.CanMutate(existing.RegisteredByUserId, request.ActorUserId, request.ActorIsAdmin))
        {
            return Task.FromResult(KawaResult<DeletePlaceInfo.Response>.Failure(new KawaError(KawaErrorKind.Forbidden, "場所情報を削除する権限がありません。")));
        }

        spots.DeletePlaceInfo(request.Id);
        return Task.FromResult(KawaResult<DeletePlaceInfo.Response>.Success(new DeletePlaceInfo.Response(request.Id)));
    }
}
