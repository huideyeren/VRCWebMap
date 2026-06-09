using Kawa.Web;
using VrcWebMap.Backend.UseCases.Comments;
using VrcWebMap.Backend.UseCases.Restaurants;
using VrcWebMap.Backend.UseCases.VRChatWorlds;

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
        endpoints.MapKawaPost<CreateVRChatWorldUseCase>("/vrchat-worlds/create").WithName("CreateVRChatWorld");
        endpoints.MapKawaPost<UpdateVRChatWorldUseCase>("/vrchat-worlds/update").WithName("UpdateVRChatWorld");
        endpoints.MapKawaPost<DeleteVRChatWorldUseCase>("/vrchat-worlds/delete").WithName("DeleteVRChatWorld");
        endpoints.MapKawaPost<CreateRestaurantUseCase>("/restaurants/create").WithName("CreateRestaurant");
        endpoints.MapKawaPost<UpdateRestaurantUseCase>("/restaurants/update").WithName("UpdateRestaurant");
        endpoints.MapKawaPost<DeleteRestaurantUseCase>("/restaurants/delete").WithName("DeleteRestaurant");
        endpoints.MapKawaPost<CreateCommentUseCase>("/comments/create").WithName("CreateComment");
        endpoints.MapKawaPost<UpdateCommentUseCase>("/comments/update").WithName("UpdateComment");
        endpoints.MapKawaPost<DeleteCommentUseCase>("/comments/delete").WithName("DeleteComment");

        return endpoints;
    }
}
