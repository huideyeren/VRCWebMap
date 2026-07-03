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
    /// 既存volumeへポータルカテゴリとワールドの排他的な所属先を補うSQLです。
    /// </summary>
    public const string EnsurePortalCategorySchemaSql =
        """
        CREATE TABLE IF NOT EXISTS "PortalCategories" (
            "Id" uuid NOT NULL,
            "RegisteredByUserId" character varying(128) NOT NULL,
            "OwnerUserId" character varying(128),
            "Name" character varying(200) NOT NULL,
            "NormalizedName" character varying(200) NOT NULL,
            "Visibility" integer NOT NULL,
            CONSTRAINT "PK_PortalCategories" PRIMARY KEY ("Id")
        );

        ALTER TABLE "VRChatWorlds"
            ALTER COLUMN "SpotId" DROP NOT NULL,
            ADD COLUMN IF NOT EXISTS "PortalCategoryId" uuid;

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_PortalCategories_NormalizedName"
            ON "PortalCategories" ("NormalizedName");

        CREATE INDEX IF NOT EXISTS "IX_VRChatWorlds_PortalCategoryId"
            ON "VRChatWorlds" ("PortalCategoryId");

        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM pg_constraint
                WHERE conname = 'FK_VRChatWorlds_PortalCategories_PortalCategoryId'
            ) THEN
                ALTER TABLE "VRChatWorlds"
                    ADD CONSTRAINT "FK_VRChatWorlds_PortalCategories_PortalCategoryId"
                    FOREIGN KEY ("PortalCategoryId") REFERENCES "PortalCategories" ("Id")
                    ON DELETE RESTRICT;
            END IF;
        END $$;

        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM pg_constraint
                WHERE conname = 'CK_VRChatWorlds_ExactlyOneParent'
            ) THEN
                ALTER TABLE "VRChatWorlds"
                    ADD CONSTRAINT "CK_VRChatWorlds_ExactlyOneParent"
                    CHECK (
                        ("SpotId" IS NOT NULL AND "PortalCategoryId" IS NULL) OR
                        ("SpotId" IS NULL AND "PortalCategoryId" IS NOT NULL)
                    );
            END IF;
        END $$;

        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM pg_constraint
                WHERE conname = 'CK_PortalCategories_OwnerMatchesVisibility'
            ) THEN
                ALTER TABLE "PortalCategories"
                    ADD CONSTRAINT "CK_PortalCategories_OwnerMatchesVisibility"
                    CHECK (
                        ("Visibility" = 0 AND "OwnerUserId" IS NOT NULL) OR
                        ("Visibility" = 1 AND "OwnerUserId" IS NULL)
                    );
            END IF;
        END $$;
        """;

    /// <summary>
    /// 現在の PostgreSQL schema を起動時に利用可能な状態へ整えます。
    /// </summary>
    /// <param name="db">アプリケーション DB context です。</param>
    public static void EnsureCreated(AppDbContext db)
    {
        db.Database.EnsureCreated();
        EnsurePortalCategorySchema(db);
        EnsureDiscordProfileSchema(db);
        EnsureSpotSearchIndex(db);
    }

    private static void EnsurePortalCategorySchema(AppDbContext db)
    {
        // EnsureCreated()は既存tableのnullable変更やFK追加を行わないため、
        // 既存volumeを保持したまま新しい所属先を冪等に補います。
        db.Database.ExecuteSqlRaw(EnsurePortalCategorySchemaSql);
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
