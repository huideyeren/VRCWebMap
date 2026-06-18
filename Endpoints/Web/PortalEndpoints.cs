using Kawa.Web;
using VrcWebMap.Backend.Contracts.Portal;
using VrcWebMap.Backend.UseCases.Portal;

namespace VrcWebMap.Backend.Endpoints.Web;

/// <summary>
/// ポータル出力ユースケースを Web エンドポイントとして公開します。
/// </summary>
public static class PortalEndpoints
{
    /// <summary>
    /// ポータル出力用の Kawa エンドポイントを登録します。
    /// </summary>
    /// <param name="endpoints">エンドポイントルートビルダーです。</param>
    /// <returns>登録後のエンドポイントルートビルダーです。</returns>
    public static IEndpointRouteBuilder MapPortal(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapKawaPost<GetWorldDataUseCase>("/portal/world-data")
            .WithName("GetPortalWorldData")
            .WithContractOpenApi<GetWorldData.Request, GetWorldData.Response>();

        return endpoints;
    }
}
