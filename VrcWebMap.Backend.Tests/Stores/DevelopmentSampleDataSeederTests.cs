using VrcWebMap.Backend.Stores;

namespace VrcWebMap.Backend.Tests.Stores;

public sealed class DevelopmentSampleDataSeederTests
{
    [Fact]
    public void Seed_AddsDevelopmentSampleData()
    {
        var repository = new InMemorySpotRepository();

        DevelopmentSampleDataSeeder.Seed(repository);

        Assert.Equal(2, repository.List().Length);
        Assert.Equal(2, repository.ListWorlds().Length);
        Assert.Equal(2, repository.ListPlaceInfos().Length);
        Assert.Equal(3, repository.ListWebLinks().Length);
        Assert.Empty(repository.ListComments());
    }

    [Fact]
    public void Seed_IsIdempotent()
    {
        var repository = new InMemorySpotRepository();

        DevelopmentSampleDataSeeder.Seed(repository);
        DevelopmentSampleDataSeeder.Seed(repository);

        Assert.Equal(2, repository.List().Length);
        Assert.Equal(2, repository.ListWorlds().Length);
        Assert.Equal(2, repository.ListPlaceInfos().Length);
        Assert.Equal(3, repository.ListWebLinks().Length);
    }
}
