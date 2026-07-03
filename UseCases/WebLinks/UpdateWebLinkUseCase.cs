using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.WebLinks;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Resources;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.WebLinks;

[KawaUseCase(
    "web-links.update",
    Summary = "Update web link",
    Description = "スポットに紐づく外部 Web サイト情報を更新します。OGP preview は保存せず、必要なときに /web-links/preview で取得します。",
    Version = "v1",
    Tags = new[] { "WebLinks" })]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "Web サイト情報が見つかりません。")]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "Web サイト情報を変更する権限がありません。")]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "Web サイト情報の入力値が不正です。")]
public sealed class UpdateWebLinkUseCase(
    ISpotRepository spots,
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
    : IUseCase<UpdateWebLink.Request, UpdateWebLink.Response>
{
    public Task<KawaResult<UpdateWebLink.Response>> ExecuteAsync(UpdateWebLink.Request request, CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Task.FromResult(KawaResult<UpdateWebLink.Response>.Failure(actorError));
        }

        if (!spots.TryGetWebLink(request.Id, out var existing))
        {
            return Task.FromResult(KawaResult<UpdateWebLink.Response>.Failure(new KawaError(KawaErrorKind.NotFound, "Web サイト情報が見つかりません。")));
        }

        if (!SpotAuthorization.CanMutate(existing.RegisteredByUserId, actor!.DiscordUserId, actor.IsAdmin))
        {
            return Task.FromResult(KawaResult<UpdateWebLink.Response>.Failure(new KawaError(KawaErrorKind.Forbidden, "Web サイト情報を変更する権限がありません。")));
        }

        if (string.IsNullOrWhiteSpace(request.SiteName))
        {
            return Task.FromResult(KawaResult<UpdateWebLink.Response>.Failure(new KawaError(KawaErrorKind.Validation, "サイト名、URL は必須です。")));
        }

        var webLink = new WebLink(
            existing.Id,
            existing.SpotId,
            existing.RegisteredByUserId,
            request.SiteName.Trim(),
            request.Url);

        spots.UpsertWebLink(webLink);
        var mapper = new PublicResourceMapper(users.List(), actor);
        return Task.FromResult(KawaResult<UpdateWebLink.Response>.Success(
            new UpdateWebLink.Response(mapper.ToWebLink(webLink))));
    }
}
