using Kawa.Web;
using VrcWebMap.Backend.Contracts.Areas;
using VrcWebMap.Backend.UseCases.Areas;

namespace VrcWebMap.Backend.Endpoints.Web;

/// <summary>
/// エリア定義ユースケースを Web エンドポイントとして公開します。
/// </summary>
public static class AreasEndpoints
{
    /// <summary>
    /// エリア定義用の Kawa エンドポイントを登録します。
    /// </summary>
    /// <param name="endpoints">エンドポイントルートビルダーです。</param>
    /// <returns>登録後のエンドポイントルートビルダーです。</returns>
    public static IEndpointRouteBuilder MapAreas(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapKawaPost<ListAreasUseCase>("/areas/list")
            .WithName("ListAreas")
            .WithContractOpenApi<ListAreas.Request, ListAreas.Response>();

        return endpoints;
    }
}
