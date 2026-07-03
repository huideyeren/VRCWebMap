using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Resources;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.Spots;

[KawaUseCase(
    "spots.kml.import",
    Summary = "Import KML spots",
    Description = "KML/KMZ ファイルから Spot を import します。",
    Version = "v1",
    Tags = new[] { "Spot Management" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "KML/KMZ の入力値が不正です。")]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "管理者のみ KML/KMZ import を実行できます。")]
/// <summary>
/// KML/KMZ から Spot を import するユースケースです。
/// </summary>
public sealed class ImportKmlSpotsUseCase(
    ISpotRepository spots,
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
    : IUseCase<ImportKmlSpots.Request, ImportKmlSpots.Response>
{
    public Task<KawaResult<ImportKmlSpots.Response>> ExecuteAsync(
        ImportKmlSpots.Request request,
        CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Task.FromResult(KawaResult<ImportKmlSpots.Response>.Failure(actorError));
        }

        if (!actor!.IsAdmin)
        {
            return Task.FromResult(KawaResult<ImportKmlSpots.Response>.Failure(
                new KawaError(KawaErrorKind.Forbidden, "KML/KMZ import は管理者のみ実行できます。")));
        }

        var parseResult = KmlSpotImportParser.Parse(request.FileName, request.ContentBase64, request.DefaultAreaCode);
        if (parseResult.ErrorMessage is not null)
        {
            return Task.FromResult(KawaResult<ImportKmlSpots.Response>.Failure(
                new KawaError(KawaErrorKind.Validation, parseResult.ErrorMessage)));
        }

        var importedSpots = new List<Spot>();
        foreach (var item in parseResult.Items)
        {
            var spot = new Spot(
                Guid.NewGuid(),
                actor.DiscordUserId,
                item.Name.Trim(),
                item.Latitude,
                item.Longitude,
                item.AreaCode,
                item.Description.Trim());
            spots.Upsert(spot);
            importedSpots.Add(spot);
        }

        var mapper = new PublicResourceMapper(users.List(), actor);
        var response = new ImportKmlSpots.Response(
            importedSpots.Select(spot => mapper.ToSpot(spot)).ToArray(),
            parseResult.Warnings,
            parseResult.UnsupportedPlacemarkCount);
        return Task.FromResult(KawaResult<ImportKmlSpots.Response>.Success(response));
    }
}
