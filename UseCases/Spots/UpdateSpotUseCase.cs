using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.UseCases.Spots;

[KawaUseCase(
    "spots.update",
    Summary = "Update spot",
    Description = "既存のスポット管理レコードを更新します。",
    Version = "v1",
    Tags = new[] { "Spot Management" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "スポットの入力値が不正です。")]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "スポットが見つかりません。")]
/// <summary>
/// 既存スポットを更新するユースケースです。
/// </summary>
public sealed class UpdateSpotUseCase(ISpotRepository spots)
    : IUseCase<UpdateSpot.Request, UpdateSpot.Response>
{
    /// <summary>
    /// 指定された ID のスポットを更新します。
    /// </summary>
    /// <param name="request">更新対象 ID と更新後の値を含むリクエストです。</param>
    /// <param name="cancellationToken">キャンセル通知です。</param>
    /// <returns>更新されたスポット、検証エラー、または未検出エラーを返します。</returns>
    public Task<KawaResult<UpdateSpot.Response>> ExecuteAsync(
        UpdateSpot.Request request,
        CancellationToken cancellationToken = default)
    {
        if (!spots.TryGet(request.Id, out var existing))
        {
            var error = new KawaError(KawaErrorKind.NotFound, "スポットが見つかりません。");
            return Task.FromResult(KawaResult<UpdateSpot.Response>.Failure(error));
        }

        if (!SpotAuthorization.CanMutate(existing.RegisteredByUserId, request.ActorUserId, request.ActorIsAdmin))
        {
            var error = new KawaError(KawaErrorKind.Forbidden, "スポットを変更する権限がありません。");
            return Task.FromResult(KawaResult<UpdateSpot.Response>.Failure(error));
        }

        var validationError = SpotValidation.Validate(
            existing.RegisteredByUserId,
            request.Name,
            request.Latitude,
            request.Longitude,
            request.AreaCode,
            request.Description);

        if (validationError is not null)
        {
            return Task.FromResult(KawaResult<UpdateSpot.Response>.Failure(validationError));
        }

        var spot = new Spot(
            request.Id,
            existing.RegisteredByUserId,
            request.Name.Trim(),
            request.Latitude,
            request.Longitude,
            request.AreaCode,
            request.Description.Trim());

        spots.Upsert(spot);

        var response = new UpdateSpot.Response(spot);
        return Task.FromResult(KawaResult<UpdateSpot.Response>.Success(response));
    }
}
