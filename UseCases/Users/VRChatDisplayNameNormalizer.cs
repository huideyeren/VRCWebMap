using System.Globalization;
using System.Text;
using Kawa.Abstractions;

namespace VrcWebMap.Backend.UseCases.Users;

/// <summary>
/// VRChat表示名の検証と一意性判定用正規化を行います。
/// </summary>
public static class VRChatDisplayNameNormalizer
{
    public const int MinimumLength = 4;
    public const int MaximumLength = 15;

    public static KawaError? Validate(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return new KawaError(KawaErrorKind.Validation, "VRChat表示名は必須です。");
        }

        var length = new StringInfo(displayName.Trim()).LengthInTextElements;
        return length is < MinimumLength or > MaximumLength
            ? new KawaError(
                KawaErrorKind.Validation,
                $"VRChat表示名は{MinimumLength}文字以上{MaximumLength}文字以下で入力してください。")
            : null;
    }

    public static string Normalize(string displayName) =>
        displayName.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();
}
