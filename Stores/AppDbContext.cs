using Microsoft.EntityFrameworkCore;
using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Stores;

/// <summary>
/// アプリケーションの PostgreSQL 永続化コンテキストです。
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Spot> Spots => Set<Spot>();

    public DbSet<VRChatWorld> VRChatWorlds => Set<VRChatWorld>();

    public DbSet<PlaceInfo> PlaceInfos => Set<PlaceInfo>();

    public DbSet<WebLink> WebLinks => Set<WebLink>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<DiscordUser> DiscordUsers => Set<DiscordUser>();

    public DbSet<PortalCategory> PortalCategories => Set<PortalCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiscordUser>(entity =>
        {
            entity.HasKey(user => user.DiscordUserId);
            entity.Property(user => user.DiscordUserId).HasMaxLength(128).IsRequired();
            entity.Property(user => user.Username).HasMaxLength(100).IsRequired();
            entity.Property(user => user.GlobalName).HasMaxLength(100);
            entity.Property(user => user.AvatarHash).HasMaxLength(128);
            entity.Property(user => user.RequiredGuildId).HasMaxLength(128).IsRequired();
            entity.Property(user => user.VRChatDisplayName).HasMaxLength(100);
            entity.Property(user => user.NormalizedVRChatDisplayName).HasMaxLength(100);
            entity.HasIndex(user => user.RequiredGuildId);
            entity.HasIndex(user => user.NormalizedVRChatDisplayName)
                .IsUnique()
                .HasFilter("\"NormalizedVRChatDisplayName\" IS NOT NULL");
        });

        modelBuilder.Entity<Spot>(entity =>
        {
            entity.HasKey(spot => spot.Id);
            entity.Property(spot => spot.RegisteredByUserId).HasMaxLength(128).IsRequired();
            entity.Property(spot => spot.Name).HasMaxLength(200).IsRequired();
            entity.Property(spot => spot.Description).IsRequired();
            entity.HasIndex(spot => spot.AreaCode);
        });

        modelBuilder.Entity<PortalCategory>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.Property(category => category.RegisteredByUserId).HasMaxLength(128).IsRequired();
            entity.Property(category => category.OwnerUserId).HasMaxLength(128);
            entity.Property(category => category.Name).HasMaxLength(200).IsRequired();
            entity.Property(category => category.NormalizedName).HasMaxLength(200).IsRequired();
            entity.HasIndex(category => category.NormalizedName).IsUnique();
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_PortalCategories_OwnerMatchesVisibility",
                "(\"Visibility\" = 0 AND \"OwnerUserId\" IS NOT NULL) OR " +
                "(\"Visibility\" = 1 AND \"OwnerUserId\" IS NULL)"));
        });

        modelBuilder.Entity<VRChatWorld>(entity =>
        {
            entity.HasKey(world => world.Id);
            entity.Property(world => world.RegisteredByUserId).HasMaxLength(128).IsRequired();
            entity.Property(world => world.VRChatWorldId).HasMaxLength(128).IsRequired();
            entity.Property(world => world.Name).HasMaxLength(300).IsRequired();
            entity.Property(world => world.Description).IsRequired();
            entity.Ignore(world => world.WorldPageUrl);
            entity.Ignore(world => world.ReleaseStatus);
            entity.HasIndex(world => world.SpotId);
            entity.HasIndex(world => world.PortalCategoryId);
            entity.HasOne<Spot>()
                .WithMany()
                .HasForeignKey(world => world.SpotId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PortalCategory>()
                .WithMany()
                .HasForeignKey(world => world.PortalCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_VRChatWorlds_ExactlyOneParent",
                "(\"SpotId\" IS NOT NULL AND \"PortalCategoryId\" IS NULL) OR " +
                "(\"SpotId\" IS NULL AND \"PortalCategoryId\" IS NOT NULL)"));
        });

        modelBuilder.Entity<PlaceInfo>(entity =>
        {
            entity.HasKey(placeInfo => placeInfo.Id);
            entity.Property(placeInfo => placeInfo.RegisteredByUserId).HasMaxLength(128).IsRequired();
            entity.Property(placeInfo => placeInfo.Name).HasMaxLength(300).IsRequired();
            entity.Property(placeInfo => placeInfo.Address).HasMaxLength(500).IsRequired();
            entity.Property(placeInfo => placeInfo.BusinessInformation).IsRequired();
            entity.HasIndex(placeInfo => placeInfo.SpotId);
            entity.HasOne<Spot>()
                .WithMany()
                .HasForeignKey(placeInfo => placeInfo.SpotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WebLink>(entity =>
        {
            entity.HasKey(webLink => webLink.Id);
            entity.Property(webLink => webLink.RegisteredByUserId).HasMaxLength(128).IsRequired();
            entity.Property(webLink => webLink.SiteName).HasMaxLength(300).IsRequired();
            entity.Property(webLink => webLink.Url).IsRequired();
            entity.HasIndex(webLink => webLink.SpotId);
            entity.HasOne<Spot>()
                .WithMany()
                .HasForeignKey(webLink => webLink.SpotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(comment => comment.Id);
            entity.Property(comment => comment.RegisteredByUserId).HasMaxLength(128).IsRequired();
            entity.Property(comment => comment.Comments).IsRequired();
            entity.HasIndex(comment => comment.SpotId);
            entity.HasOne<Spot>()
                .WithMany()
                .HasForeignKey(comment => comment.SpotId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
