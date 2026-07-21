using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.Spots;

[KawaUseCase(
    "spots.kml.preview",
    Summary = "Preview KML spot import",
    Description = "KML/KMZ ファイルから Spot import 候補を preview します。",
    Version = "v1",
    Tags = new[] { "Spot Management" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "KML/KMZ の入力値が不正です。")]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "VRChat表示名を登録したログインユーザーのみ KML/KMZ import preview を実行できます。")]
/// <summary>
/// KML/KMZ import 候補を preview するユースケースです。
/// </summary>
public sealed class PreviewKmlImportUseCase(
    ISpotRepository spots,
    ICurrentActorAccessor currentActor)
    : IUseCase<PreviewKmlImport.Request, PreviewKmlImport.Response>
{
    public Task<KawaResult<PreviewKmlImport.Response>> ExecuteAsync(
        PreviewKmlImport.Request request,
        CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Task.FromResult(KawaResult<PreviewKmlImport.Response>.Failure(actorError));
        }

        var parseResult = KmlSpotImportParser.Parse(request.FileName, request.ContentBase64, request.DefaultAreaCode);
        if (parseResult.ErrorMessage is not null)
        {
            return Task.FromResult(KawaResult<PreviewKmlImport.Response>.Failure(
                new KawaError(KawaErrorKind.Validation, parseResult.ErrorMessage)));
        }

        var existingSpots = spots.List();
        var items = parseResult.Items
            .Select(item =>
            {
                var nearbySpots = KmlSpotDuplicateMatcher.FindNearDuplicates(item, existingSpots);
                return item with
                {
                    NearbySpots = nearbySpots,
                    IsSelectedByDefault = nearbySpots.Length == 0
                };
            })
            .ToArray();
        var response = new PreviewKmlImport.Response(
            items,
            parseResult.Warnings,
            parseResult.UnsupportedPlacemarkCount);
        return Task.FromResult(KawaResult<PreviewKmlImport.Response>.Success(response));
    }
}
