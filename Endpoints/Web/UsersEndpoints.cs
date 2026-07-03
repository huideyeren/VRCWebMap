using Kawa.Web;
using VrcWebMap.Backend.Contracts.Users;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Endpoints.Web;

/// <summary>
/// プロフィールとユーザー管理UseCaseをWebへ公開します。
/// </summary>
public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsers(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapKawaPost<UpdateVRChatDisplayNameUseCase>("/users/profile")
            .WithName("UpdateVRChatDisplayName")
            .WithContractOpenApi<UpdateVRChatDisplayName.Request, UpdateVRChatDisplayName.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<ListUsersUseCase>("/users/list")
            .WithName("ListUsers")
            .WithContractOpenApi<ListUsers.Request, ListUsers.Response>()
            .RequireAuthorization();
        endpoints.MapKawaPost<SetUserAdminStatusUseCase>("/users/admin-status")
            .WithName("SetUserAdminStatus")
            .WithContractOpenApi<SetUserAdminStatus.Request, SetUserAdminStatus.Response>()
            .RequireAuthorization();
        return endpoints;
    }
}
