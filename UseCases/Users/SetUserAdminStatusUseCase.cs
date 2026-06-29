using Kawa.Abstractions;
using Microsoft.Extensions.Options;
using VrcWebMap.Backend.Contracts.Users;
using VrcWebMap.Backend.Options;

namespace VrcWebMap.Backend.UseCases.Users;

[KawaUseCase(
    "users.admin-status.set",
    Summary = "Set user administrator status",
    Description = "管理者が他ユーザーの管理者状態を変更します。",
    Version = "v1",
    Tags = new[] { "Users" })]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "管理者権限が必要です。")]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "対象ユーザーが存在しません。")]
[KawaErrorResponse(KawaErrorKind.Conflict, Description = "保護された管理者権限は解除できません。")]
public sealed class SetUserAdminStatusUseCase(
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor,
    IOptions<DiscordOptions> options)
    : IUseCase<SetUserAdminStatus.Request, SetUserAdminStatus.Response>
{
    public Task<KawaResult<SetUserAdminStatus.Response>> ExecuteAsync(
        SetUserAdminStatus.Request request,
        CancellationToken cancellationToken = default)
    {
        var actor = currentActor.GetCurrent();
        if (actor?.IsAdmin != true)
        {
            return Failure(KawaErrorKind.Forbidden, "管理者権限が必要です。");
        }

        var targetId = request.DiscordUserId?.Trim();
        if (string.IsNullOrWhiteSpace(targetId) ||
            !users.TryGetByDiscordUserId(targetId, out var target))
        {
            return Failure(KawaErrorKind.NotFound, "対象ユーザーが存在しません。");
        }

        if (!request.IsAdmin &&
            (AdministratorPolicy.IsInitialAdministrator(options.Value, targetId) ||
             string.Equals(actor.DiscordUserId, targetId, StringComparison.Ordinal)))
        {
            return Failure(KawaErrorKind.Conflict, "初期管理者または自分自身の管理者権限は解除できません。");
        }

        var updated = target with { IsAdmin = request.IsAdmin };
        users.Upsert(updated);
        return Task.FromResult(KawaResult<SetUserAdminStatus.Response>.Success(new(updated)));
    }

    private static Task<KawaResult<SetUserAdminStatus.Response>> Failure(
        KawaErrorKind kind,
        string message) =>
        Task.FromResult(
            KawaResult<SetUserAdminStatus.Response>.Failure(new KawaError(kind, message)));
}
