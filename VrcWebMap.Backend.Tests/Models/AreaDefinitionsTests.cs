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
            area => area is { AreaCode: AreaCodes.Japan.Mie, AreaName: "三重県", Category: AreaCategory.Chubu });

        Assert.Contains(
            AreaDefinitions.All,
            area => area is { AreaCode: AreaCodes.Japan.Osaka, AreaName: "大阪府", Category: AreaCategory.Kansai });

        Assert.Contains(
            AreaDefinitions.All,
            area => area is { AreaCode: AreaCodes.Japan.Okinawa, AreaName: "沖縄県", Category: AreaCategory.KyushuOkinawa });

        Assert.Contains(
            AreaDefinitions.All,
            area => area is { AreaCode: AreaCodes.Overseas.Europe, AreaName: "ヨーロッパ", Category: AreaCategory.Europe });
    }

    [Fact]
    public void CategoryDisplayNames_DefinesExpectedNamesAndOrder()
    {
        Assert.Equal("中部", AreaCategoryDisplayNames.Get(AreaCategory.Chubu));
        Assert.Equal(
            Enum.GetValues<AreaCategory>().Length,
            AreaCategoryDisplayNames.All.Length);
        Assert.True(
            AreaCategoryDisplayNames.OrderOf(AreaCategory.Hokkaido) <
            AreaCategoryDisplayNames.OrderOf(AreaCategory.Tohoku));
        Assert.True(
            AreaCategoryDisplayNames.OrderOf(AreaCategory.Kanto) <
            AreaCategoryDisplayNames.OrderOf(AreaCategory.Chubu));
    }
}
