using Kawa.Abstractions;
using VRCWebMapBackend.Contracts.Spots;
using VRCWebMapBackend.Models;

namespace VRCWebMapBackend.UseCases.Spots;

[KawaUseCase(
    "spots.create",
    Summary = "Create spot",
    Description = "スポット管理用のレコードを作成します。",
    Version = "v1",
    Tags = new[] { "Spot Management" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "スポットの入力値が不正です。")]
/// <summary>
/// スポットを新規作成するユースケースです。
/// </summary>
public sealed class CreateSpotUseCase(ISpotRepository spots)
    : IUseCase<CreateSpot.Request, CreateSpot.Response>
{
    /// <summary>
    /// 入力値を検証し、スポットを新規作成します。
    /// </summary>
    /// <param name="request">スポット作成リクエストです。</param>
    /// <param name="cancellationToken">キャンセル通知です。</param>
    /// <returns>作成されたスポット、または検証エラーを返します。</returns>
    public Task<KawaResult<CreateSpot.Response>> ExecuteAsync(
        CreateSpot.Request request,
        CancellationToken cancellationToken = default)
    {
        var validationError = SpotValidation.Validate(
            request.Name,
            request.Latitude,
            request.Longitude,
            request.Description);

        if (validationError is not null)
        {
            return Task.FromResult(KawaResult<CreateSpot.Response>.Failure(validationError));
        }

        var spot = new Spot(
            Guid.NewGuid(),
            request.Name.Trim(),
            request.Latitude,
            request.Longitude,
            request.Description.Trim());

        spots.Upsert(spot);

        var response = new CreateSpot.Response(spot);
        return Task.FromResult(KawaResult<CreateSpot.Response>.Success(response));
    }
}
