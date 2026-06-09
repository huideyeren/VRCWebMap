using Kawa.Abstractions;
using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.UseCases.Spots;

/// <summary>
/// スポット入力値の検証ルールです。
/// </summary>
internal static class SpotValidation
{
    /// <summary>
    /// スポット登録者、スポット名、座標、地域コード、説明を検証します。
    /// </summary>
    /// <param name="registeredByUserId">登録者 ID です。</param>
    /// <param name="name">スポット名です。</param>
    /// <param name="latitude">緯度です。</param>
    /// <param name="longitude">経度です。</param>
    /// <param name="areaCode">都道府県コードまたは地域コードです。</param>
    /// <param name="description">Markdown を想定した説明です。</param>
    /// <returns>検証エラーがある場合は <see cref="KawaError"/>、問題がない場合は <c>null</c> です。</returns>
    public static KawaError? Validate(
        string registeredByUserId,
        string name,
        double latitude,
        double longitude,
        int areaCode,
        string description)
    {
        if (string.IsNullOrWhiteSpace(registeredByUserId))
        {
            return new KawaError(KawaErrorKind.Validation, "登録者 ID は必須です。");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return new KawaError(KawaErrorKind.Validation, "地図名は必須です。");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return new KawaError(KawaErrorKind.Validation, "説明は必須です。");
        }

        if (latitude is < -90 or > 90)
        {
            return new KawaError(KawaErrorKind.Validation, "緯度は -90 から 90 の範囲で指定してください。");
        }

        if (longitude is < -180 or > 180)
        {
            return new KawaError(KawaErrorKind.Validation, "経度は -180 から 180 の範囲で指定してください。");
        }

        if (!AreaDefinitions.All.Any(area => area.AreaCode == areaCode))
        {
            return new KawaError(KawaErrorKind.Validation, "地域コードは定義済みの値を指定してください。");
        }

        return null;
    }
}
