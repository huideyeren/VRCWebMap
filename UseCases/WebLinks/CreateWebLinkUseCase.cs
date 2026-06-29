using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.WebLinks;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.WebLinks;

[KawaUseCase(
    "web-links.create",
    Summary = "Create web link",
    Description = "指定されたスポットに Web サイト情報を追加します。",
    Version = "v1",
    Tags = new[] { "WebLinks" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "Web サイト情報の入力値が不正です。")]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "スポットが見つかりません。")]
public sealed class CreateWebLinkUseCase(
    ISpotRepository spots,
    ICurrentActorAccessor currentActor)
    : IUseCase<CreateWebLink.Request, CreateWebLink.Response>
{
    public Task<KawaResult<CreateWebLink.Response>> ExecuteAsync(
        CreateWebLink.Request request,
        CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Task.FromResult(KawaResult<CreateWebLink.Response>.Failure(actorError));
        }

        if (request.SpotId == Guid.Empty)
        {
            var error = new KawaError(KawaErrorKind.Validation, "スポット ID は必須です。");
            return Task.FromResult(KawaResult<CreateWebLink.Response>.Failure(error));
        }

        if (!spots.Exists(request.SpotId))
        {
            var error = new KawaError(KawaErrorKind.NotFound, "スポットが見つかりません。");
            return Task.FromResult(KawaResult<CreateWebLink.Response>.Failure(error));
        }

        if (string.IsNullOrWhiteSpace(request.SiteName))
        {
            var error = new KawaError(KawaErrorKind.Validation, "サイト名とURLは必須です。");
            return Task.FromResult(KawaResult<CreateWebLink.Response>.Failure(error));
        }

        var webLink = new WebLink(
            Guid.NewGuid(),
            request.SpotId,
            actor!.DiscordUserId,
            request.SiteName.Trim(),
            request.Url);

        spots.UpsertWebLink(webLink);

        var response = new CreateWebLink.Response(webLink);
        return Task.FromResult(KawaResult<CreateWebLink.Response>.Success(response));
    }
}
