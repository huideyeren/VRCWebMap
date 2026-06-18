using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.WebLinks;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.UseCases.WebLinks;

[KawaUseCase(
    "web-links.delete",
    Summary = "Delete web link",
    Description = "スポットに紐づく外部 Web サイト情報を削除します。管理者または Web サイト情報の登録者本人のみ実行できます。",
    Version = "v1",
    Tags = new[] { "WebLinks" })]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "Web サイト情報が見つかりません。")]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "Web サイト情報を削除する権限がありません。")]
public sealed class DeleteWebLinkUseCase(ISpotRepository spots)
    : IUseCase<DeleteWebLink.Request, DeleteWebLink.Response>
{
    public Task<KawaResult<DeleteWebLink.Response>> ExecuteAsync(DeleteWebLink.Request request, CancellationToken cancellationToken = default)
    {
        if (!spots.TryGetWebLink(request.Id, out var existing))
        {
            return Task.FromResult(KawaResult<DeleteWebLink.Response>.Failure(new KawaError(KawaErrorKind.NotFound, "Web サイト情報が見つかりません。")));
        }

        if (!SpotAuthorization.CanMutate(existing.RegisteredByUserId, request.ActorUserId, request.ActorIsAdmin))
        {
            return Task.FromResult(KawaResult<DeleteWebLink.Response>.Failure(new KawaError(KawaErrorKind.Forbidden, "Web サイト情報を削除する権限がありません。")));
        }

        spots.DeleteWebLink(request.Id);
        return Task.FromResult(KawaResult<DeleteWebLink.Response>.Success(new DeleteWebLink.Response(request.Id)));
    }
}
