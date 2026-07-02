using System.Text;
using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Portal;

namespace VrcWebMap.Backend.UseCases.Portal;

[KawaUseCase(
    "portal.world-data.merge",
    Summary = "Merge portal world data",
    Description = "既存のWPPLS WorldData.jsonへ、現在閲覧できるシステムデータを追加します。",
    Version = "v1",
    Tags = new[] { "Portal" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "既存のWorldData.jsonが不正です。")]
public sealed class MergeWorldDataUseCase(GetWorldDataUseCase worldData)
    : IUseCase<MergeWorldData.Request, MergeWorldData.Response>
{
    private const int MaximumPayloadBytes = 5 * 1024 * 1024;

    public async Task<KawaResult<MergeWorldData.Response>> ExecuteAsync(
        MergeWorldData.Request request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ExistingJson))
        {
            return Failure("既存のWorldData.jsonを指定してください。");
        }

        if (Encoding.UTF8.GetByteCount(request.ExistingJson) > MaximumPayloadBytes)
        {
            return Failure("WorldData.jsonは5 MiB以下にしてください。");
        }

        var systemResult = await worldData.ExecuteAsync(
            new GetWorldData.Request(),
            cancellationToken);
        if (systemResult.IsFailure)
        {
            return KawaResult<MergeWorldData.Response>.Failure(systemResult.Error!);
        }

        var mergeResult = WorldDataJsonMerger.Merge(
            request.ExistingJson,
            systemResult.Value!);
        if (!mergeResult.IsSuccess)
        {
            return Failure(mergeResult.ErrorMessage!);
        }

        return KawaResult<MergeWorldData.Response>.Success(
            new MergeWorldData.Response(mergeResult.Json!));
    }

    private static KawaResult<MergeWorldData.Response> Failure(string message) =>
        KawaResult<MergeWorldData.Response>.Failure(
            new KawaError(KawaErrorKind.Validation, message));
}
