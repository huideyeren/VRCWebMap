using System.Text;
using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.UseCases.Spots;

public sealed class KmlImportUseCaseTests
{
    [Fact]
    public async Task Preview_KmlPoint_ReturnsCandidateWithLongitudeLatitudeOrder()
    {
        var useCase = CreatePreviewUseCase(isAdmin: true);

        var result = await useCase.ExecuteAsync(new PreviewKmlImport.Request(
            "spots.kml",
            ToBase64(SampleKml()),
            AreaCodes.Japan.Tokyo));

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal("井の頭公園駅", item.Name);
        Assert.Equal(35.697484, item.Latitude);
        Assert.Equal(139.582739, item.Longitude);
        Assert.Equal(AreaCodes.Japan.Tokyo, item.AreaCode);
    }

    [Fact]
    public async Task Preview_NonAdmin_ReturnsForbidden()
    {
        var useCase = CreatePreviewUseCase(isAdmin: false);

        var result = await useCase.ExecuteAsync(new PreviewKmlImport.Request(
            "spots.kml",
            ToBase64(SampleKml()),
            AreaCodes.Japan.Tokyo));

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Forbidden, result.Error.Kind);
    }

    [Fact]
    public async Task Import_Admin_CreatesSpots()
    {
        var repository = new FakeSpotRepository();
        var useCase = new ImportKmlSpotsUseCase(
            repository,
            Writer("admin-user", isAdmin: true));

        var result = await useCase.ExecuteAsync(new ImportKmlSpots.Request(
            "spots.kml",
            ToBase64(SampleKml()),
            AreaCodes.Japan.Tokyo));

        Assert.True(result.IsSuccess);
        var spot = Assert.Single(result.Value!.Spots);
        Assert.Equal("admin-user", spot.RegisteredByUserId);
        Assert.Equal("井の頭公園駅", spot.Name);
        Assert.Equal(35.697484, spot.Latitude);
        Assert.Equal(139.582739, spot.Longitude);
        Assert.Equal(AreaCodes.Japan.Tokyo, spot.AreaCode);
        Assert.True(repository.Exists(spot.Id));
    }

    [Fact]
    public async Task Preview_UnsupportedPolygon_ReportsUnsupportedCount()
    {
        var useCase = CreatePreviewUseCase(isAdmin: true);

        var result = await useCase.ExecuteAsync(new PreviewKmlImport.Request(
            "polygon.kml",
            ToBase64("""
                <?xml version="1.0" encoding="UTF-8"?>
                <kml xmlns="http://www.opengis.net/kml/2.2">
                  <Document>
                    <Placemark>
                      <name>未対応ポリゴン</name>
                      <Polygon><outerBoundaryIs><LinearRing><coordinates>139,35 140,35 140,36 139,35</coordinates></LinearRing></outerBoundaryIs></Polygon>
                    </Placemark>
                  </Document>
                </kml>
                """),
            AreaCodes.Japan.Tokyo));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(1, result.Value.UnsupportedPlacemarkCount);
    }

    [Fact]
    public async Task Preview_MoreThanOldLimit_ReturnsAllPointPlacemarks()
    {
        var useCase = CreatePreviewUseCase(isAdmin: true);

        var result = await useCase.ExecuteAsync(new PreviewKmlImport.Request(
            "many-spots.kml",
            ToBase64(ManyPointPlacemarksKml(498)),
            AreaCodes.Japan.Tokyo));

        Assert.True(result.IsSuccess);
        Assert.Equal(498, result.Value!.Items.Length);
        Assert.DoesNotContain(result.Value.Warnings, warning => warning.Contains("先頭", StringComparison.Ordinal));
    }

    private static PreviewKmlImportUseCase CreatePreviewUseCase(bool isAdmin) =>
        new(Writer(isAdmin ? "admin-user" : "general-user", isAdmin));

    private static FakeCurrentActorAccessor Writer(string userId, bool isAdmin) =>
        new(new CurrentActor(userId, isAdmin, HasVRChatDisplayName: true));

    private static string SampleKml() =>
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <kml xmlns="http://www.opengis.net/kml/2.2">
          <Document>
            <Placemark>
              <name>井の頭公園駅</name>
              <description><![CDATA[<p>VRChatの聖地</p>]]></description>
              <Point>
                <coordinates>139.582739,35.697484,0</coordinates>
              </Point>
            </Placemark>
          </Document>
        </kml>
        """;

    private static string ManyPointPlacemarksKml(int count)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""
            <?xml version="1.0" encoding="UTF-8"?>
            <kml xmlns="http://www.opengis.net/kml/2.2">
              <Document>
            """);

        for (var index = 0; index < count; index++)
        {
            // 添付された Google My Maps 相当の件数を再現するため、
            // 旧上限 200 を大きく超える Point Placemark を生成します。
            builder.AppendLine($"""
                    <Placemark>
                      <name>Spot {index + 1}</name>
                      <description>Generated KML spot {index + 1}</description>
                      <Point>
                        <coordinates>{139.0 + index * 0.0001:0.####},{35.0 + index * 0.0001:0.####},0</coordinates>
                      </Point>
                    </Placemark>
                """);
        }

        builder.AppendLine("""
              </Document>
            </kml>
            """);
        return builder.ToString();
    }

    private static string ToBase64(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}
