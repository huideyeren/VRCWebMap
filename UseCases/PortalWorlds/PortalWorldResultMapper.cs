using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.PortalCategories;
using VrcWebMap.Backend.UseCases.Resources;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.PortalWorlds;

/// <summary>
/// 親カテゴリの変更権限を地図外ワールドの編集可否として公開します。
/// </summary>
internal static class PortalWorldResultMapper
{
    public static VRChatWorldData ToData(
        VRChatWorld world,
        PortalCategory category,
        IDiscordUserRepository users,
        CurrentActor actor)
    {
        var resources = new PublicResourceMapper(users.List(), actor);
        return resources.ToVRChatWorld(
            world,
            PortalCategoryAuthorization.CanMutate(category, actor));
    }
}
