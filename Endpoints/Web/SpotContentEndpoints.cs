using Kawa.Web;
using VrcWebMap.Backend.Contracts.Comments;
using VrcWebMap.Backend.Contracts.PlaceInfos;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Contracts.WebLinks;
using VrcWebMap.Backend.UseCases.Comments;
using VrcWebMap.Backend.UseCases.PlaceInfos;
using VrcWebMap.Backend.UseCases.VRChatWorlds;
using VrcWebMap.Backend.UseCases.WebLinks;

namespace VrcWebMap.Backend.Endpoints.Web;

/// <summary>
/// スポットに従属する情報の登録ユースケースを Web エンドポイントとして公開します。
/// </summary>
public static class SpotContentEndpoints
{
    /// <summary>
    /// スポット従属情報登録用の Kawa エンドポイントを登録します。
    /// </summary>
    /// <param name="endpoints">エンドポイントルートビルダーです。</param>
    /// <returns>登録後のエンドポイントルートビルダーです。</returns>
    public static IEndpointRouteBuilder MapSpotContent(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapKawaPost<CreateVRChatWorldUseCase>("/vrchat-worlds/create")
            .WithName("CreateVRChatWorld")
            .WithContractOpenApi<CreateVRChatWorld.Request, CreateVRChatWorld.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<UpdateVRChatWorldUseCase>("/vrchat-worlds/update")
            .WithName("UpdateVRChatWorld")
            .WithContractOpenApi<UpdateVRChatWorld.Request, UpdateVRChatWorld.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<DeleteVRChatWorldUseCase>("/vrchat-worlds/delete")
            .WithName("DeleteVRChatWorld")
            .WithContractOpenApi<DeleteVRChatWorld.Request, DeleteVRChatWorld.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<CreatePlaceInfoUseCase>("/place-infos/create")
            .WithName("CreatePlaceInfo")
            .WithContractOpenApi<CreatePlaceInfo.Request, CreatePlaceInfo.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<UpdatePlaceInfoUseCase>("/place-infos/update")
            .WithName("UpdatePlaceInfo")
            .WithContractOpenApi<UpdatePlaceInfo.Request, UpdatePlaceInfo.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<DeletePlaceInfoUseCase>("/place-infos/delete")
            .WithName("DeletePlaceInfo")
            .WithContractOpenApi<DeletePlaceInfo.Request, DeletePlaceInfo.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<CreateWebLinkUseCase>("/web-links/create")
            .WithName("CreateWebLink")
            .WithContractOpenApi<CreateWebLink.Request, CreateWebLink.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<UpdateWebLinkUseCase>("/web-links/update")
            .WithName("UpdateWebLink")
            .WithContractOpenApi<UpdateWebLink.Request, UpdateWebLink.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<DeleteWebLinkUseCase>("/web-links/delete")
            .WithName("DeleteWebLink")
            .WithContractOpenApi<DeleteWebLink.Request, DeleteWebLink.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<GetWebLinkPreviewUseCase>("/web-links/preview")
            .WithName("GetWebLinkPreview")
            .WithContractOpenApi<GetWebLinkPreview.Request, GetWebLinkPreview.Response>();
        endpoints.MapKawaPost<CreateCommentUseCase>("/comments/create")
            .WithName("CreateComment")
            .WithContractOpenApi<CreateComment.Request, CreateComment.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<UpdateCommentUseCase>("/comments/update")
            .WithName("UpdateComment")
            .WithContractOpenApi<UpdateComment.Request, UpdateComment.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<DeleteCommentUseCase>("/comments/delete")
            .WithName("DeleteComment")
            .WithContractOpenApi<DeleteComment.Request, DeleteComment.Response>()
            .RequireAuthorization();

        return endpoints;
    }
}
