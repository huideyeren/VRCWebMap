using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Tests.Models;

public sealed class AreaDefinitionsTests
{
    [Fact]
    public void All_ContainsJapanPrefectureCodesFromOneToFortySeven()
    {
        var japanCodes = AreaDefinitions.All
            .Where(area => area.AreaCode is >= 1 and <= 47)
            .Select(area => area.AreaCode)
            .Order()
            .ToArray();

        Assert.Equal(Enumerable.Range(1, 47).ToArray(), japanCodes);
    }

    [Fact]
    public void All_ContainsOverseasCodesInHundreds()
    {
        var overseasCodes = AreaDefinitions.All
            .Where(area => area.AreaCode >= 100)
            .Select(area => area.AreaCode)
            .Order()
            .ToArray();

        Assert.Equal(
            [
                AreaCodes.Overseas.Asia,
                AreaCodes.Overseas.Europe,
                AreaCodes.Overseas.Africa,
                AreaCodes.Overseas.Oceania,
                AreaCodes.Overseas.NorthAmerica,
                AreaCodes.Overseas.SouthAmerica,
                AreaCodes.Overseas.Antarctica
            ],
            overseasCodes);
    }

    [Fact]
    public void All_UsesExpectedRepresentativeDefinitions()
    {
        Assert.Contains(
            AreaDefinitions.All,
            area => area is { AreaCode: AreaCodes.Japan.Hokkaido, AreaName: "北海道", Category: AreaCategory.Hokkaido });

        Assert.Contains(
            AreaDefinitions.All,
            area => area is { AreaCode: AreaCodes.Japan.Tokyo, AreaName: "東京都", Category: AreaCategory.Kanto });

        Assert.Contains(
            AreaDefinitions.All,
            area => area is { AreaCode: AreaCodes.Japan.Okinawa, AreaName: "沖縄県", Category: AreaCategory.KyushuOkinawa });

        Assert.Contains(
            AreaDefinitions.All,
            area => area is { AreaCode: AreaCodes.Overseas.Europe, AreaName: "ヨーロッパ", Category: AreaCategory.Europe });
    }
}
