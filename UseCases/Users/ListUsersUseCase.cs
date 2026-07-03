using Kawa.Abstractions;
using Microsoft.Extensions.Options;
using VrcWebMap.Backend.Contracts.Users;
using VrcWebMap.Backend.Options;

namespace VrcWebMap.Backend.UseCases.Users;

[KawaUseCase(
    "users.list",
    Summary = "List users",
    Description = "管理者向けに登録ユーザー一覧を返します。",
    Version = "v1",
    Tags = new[] { "Users" })]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "管理者権限が必要です。")]
public sealed class ListUsersUseCase(
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor,
    IOptions<DiscordOptions> options)
    : IUseCase<ListUsers.Request, ListUsers.Response>
{
    public Task<KawaResult<ListUsers.Response>> ExecuteAsync(
        ListUsers.Request request,
        CancellationToken cancellationToken = default)
    {
        var actor = currentActor.GetCurrent();
        if (actor?.IsAdmin != true)
        {
            return Task.FromResult(
                KawaResult<ListUsers.Response>.Failure(
                    new KawaError(KawaErrorKind.Forbidden, "管理者権限が必要です。")));
        }

        var listedUsers = users.List()
            .Select(user => new ListUsers.Item(
                user.DiscordUserId,
                user.Username,
                user.VRChatDisplayName,
                user.IsAdmin,
                AdministratorPolicy.IsInitialAdministrator(options.Value, user.DiscordUserId)))
            .ToArray();
        return Task.FromResult(KawaResult<ListUsers.Response>.Success(new(listedUsers)));
    }
}
