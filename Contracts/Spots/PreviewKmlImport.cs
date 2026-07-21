using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.Spots;

/// <summary>
/// KML/KMZ から Spot import 候補を preview するユースケースの契約です。
/// </summary>
public static class PreviewKmlImport
{
    /// <summary>
    /// KML/KMZ preview に必要な入力です。
    /// </summary>
    /// <param name="FileName">読み込むファイル名です。</param>
    /// <param name="ContentBase64">KML または KMZ ファイル内容の Base64 文字列です。</param>
    /// <param name="DefaultAreaCode">import 候補に設定する既定エリアコードです。</param>
    public sealed record Request(
        string FileName,
        string ContentBase64,
        int DefaultAreaCode);

    /// <summary>
    /// KML/KMZ preview の結果です。
    /// </summary>
    /// <param name="Items">import 可能な Spot 候補です。</param>
    /// <param name="Warnings">ファイル全体に対する警告です。</param>
    /// <param name="UnsupportedPlacemarkCount">Point として読み込めなかった Placemark 件数です。</param>
    public sealed record Response(
        KmlImportSpotCandidate[] Items,
        string[] Warnings,
        int UnsupportedPlacemarkCount);

    /// <summary>
    /// KML/KMZ から読み取った Spot 候補です。
    /// </summary>
    /// <param name="SourceIndex">入力ファイル内での候補の安定した順序です。</param>
    /// <param name="Name">Spot 名です。</param>
    /// <param name="Description">Spot 説明です。</param>
    /// <param name="Latitude">WGS84 緯度です。</param>
    /// <param name="Longitude">WGS84 経度です。</param>
    /// <param name="AreaCode">import 時に設定するエリアコードです。</param>
    /// <param name="IsSelectedByDefault">初期状態で import 対象として選択するかどうかです。</param>
    /// <param name="NearbySpots">50 m 以内にある既存 Spot です。</param>
    /// <param name="Warnings">この候補に対する警告です。</param>
    public sealed record KmlImportSpotCandidate(
        int SourceIndex,
        string Name,
        string Description,
        double Latitude,
        double Longitude,
        int AreaCode,
        bool IsSelectedByDefault,
        NearbySpot[] NearbySpots,
        string[] Warnings);

    /// <summary>
    /// KML import 候補の近くにある公開済み Spot です。
    /// </summary>
    /// <param name="Id">既存 Spot の ID です。</param>
    /// <param name="Name">既存 Spot の名前です。</param>
    /// <param name="Latitude">既存 Spot の緯度です。</param>
    /// <param name="Longitude">既存 Spot の経度です。</param>
    /// <param name="DistanceMeters">候補からの地表面距離（m）です。</param>
    public sealed record NearbySpot(
        Guid Id,
        string Name,
        double Latitude,
        double Longitude,
        double DistanceMeters);
}
