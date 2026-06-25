using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.Spots;

/// <summary>
/// KML/KMZ から Spot を import するユースケースの契約です。
/// </summary>
public static class ImportKmlSpots
{
    /// <summary>
    /// KML/KMZ import に必要な入力です。
    /// </summary>
    /// <param name="ActorUserId">操作ユーザー ID です。</param>
    /// <param name="ActorIsAdmin">操作ユーザーが管理者かどうかです。</param>
    /// <param name="FileName">読み込むファイル名です。</param>
    /// <param name="ContentBase64">KML または KMZ ファイル内容の Base64 文字列です。</param>
    /// <param name="DefaultAreaCode">import する Spot に設定する既定エリアコードです。</param>
    public sealed record Request(
        string ActorUserId,
        bool ActorIsAdmin,
        string FileName,
        string ContentBase64,
        int DefaultAreaCode);

    /// <summary>
    /// KML/KMZ import の結果です。
    /// </summary>
    /// <param name="Spots">作成された Spot です。</param>
    /// <param name="Warnings">import 時の警告です。</param>
    /// <param name="UnsupportedPlacemarkCount">Point として読み込めなかった Placemark 件数です。</param>
    public sealed record Response(
        Spot[] Spots,
        string[] Warnings,
        int UnsupportedPlacemarkCount);
}
