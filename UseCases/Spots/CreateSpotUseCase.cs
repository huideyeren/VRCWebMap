using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Resources;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.Spots;

[KawaUseCase(
    "spots.create",
    Summary = "Create spot",
    Description = "スポット管理用のレコードを作成します。",
    Version = "v1",
    Tags = new[] { "Spot Management" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "スポットの入力値が不正です。")]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "DiscordログインとVRChat表示名の登録が必要です。")]
/// <summary>
/// スポットを新規作成するユースケースです。
/// </summary>
public sealed class CreateSpotUseCase(
    ISpotRepository spots,
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
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
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Task.FromResult(KawaResult<CreateSpot.Response>.Failure(actorError));
        }

        var validationError = SpotValidation.Validate(
            actor!.DiscordUserId,
            request.Name,
            request.Latitude,
            request.Longitude,
            request.AreaCode,
            request.Description);

        if (validationError is not null)
        {
            return Task.FromResult(KawaResult<CreateSpot.Response>.Failure(validationError));
        }

        var spot = new Spot(
            Guid.NewGuid(),
            actor.DiscordUserId,
            request.Name.Trim(),
            request.Latitude,
            request.Longitude,
            request.AreaCode,
            request.Description.Trim());

        spots.Upsert(spot);

        var mapper = new PublicResourceMapper(users.List(), actor);
        var response = new CreateSpot.Response(mapper.ToSpot(spot));
        return Task.FromResult(KawaResult<CreateSpot.Response>.Success(response));
    }
}
