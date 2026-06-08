using Kawa.Web;
using VRCWebMapBackend.UseCases.Spots;

namespace VRCWebMapBackend.Endpoints.Web;

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
        endpoints.MapKawaPost<ListSpotsUseCase>("/spots/list").WithName("ListSpots");
        endpoints.MapKawaPost<GetSpotUseCase>("/spots/get").WithName("GetSpot");
        endpoints.MapKawaPost<CreateSpotUseCase>("/spots/create").WithName("CreateSpot");
        endpoints.MapKawaPost<UpdateSpotUseCase>("/spots/update").WithName("UpdateSpot");
        endpoints.MapKawaPost<DeleteSpotUseCase>("/spots/delete").WithName("DeleteSpot");

        return endpoints;
    }
}
