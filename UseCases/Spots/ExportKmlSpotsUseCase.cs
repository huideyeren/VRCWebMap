using System.Xml.Linq;
using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Options;
using VrcWebMap.Backend.UseCases.Resources;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.Spots;

public sealed class ExportKmlSpotsUseCase(ISpotRepository spots, IDiscordUserRepository users, AppOptions options)
    : IUseCase<ExportKmlSpots.Request, ExportKmlSpots.Response>
{
    public Task<KawaResult<ExportKmlSpots.Response>> ExecuteAsync(ExportKmlSpots.Request request, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https"))
            return Task.FromResult(KawaResult<ExportKmlSpots.Response>.Failure(new(KawaErrorKind.Validation, "App:PublicBaseUrl は絶対 HTTP(S) URL を設定してください。")));
        var byId = spots.List().ToDictionary(spot => spot.Id);
        var selected = request.SpotIds.Distinct().Where(byId.ContainsKey).Select(id => byId[id]).ToArray();
        var missing = request.SpotIds.Distinct().Where(id => !byId.ContainsKey(id)).ToArray();
        var mapper = new PublicResourceMapper(users.List(), null);
        XNamespace kml = "http://www.opengis.net/kml/2.2";
        var document = new XDocument(new XDeclaration("1.0", "UTF-8", null), new XElement(kml + "kml", new XElement(kml + "Document",
            selected.Select(spot => new XElement(kml + "Placemark", new XElement(kml + "name", spot.Name), new XElement(kml + "description", $"{spot.Description}\n\n出典: VRC Web Map\n登録者: {mapper.ResolveDisplayName(spot.RegisteredByUserId)}\n元Spot: {options.PublicBaseUrl.TrimEnd('/')}/?spotId={spot.Id}"), new XElement(kml + "ExtendedData", new XElement(kml + "Data", new XAttribute("name", "vrcwebmap:spotId"), new XElement(kml + "value", spot.Id))), new XElement(kml + "Point", new XElement(kml + "coordinates", $"{spot.Longitude},{spot.Latitude},0")))))));
        return Task.FromResult(KawaResult<ExportKmlSpots.Response>.Success(new("VrcWebMap-spots.kml", document.ToString(), missing)));
    }
}
