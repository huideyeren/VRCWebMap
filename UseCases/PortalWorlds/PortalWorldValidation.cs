using Kawa.Abstractions;

namespace VrcWebMap.Backend.UseCases.PortalWorlds;

/// <summary>
/// 地図外ワールドの保存前に共通入力を検証します。
/// </summary>
internal static class PortalWorldValidation
{
    public static KawaError? Validate(
        string worldId,
        string name,
        int recommendedCapacity,
        int capacity,
        string description)
    {
        if (string.IsNullOrWhiteSpace(worldId) ||
            string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(description))
        {
            return new KawaError(
                KawaErrorKind.Validation,
                "VRChatワールドID、名前、説明は必須です。");
        }

        if (recommendedCapacity < 0 || capacity < 0)
        {
            return new KawaError(
                KawaErrorKind.Validation,
                "収容人数は0以上で指定してください。");
        }

        return null;
    }
}
