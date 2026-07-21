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
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "VRChat表示名を登録したログインユーザーのみ KML/KMZ import を実行できます。")]
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

        var parseResult = KmlSpotImportParser.Parse(request.FileName, request.ContentBase64, request.DefaultAreaCode);
        if (parseResult.ErrorMessage is not null)
        {
            return Task.FromResult(KawaResult<ImportKmlSpots.Response>.Failure(
                new KawaError(KawaErrorKind.Validation, parseResult.ErrorMessage)));
        }

        var selectedIndexes = request.SelectedSourceIndexes.Distinct().ToHashSet();
        if (selectedIndexes.Count != request.SelectedSourceIndexes.Length ||
            selectedIndexes.Any(index => parseResult.Items.All(item => item.SourceIndex != index)))
        {
            return Task.FromResult(KawaResult<ImportKmlSpots.Response>.Failure(
                new KawaError(KawaErrorKind.Validation, "選択した KML 候補が入力ファイルと一致しません。")));
        }

        var selectedItems = parseResult.Items.Where(item => selectedIndexes.Contains(item.SourceIndex)).ToArray();
        var confirmationGroups = request.Confirmations.GroupBy(item => item.SourceIndex).ToArray();
        if (confirmationGroups.Any(group => group.Count() != 1 || !selectedIndexes.Contains(group.Key)))
        {
            return Task.FromResult(KawaResult<ImportKmlSpots.Response>.Failure(
                new KawaError(KawaErrorKind.Validation, "重複候補の確認情報が選択した KML 候補と一致しません。")));
        }

        var confirmationByIndex = confirmationGroups.ToDictionary(
            group => group.Key,
            group => group.Single().NearbySpotIds.ToHashSet());
        var existingSpots = spots.List();
        var reconfirmationItems = selectedItems
            .Where(item => KmlSpotDuplicateMatcher.FindNearDuplicates(item, existingSpots)
                .Any(nearby => !confirmationByIndex.GetValueOrDefault(item.SourceIndex, []).Contains(nearby.Id)))
            .Select(item =>
            {
                var nearbySpots = KmlSpotDuplicateMatcher.FindNearDuplicates(item, existingSpots);
                return item with { NearbySpots = nearbySpots, IsSelectedByDefault = false };
            })
            .ToArray();
        if (reconfirmationItems.Length > 0)
        {
            return Task.FromResult(KawaResult<ImportKmlSpots.Response>.Success(
                new([], parseResult.Warnings, parseResult.UnsupportedPlacemarkCount, reconfirmationItems)));
        }

        var importedSpots = new List<Spot>();
        foreach (var item in selectedItems)
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
            parseResult.UnsupportedPlacemarkCount,
            []);
        return Task.FromResult(KawaResult<ImportKmlSpots.Response>.Success(response));
    }
}
