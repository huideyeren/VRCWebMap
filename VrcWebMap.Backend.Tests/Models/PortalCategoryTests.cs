using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Tests.Models;

public sealed class PortalCategoryTests
{
    [Fact]
    public void PersonalCategory_KeepsOwnerAndNormalizedName()
    {
        var category = new PortalCategory(
            Guid.NewGuid(),
            "creator",
            "owner",
            "個人用",
            "個人用".ToUpperInvariant(),
            PortalCategoryVisibility.Personal);

        Assert.Equal("owner", category.OwnerUserId);
        Assert.Equal("個人用", category.NormalizedName);
        Assert.Equal(PortalCategoryVisibility.Personal, category.Visibility);
    }
}
