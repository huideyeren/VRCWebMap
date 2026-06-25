using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;

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
public sealed class ImportKmlSpotsUseCase(ISpotRepository spots)
    : IUseCase<ImportKmlSpots.Request, ImportKmlSpots.Response>
{
    public Task<KawaResult<ImportKmlSpots.Response>> ExecuteAsync(
        ImportKmlSpots.Request request,
        CancellationToken cancellationToken = default)
    {
        if (!request.ActorIsAdmin)
        {
            return Task.FromResult(KawaResult<ImportKmlSpots.Response>.Failure(
                new KawaError(KawaErrorKind.Forbidden, "KML/KMZ import は管理者のみ実行できます。")));
        }

        if (string.IsNullOrWhiteSpace(request.ActorUserId))
        {
            return Task.FromResult(KawaResult<ImportKmlSpots.Response>.Failure(
                new KawaError(KawaErrorKind.Validation, "操作ユーザー ID は必須です。")));
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
                request.ActorUserId.Trim(),
                item.Name.Trim(),
                item.Latitude,
                item.Longitude,
                item.AreaCode,
                item.Description.Trim());
            spots.Upsert(spot);
            importedSpots.Add(spot);
        }

        var response = new ImportKmlSpots.Response(
            importedSpots.ToArray(),
            parseResult.Warnings,
            parseResult.UnsupportedPlacemarkCount);
        return Task.FromResult(KawaResult<ImportKmlSpots.Response>.Success(response));
    }
}
