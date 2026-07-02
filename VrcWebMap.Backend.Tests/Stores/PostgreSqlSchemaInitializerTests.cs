using VrcWebMap.Backend.Stores;

namespace VrcWebMap.Backend.Tests.Stores;

public sealed class PostgreSqlSchemaInitializerTests
{
    [Fact]
    public void EnsureSpotSearchIndexSql_CreatesGinFullTextIndexForSpotNameAndDescription()
    {
        var sql = PostgreSqlSchemaInitializer.EnsureSpotSearchIndexSql;

        Assert.Contains("CREATE INDEX IF NOT EXISTS", sql, StringComparison.Ordinal);
        Assert.Contains("\"IX_Spots_SearchVector\"", sql, StringComparison.Ordinal);
        Assert.Contains("USING GIN", sql, StringComparison.Ordinal);
        Assert.Contains("to_tsvector('simple'", sql, StringComparison.Ordinal);
        Assert.Contains("\"Name\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"Description\"", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureDiscordProfileSchemaSql_AddsNullableColumnsAndUniquePartialIndex()
    {
        var sql = PostgreSqlSchemaInitializer.EnsureDiscordProfileSchemaSql;

        Assert.Contains("ADD COLUMN IF NOT EXISTS \"VRChatDisplayName\"", sql, StringComparison.Ordinal);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"NormalizedVRChatDisplayName\"", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE UNIQUE INDEX IF NOT EXISTS", sql, StringComparison.Ordinal);
        Assert.Contains("\"IX_DiscordUsers_NormalizedVRChatDisplayName\"", sql, StringComparison.Ordinal);
        Assert.Contains("WHERE \"NormalizedVRChatDisplayName\" IS NOT NULL", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsurePortalCategorySchemaSql_AddsCategoryAndExactlyOneWorldParent()
    {
        var sql = PostgreSqlSchemaInitializer.EnsurePortalCategorySchemaSql;

        Assert.Contains("CREATE TABLE IF NOT EXISTS \"PortalCategories\"", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER COLUMN \"SpotId\" DROP NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("\"PortalCategoryId\" uuid", sql, StringComparison.Ordinal);
        Assert.Contains("CHECK", sql, StringComparison.Ordinal);
        Assert.Contains("\"SpotId\" IS NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("\"PortalCategoryId\" IS NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("ON DELETE RESTRICT", sql, StringComparison.Ordinal);
        Assert.Contains("\"IX_PortalCategories_NormalizedName\"", sql, StringComparison.Ordinal);
        Assert.Contains("CK_PortalCategories_OwnerMatchesVisibility", sql, StringComparison.Ordinal);
    }
}
