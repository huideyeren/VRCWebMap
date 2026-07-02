using Kawa.Web;
using VrcWebMap.Backend.Contracts.PortalCategories;
using VrcWebMap.Backend.Contracts.PortalWorlds;
using VrcWebMap.Backend.UseCases.PortalCategories;
using VrcWebMap.Backend.UseCases.PortalWorlds;

namespace VrcWebMap.Backend.Endpoints.Web;

/// <summary>
/// 地図外カテゴリと配下ワールドのユースケースをWebへ公開します。
/// </summary>
public static class PortalCategoryEndpoints
{
    public static IEndpointRouteBuilder MapPortalCategories(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapKawaPost<ListPortalCategoriesUseCase>("/portal-categories/list")
            .WithName("ListPortalCategories")
            .WithContractOpenApi<ListPortalCategories.Request, ListPortalCategories.Response>();
        endpoints.MapKawaPost<CreatePortalCategoryUseCase>("/portal-categories/create")
            .WithName("CreatePortalCategory")
            .WithContractOpenApi<CreatePortalCategory.Request, CreatePortalCategory.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<UpdatePortalCategoryUseCase>("/portal-categories/update")
            .WithName("UpdatePortalCategory")
            .WithContractOpenApi<UpdatePortalCategory.Request, UpdatePortalCategory.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<DeletePortalCategoryUseCase>("/portal-categories/delete")
            .WithName("DeletePortalCategory")
            .WithContractOpenApi<DeletePortalCategory.Request, DeletePortalCategory.Response>()
            .RequireAuthorization();

        endpoints.MapKawaPost<CreatePortalWorldUseCase>("/portal-worlds/create")
            .WithName("CreatePortalWorld")
            .WithContractOpenApi<CreatePortalWorld.Request, CreatePortalWorld.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<UpdatePortalWorldUseCase>("/portal-worlds/update")
            .WithName("UpdatePortalWorld")
            .WithContractOpenApi<UpdatePortalWorld.Request, UpdatePortalWorld.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<DeletePortalWorldUseCase>("/portal-worlds/delete")
            .WithName("DeletePortalWorld")
            .WithContractOpenApi<DeletePortalWorld.Request, DeletePortalWorld.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<MovePortalWorldUseCase>("/portal-worlds/move")
            .WithName("MovePortalWorld")
            .WithContractOpenApi<MovePortalWorld.Request, MovePortalWorld.Response>()
            .RequireAuthorization();

        return endpoints;
    }
}
