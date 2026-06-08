using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.Spots;

/// <summary>
/// 指定されたスポットを取得するユースケースの契約です。
/// </summary>
public static class GetSpot
{
    /// <summary>
    /// 取得対象のスポットを指定する入力です。
    /// </summary>
    /// <param name="Id">取得するスポットの ID です。</param>
    public sealed record Request(Guid Id);

    /// <summary>
    /// 取得したスポットを返すレスポンスです。
    /// </summary>
    /// <param name="Spot">取得したスポットです。</param>
    public sealed record Response(Spot Spot);
}
