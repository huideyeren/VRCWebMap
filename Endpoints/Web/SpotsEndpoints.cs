using Kawa.Web;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.Endpoints.Web;

/// <summary>
/// スポット管理ユースケースを Web エンドポイントとして公開します。
/// </summary>
public static class SpotsEndpoints
{
    /// <summary>
    /// スポット管理用の Kawa エンドポイントを登録します。
    /// </summary>
    /// <param name="endpoints">エンドポイントルートビルダーです。</param>
    /// <returns>登録後のエンドポイントルートビルダーです。</returns>
    public static IEndpointRouteBuilder MapSpots(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapKawaPost<ListSpotsUseCase>("/spots/list")
            .WithName("ListSpots")
            .WithContractOpenApi<ListSpots.Request, ListSpots.Response>();
        endpoints.MapKawaPost<GetSpotUseCase>("/spots/get")
            .WithName("GetSpot")
            .WithContractOpenApi<GetSpot.Request, GetSpot.Response>();
        endpoints.MapKawaPost<CreateSpotUseCase>("/spots/create")
            .WithName("CreateSpot")
            .WithContractOpenApi<CreateSpot.Request, CreateSpot.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<UpdateSpotUseCase>("/spots/update")
            .WithName("UpdateSpot")
            .WithContractOpenApi<UpdateSpot.Request, UpdateSpot.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<DeleteSpotUseCase>("/spots/delete")
            .WithName("DeleteSpot")
            .WithContractOpenApi<DeleteSpot.Request, DeleteSpot.Response>()
            .RequireAuthorization();

        return endpoints;
    }
}
