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
}
