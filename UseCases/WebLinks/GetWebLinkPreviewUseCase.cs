using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.WebLinks;

namespace VrcWebMap.Backend.UseCases.WebLinks;

[KawaUseCase(
    "web-links.preview",
    Summary = "Get web link preview",
    Description = "Web サイトの OGP preview を取得します。",
    Version = "v1",
    Tags = new[] { "WebLinks" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "URL が不正です。")]
public sealed class GetWebLinkPreviewUseCase(IOpenGraphPreviewProvider previews)
    : IUseCase<GetWebLinkPreview.Request, GetWebLinkPreview.Response>
{
    public async Task<KawaResult<GetWebLinkPreview.Response>> ExecuteAsync(
        GetWebLinkPreview.Request request,
        CancellationToken cancellationToken = default)
    {
        if (!request.Url.IsAbsoluteUri ||
            request.Url.Scheme is not ("http" or "https"))
        {
            var error = new KawaError(KawaErrorKind.Validation, "OGP preview を取得できる URL は http/https の絶対 URL のみです。");
            return KawaResult<GetWebLinkPreview.Response>.Failure(error);
        }

        var preview = await previews.TryGetPreviewAsync(request.Url, cancellationToken)
            ?? new GetWebLinkPreview.Preview(request.Url, Title: null, Description: null, ImageUrl: null, SiteName: null);

        return KawaResult<GetWebLinkPreview.Response>.Success(new GetWebLinkPreview.Response(preview));
    }
}
