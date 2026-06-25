using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.UseCases.Spots;

/// <summary>
/// KML/KMZ を Spot import 候補へ変換する parser です。
/// </summary>
internal static partial class KmlSpotImportParser
{
    private const int MaxFileBytes = 5 * 1024 * 1024;
    private const int MaxKmlBytesInKmz = 5 * 1024 * 1024;
    private const int MaxPlacemarkCount = 5_000;

    /// <summary>
    /// KML/KMZ を解析して Spot 候補を返します。
    /// </summary>
    /// <param name="fileName">読み込むファイル名です。</param>
    /// <param name="contentBase64">ファイル内容の Base64 文字列です。</param>
    /// <param name="defaultAreaCode">候補に設定する既定エリアコードです。</param>
    /// <returns>解析結果です。</returns>
    public static ParseResult Parse(string fileName, string contentBase64, int defaultAreaCode)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return ParseResult.ValidationFailure("ファイル名は必須です。");
        }

        if (!AreaDefinitions.All.Any(area => area.AreaCode == defaultAreaCode))
        {
            return ParseResult.ValidationFailure("既定エリアコードは定義済みの値を指定してください。");
        }

        byte[] fileBytes;
        try
        {
            fileBytes = Convert.FromBase64String(contentBase64);
        }
        catch (FormatException)
        {
            return ParseResult.ValidationFailure("ファイル内容の Base64 形式が不正です。");
        }

        if (fileBytes.Length == 0)
        {
            return ParseResult.ValidationFailure("ファイルが空です。");
        }

        if (fileBytes.Length > MaxFileBytes)
        {
            return ParseResult.ValidationFailure("KML/KMZ ファイルは 5 MiB 以下にしてください。");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var warnings = new List<string>
        {
            "KML coordinates は WGS84 の longitude,latitude[,altitude] として読み込みます。旧日本測地系などの座標は事前に WGS84 へ変換してください。"
        };

        var kmlBytesResult = extension switch
        {
            ".kml" => ExtractedKml.Success(fileBytes, warnings),
            ".kmz" => ExtractKmlFromKmz(fileBytes, warnings),
            _ => ExtractedKml.ValidationFailure("対応しているファイル形式は .kml または .kmz です。")
        };

        if (kmlBytesResult.ErrorMessage is not null)
        {
            return ParseResult.ValidationFailure(kmlBytesResult.ErrorMessage);
        }

        return ParseKml(kmlBytesResult.KmlBytes, defaultAreaCode, kmlBytesResult.Warnings);
    }

    private static ExtractedKml ExtractKmlFromKmz(byte[] fileBytes, List<string> warnings)
    {
        try
        {
            using var archive = new ZipArchive(new MemoryStream(fileBytes), ZipArchiveMode.Read);
            var kmlEntry = archive.Entries
                .Where(entry => string.Equals(Path.GetExtension(entry.FullName), ".kml", StringComparison.OrdinalIgnoreCase))
                .OrderBy(entry => entry.FullName.Count(character => character == '/'))
                .ThenBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (kmlEntry is null)
            {
                return ExtractedKml.ValidationFailure("KMZ 内に KML ファイルが見つかりませんでした。");
            }

            if (kmlEntry.Length > MaxKmlBytesInKmz)
            {
                return ExtractedKml.ValidationFailure("KMZ 内の KML ファイルは 5 MiB 以下にしてください。");
            }

            warnings.Add($"KMZ 内の {kmlEntry.FullName} を読み込みました。NetworkLink や外部参照は取得しません。");

            using var stream = kmlEntry.Open();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return ExtractedKml.Success(memory.ToArray(), warnings);
        }
        catch (InvalidDataException)
        {
            return ExtractedKml.ValidationFailure("KMZ として読み込めませんでした。zip ファイルが破損している可能性があります。");
        }
    }

    private static ParseResult ParseKml(byte[] kmlBytes, int defaultAreaCode, List<string> warnings)
    {
        XDocument document;
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };

            using var stream = new MemoryStream(kmlBytes);
            using var reader = XmlReader.Create(stream, settings);
            document = XDocument.Load(reader, LoadOptions.None);
        }
        catch (XmlException)
        {
            return ParseResult.ValidationFailure("KML XML を解析できませんでした。");
        }

        var items = new List<PreviewKmlImport.KmlImportSpotCandidate>();
        var unsupportedPlacemarkCount = 0;

        foreach (var placemark in document.Descendants().Where(element => element.Name.LocalName == "Placemark"))
        {
            if (items.Count >= MaxPlacemarkCount)
            {
                // Google My Maps から出力した観光地一覧のような数百件規模の KML は通しつつ、
                // 誤って巨大ファイルを投入したときに preview/import が長時間占有されないように上限は残します。
                warnings.Add($"Point Placemark は先頭 {MaxPlacemarkCount} 件まで読み込みました。");
                break;
            }

            var point = placemark.Descendants().FirstOrDefault(element => element.Name.LocalName == "Point");
            var coordinates = point?.Descendants().FirstOrDefault(element => element.Name.LocalName == "coordinates")?.Value;
            if (string.IsNullOrWhiteSpace(coordinates))
            {
                unsupportedPlacemarkCount++;
                continue;
            }

            if (!TryParseFirstCoordinate(coordinates, out var latitude, out var longitude))
            {
                unsupportedPlacemarkCount++;
                continue;
            }

            var name = PlainText(placemark.Elements().FirstOrDefault(element => element.Name.LocalName == "name")?.Value);
            var description = PlainText(placemark.Elements().FirstOrDefault(element => element.Name.LocalName == "description")?.Value);
            var candidateWarnings = new List<string>();

            if (string.IsNullOrWhiteSpace(name))
            {
                name = "名称未設定 Spot";
                candidateWarnings.Add("KML に name が無いため仮の Spot 名を設定しました。");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                description = $"KML import: {name}";
                candidateWarnings.Add("KML に description が無いため仮の説明を設定しました。");
            }

            var validationError = SpotValidation.Validate(
                registeredByUserId: "kml-preview-user",
                name,
                latitude,
                longitude,
                defaultAreaCode,
                description);

            if (validationError is not null)
            {
                unsupportedPlacemarkCount++;
                continue;
            }

            items.Add(new PreviewKmlImport.KmlImportSpotCandidate(
                name,
                description,
                latitude,
                longitude,
                defaultAreaCode,
                candidateWarnings.ToArray()));
        }

        if (items.Count == 0)
        {
            warnings.Add("import 可能な Point Placemark は見つかりませんでした。LineString、Polygon、MultiGeometry は現在未対応です。");
        }

        return new ParseResult(items.ToArray(), warnings.ToArray(), unsupportedPlacemarkCount, ErrorMessage: null);
    }

    private static bool TryParseFirstCoordinate(string coordinates, out double latitude, out double longitude)
    {
        latitude = 0;
        longitude = 0;

        var firstCoordinate = coordinates
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (firstCoordinate is null)
        {
            return false;
        }

        var parts = firstCoordinate.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out longitude) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out latitude))
        {
            return false;
        }

        return latitude is >= -90 and <= 90 &&
            longitude is >= -180 and <= 180;
    }

    private static string PlainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var withoutTags = HtmlTagRegex().Replace(value, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return WhitespaceRegex().Replace(decoded, " ").Trim();
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();

    public sealed record ParseResult(
        PreviewKmlImport.KmlImportSpotCandidate[] Items,
        string[] Warnings,
        int UnsupportedPlacemarkCount,
        string? ErrorMessage)
    {
        public static ParseResult ValidationFailure(string message) =>
            new([], [], 0, message);
    }

    private sealed record ExtractedKml(
        byte[] KmlBytes,
        List<string> Warnings,
        string? ErrorMessage)
    {
        public static ExtractedKml Success(byte[] kmlBytes, List<string> warnings) =>
            new(kmlBytes, warnings, ErrorMessage: null);

        public static ExtractedKml ValidationFailure(string message) =>
            new([], [], message);
    }
}
