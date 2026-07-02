using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.PortalCategories;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.UseCases.PortalCategories;

public sealed class PortalCategoryPolicyTests
{
    [Theory]
    [InlineData("関東")]
    [InlineData("  ")]
    public void Validate_RejectsReservedOrBlankName(string name)
    {
        Assert.NotNull(PortalCategoryName.Validate(name));
    }

    [Fact]
    public void CanMutate_PersonalOwnerAndAdminOnly()
    {
        var category = Personal("owner");

        Assert.True(PortalCategoryAuthorization.CanMutate(category, Actor("owner")));
        Assert.True(PortalCategoryAuthorization.CanMutate(category, Actor("admin", isAdmin: true)));
        Assert.False(PortalCategoryAuthorization.CanMutate(category, Actor("other")));
    }

    [Fact]
    public void CanMutate_PublicRequiresAdministrator()
    {
        var category = new PortalCategory(
            Guid.NewGuid(),
            "admin",
            OwnerUserId: null,
            "全体公開",
            "全体公開",
            PortalCategoryVisibility.Public);

        Assert.True(PortalCategoryAuthorization.CanMutate(category, Actor("admin", isAdmin: true)));
        Assert.False(PortalCategoryAuthorization.CanMutate(category, Actor("owner")));
    }

    private static PortalCategory Personal(string owner) =>
        new(
            Guid.NewGuid(),
            owner,
            owner,
            "個人用",
            "個人用",
            PortalCategoryVisibility.Personal);

    private static CurrentActor Actor(string id, bool isAdmin = false) =>
        new(id, isAdmin, HasVRChatDisplayName: true);
}
