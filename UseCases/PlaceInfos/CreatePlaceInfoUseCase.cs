using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.PlaceInfos;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.PlaceInfos;

[KawaUseCase(
    "place-infos.create",
    Summary = "Create place info",
    Description = "指定されたスポットに場所情報を追加します。",
    Version = "v1",
    Tags = new[] { "PlaceInfos" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "場所情報の入力値が不正です。")]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "スポットが見つかりません。")]
/// <summary>
/// スポットに場所情報を追加するユースケースです。
/// </summary>
public sealed class CreatePlaceInfoUseCase(
    ISpotRepository spots,
    ICurrentActorAccessor currentActor)
    : IUseCase<CreatePlaceInfo.Request, CreatePlaceInfo.Response>
{
    /// <summary>
    /// 指定されたスポットに場所情報を追加します。
    /// </summary>
    public Task<KawaResult<CreatePlaceInfo.Response>> ExecuteAsync(
        CreatePlaceInfo.Request request,
        CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Task.FromResult(KawaResult<CreatePlaceInfo.Response>.Failure(actorError));
        }

        if (request.SpotId == Guid.Empty)
        {
            var error = new KawaError(KawaErrorKind.Validation, "スポット ID は必須です。");
            return Task.FromResult(KawaResult<CreatePlaceInfo.Response>.Failure(error));
        }

        if (!spots.Exists(request.SpotId))
        {
            var error = new KawaError(KawaErrorKind.NotFound, "スポットが見つかりません。");
            return Task.FromResult(KawaResult<CreatePlaceInfo.Response>.Failure(error));
        }

        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Address) ||
            string.IsNullOrWhiteSpace(request.BusinessInformation))
        {
            var error = new KawaError(KawaErrorKind.Validation, "場所名、所在地、営業情報は必須です。");
            return Task.FromResult(KawaResult<CreatePlaceInfo.Response>.Failure(error));
        }

        var placeInfo = new PlaceInfo(
            Guid.NewGuid(),
            request.SpotId,
            actor!.DiscordUserId,
            request.Name.Trim(),
            request.Address.Trim(),
            request.BusinessInformation.Trim());

        spots.UpsertPlaceInfo(placeInfo);

        var response = new CreatePlaceInfo.Response(placeInfo);
        return Task.FromResult(KawaResult<CreatePlaceInfo.Response>.Success(response));
    }
}
