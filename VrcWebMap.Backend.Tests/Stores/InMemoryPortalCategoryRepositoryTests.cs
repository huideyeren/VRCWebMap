using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Stores;

namespace VrcWebMap.Backend.Tests.Stores;

public sealed class InMemoryPortalCategoryRepositoryTests
{
    [Fact]
    public void UpsertAndTryGet_RoundTripsCategory()
    {
        var repository = new InMemoryPortalCategoryRepository();
        var category = new PortalCategory(
            Guid.NewGuid(),
            "creator",
            "owner",
            "個人用",
            "個人用",
            PortalCategoryVisibility.Personal);

        repository.Upsert(category);

        Assert.True(repository.TryGet(category.Id, out var loaded));
        Assert.Equal(category, loaded);
        Assert.True(repository.TryGetByNormalizedName("個人用", out _));
        Assert.Equal(category, Assert.Single(repository.List()));
        Assert.True(repository.Delete(category.Id));
        Assert.False(repository.TryGet(category.Id, out _));
    }
}
