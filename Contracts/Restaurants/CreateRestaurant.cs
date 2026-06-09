using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.Restaurants;

/// <summary>
/// スポットに飲食店情報を追加するユースケースの契約です。
/// </summary>
public static class CreateRestaurant
{
    /// <summary>
    /// 飲食店情報の登録入力です。
    /// </summary>
    public sealed record Request(
        Guid SpotId,
        string RegisteredByUserId,
        string Name,
        string Address,
        Uri? Url,
        Uri? GurunaviUrl,
        Uri? TabelogUrl,
        Uri? RettyUrl,
        Uri? XUrl,
        Uri? InstagramUrl,
        TimeOnly OpenTime,
        TimeOnly CloseTime,
        string ClosedOn);

    /// <summary>
    /// 登録された飲食店情報を返すレスポンスです。
    /// </summary>
    /// <param name="Restaurant">登録された飲食店情報です。</param>
    public sealed record Response(Restaurant Restaurant);
}
