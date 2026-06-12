using VrcWebMap.Backend.Contracts.WebLinks;

namespace VrcWebMap.Backend.UseCases.WebLinks;

public interface IOpenGraphPreviewProvider
{
    Task<GetWebLinkPreview.Preview?> TryGetPreviewAsync(Uri url, CancellationToken cancellationToken);
}
