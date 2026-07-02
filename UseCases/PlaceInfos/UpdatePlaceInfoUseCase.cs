using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.PlaceInfos;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Resources;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.PlaceInfos;

[KawaUseCase(
    "place-infos.update",
    Summary = "Update place info",
    Description = "スポットに紐づく場所情報を更新します。営業情報は Markdown 対応の自由記述テキストとして保存します。",
    Version = "v1",
    Tags = new[] { "PlaceInfos" })]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "場所情報が見つかりません。")]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "場所情報を変更する権限がありません。")]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "場所情報の入力値が不正です。")]
public sealed class UpdatePlaceInfoUseCase(
    ISpotRepository spots,
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
    : IUseCase<UpdatePlaceInfo.Request, UpdatePlaceInfo.Response>
{
    public Task<KawaResult<UpdatePlaceInfo.Response>> ExecuteAsync(UpdatePlaceInfo.Request request, CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Task.FromResult(KawaResult<UpdatePlaceInfo.Response>.Failure(actorError));
        }

        if (!spots.TryGetPlaceInfo(request.Id, out var existing))
        {
            return Task.FromResult(KawaResult<UpdatePlaceInfo.Response>.Failure(new KawaError(KawaErrorKind.NotFound, "場所情報が見つかりません。")));
        }

        if (!SpotAuthorization.CanMutate(existing.RegisteredByUserId, actor!.DiscordUserId, actor.IsAdmin))
        {
            return Task.FromResult(KawaResult<UpdatePlaceInfo.Response>.Failure(new KawaError(KawaErrorKind.Forbidden, "場所情報を変更する権限がありません。")));
        }

        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Address) ||
            string.IsNullOrWhiteSpace(request.BusinessInformation))
        {
            return Task.FromResult(KawaResult<UpdatePlaceInfo.Response>.Failure(new KawaError(KawaErrorKind.Validation, "場所名、所在地、営業情報は必須です。")));
        }

        var placeInfo = new PlaceInfo(
            existing.Id,
            existing.SpotId,
            existing.RegisteredByUserId,
            request.Name.Trim(),
            request.Address.Trim(),
            request.BusinessInformation.Trim());

        spots.UpsertPlaceInfo(placeInfo);
        var mapper = new PublicResourceMapper(users.List(), actor);
        return Task.FromResult(KawaResult<UpdatePlaceInfo.Response>.Success(
            new UpdatePlaceInfo.Response(mapper.ToPlaceInfo(placeInfo))));
    }
}
