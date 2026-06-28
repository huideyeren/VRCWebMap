using Microsoft.EntityFrameworkCore;

namespace VrcWebMap.Backend.Stores;

/// <summary>
/// PostgreSQL 固有の schema 補修を行います。
/// </summary>
public static class PostgreSqlSchemaInitializer
{
    /// <summary>
    /// Spot 名と説明を対象にした全文検索用 GIN index を作成する SQL です。
    /// </summary>
    public const string EnsureSpotSearchIndexSql =
        """
        CREATE INDEX IF NOT EXISTS "IX_Spots_SearchVector"
        ON "Spots"
        USING GIN (
            (
                setweight(to_tsvector('simple', coalesce("Name", '')), 'A') ||
                setweight(to_tsvector('simple', coalesce("Description", '')), 'B')
            )
        );
        """;

    /// <summary>
    /// 既存の DiscordUsers table へVRChat表示名の列と一意indexを補うSQLです。
    /// </summary>
    public const string EnsureDiscordProfileSchemaSql =
        """
        ALTER TABLE "DiscordUsers"
            ADD COLUMN IF NOT EXISTS "VRChatDisplayName" character varying(100),
            ADD COLUMN IF NOT EXISTS "NormalizedVRChatDisplayName" character varying(100);

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_DiscordUsers_NormalizedVRChatDisplayName"
        ON "DiscordUsers" ("NormalizedVRChatDisplayName")
        WHERE "NormalizedVRChatDisplayName" IS NOT NULL;
        """;

    /// <summary>
    /// 現在の PostgreSQL schema を起動時に利用可能な状態へ整えます。
    /// </summary>
    /// <param name="db">アプリケーション DB context です。</param>
    public static void EnsureCreated(AppDbContext db)
    {
        db.Database.EnsureCreated();
        EnsureDiscordProfileSchema(db);
        EnsureSpotSearchIndex(db);
    }

    private static void EnsureDiscordProfileSchema(AppDbContext db)
    {
        // EnsureCreated() は既存tableへ列を追加しないため、既存volumeを保持したまま
        // nullable列と一意indexだけを冪等に補います。
        db.Database.ExecuteSqlRaw(EnsureDiscordProfileSchemaSql);
    }

    private static void EnsureSpotSearchIndex(AppDbContext db)
    {
        // このアプリは現段階では migrations ではなく EnsureCreated() 運用のため、
        // 既存の Docker volume にも安全に反映できるよう IF NOT EXISTS で index を補修します。
        // 'simple' は日本語・英語混在の Spot 名で stemming による意図しない変形を避けるために使います。
        db.Database.ExecuteSqlRaw(EnsureSpotSearchIndexSql);
    }
}
