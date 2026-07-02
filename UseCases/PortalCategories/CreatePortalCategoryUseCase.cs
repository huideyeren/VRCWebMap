using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.PortalCategories;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.PortalCategories;

[KawaUseCase("portal-categories.create", Summary = "Create portal category", Version = "v1", Tags = new[] { "Portal Categories" })]
public sealed class CreatePortalCategoryUseCase(
    IPortalCategoryRepository categories,
    ISpotRepository spots,
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
    : IUseCase<CreatePortalCategory.Request, CreatePortalCategory.Response>
{
    public Task<KawaResult<CreatePortalCategory.Response>> ExecuteAsync(
        CreatePortalCategory.Request request,
        CancellationToken cancellationToken = default)
    {
        var actorError = CurrentActorPolicy.RequireWriter(currentActor, out var actor);
        if (actorError is not null)
        {
            return Failure(actorError);
        }

        var nameError = PortalCategoryName.Validate(request.Name);
        if (nameError is not null)
        {
            return Failure(nameError);
        }

        if (!Enum.IsDefined(request.Visibility))
        {
            return Failure(KawaErrorKind.Validation, "公開範囲が不正です。");
        }

        if (request.Visibility == PortalCategoryVisibility.Public && !actor!.IsAdmin)
        {
            return Failure(KawaErrorKind.Forbidden, "全体公開カテゴリは管理者だけが作成できます。");
        }

        var normalizedName = PortalCategoryName.Normalize(request.Name);
        if (categories.TryGetByNormalizedName(normalizedName, out _))
        {
            return Failure(KawaErrorKind.Conflict, "同じ名前のカテゴリがすでに存在します。");
        }

        var ownerUserId = ResolveOwnerUserId(request, actor!);
        if (ownerUserId is not null &&
            (!users.TryGetByDiscordUserId(ownerUserId, out var owner) ||
             string.IsNullOrWhiteSpace(owner.VRChatDisplayName)))
        {
            return Failure(
                KawaErrorKind.Validation,
                "所有者にはVRChat表示名を登録済みのユーザーを指定してください。");
        }

        var category = new PortalCategory(
            Guid.NewGuid(),
            actor!.DiscordUserId,
            ownerUserId,
            request.Name.Trim(),
            normalizedName,
            request.Visibility);
        categories.Upsert(category);

        var data = new PortalCategoryDataMapper(spots, users, actor).ToData(category);
        return Task.FromResult(
            KawaResult<CreatePortalCategory.Response>.Success(new(data)));
    }

    private static string? ResolveOwnerUserId(
        CreatePortalCategory.Request request,
        CurrentActor actor) =>
        request.Visibility == PortalCategoryVisibility.Public
            ? null
            : actor.IsAdmin && !string.IsNullOrWhiteSpace(request.OwnerUserId)
                ? request.OwnerUserId
                : actor.DiscordUserId;

    private static Task<KawaResult<CreatePortalCategory.Response>> Failure(KawaError error) =>
        Task.FromResult(KawaResult<CreatePortalCategory.Response>.Failure(error));

    private static Task<KawaResult<CreatePortalCategory.Response>> Failure(
        KawaErrorKind kind,
        string message) =>
        Failure(new KawaError(kind, message));
}
