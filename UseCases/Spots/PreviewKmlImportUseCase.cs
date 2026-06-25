using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;

namespace VrcWebMap.Backend.UseCases.Spots;

[KawaUseCase(
    "spots.kml.preview",
    Summary = "Preview KML spot import",
    Description = "KML/KMZ ファイルから Spot import 候補を preview します。",
    Version = "v1",
    Tags = new[] { "Spot Management" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "KML/KMZ の入力値が不正です。")]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "管理者のみ KML/KMZ import preview を実行できます。")]
/// <summary>
/// KML/KMZ import 候補を preview するユースケースです。
/// </summary>
public sealed class PreviewKmlImportUseCase
    : IUseCase<PreviewKmlImport.Request, PreviewKmlImport.Response>
{
    public Task<KawaResult<PreviewKmlImport.Response>> ExecuteAsync(
        PreviewKmlImport.Request request,
        CancellationToken cancellationToken = default)
    {
        if (!request.ActorIsAdmin)
        {
            return Task.FromResult(KawaResult<PreviewKmlImport.Response>.Failure(
                new KawaError(KawaErrorKind.Forbidden, "KML/KMZ import preview は管理者のみ実行できます。")));
        }

        var parseResult = KmlSpotImportParser.Parse(request.FileName, request.ContentBase64, request.DefaultAreaCode);
        if (parseResult.ErrorMessage is not null)
        {
            return Task.FromResult(KawaResult<PreviewKmlImport.Response>.Failure(
                new KawaError(KawaErrorKind.Validation, parseResult.ErrorMessage)));
        }

        var response = new PreviewKmlImport.Response(
            parseResult.Items,
            parseResult.Warnings,
            parseResult.UnsupportedPlacemarkCount);
        return Task.FromResult(KawaResult<PreviewKmlImport.Response>.Success(response));
    }
}
