namespace VrcWebMap.Backend.Contracts.Spots;

/// <summary>
/// KML/KMZ から Spot を import するユースケースの契約です。
/// </summary>
public static class ImportKmlSpots
{
    /// <summary>
    /// KML/KMZ import に必要な入力です。
    /// </summary>
    /// <param name="FileName">読み込むファイル名です。</param>
    /// <param name="ContentBase64">KML または KMZ ファイル内容の Base64 文字列です。</param>
    /// <param name="DefaultAreaCode">import する Spot に設定する既定エリアコードです。</param>
    /// <param name="SelectedSourceIndexes">利用者が import を選択した候補の順序です。</param>
    /// <param name="Confirmations">近接Spotを確認済みとして明示的に追加する候補です。</param>
    public sealed record Request(
        string FileName,
        string ContentBase64,
        int DefaultAreaCode,
        int[] SelectedSourceIndexes,
        NearDuplicateConfirmation[] Confirmations);

    /// <summary>近接Spotを確認した候補と、その時点の近接Spot IDです。</summary>
    public sealed record NearDuplicateConfirmation(int SourceIndex, Guid[] NearbySpotIds);

    /// <summary>
    /// KML/KMZ import の結果です。
    /// </summary>
    /// <param name="Spots">作成された Spot です。</param>
    /// <param name="ReconfirmationRequiredItems">実行直前の再照合で確認が必要になった候補です。</param>
    /// <param name="Warnings">import 時の警告です。</param>
    /// <param name="UnsupportedPlacemarkCount">Point として読み込めなかった Placemark 件数です。</param>
    public sealed record Response(
        SpotData[] Spots,
        string[] Warnings,
        int UnsupportedPlacemarkCount,
        PreviewKmlImport.KmlImportSpotCandidate[] ReconfirmationRequiredItems);
}
