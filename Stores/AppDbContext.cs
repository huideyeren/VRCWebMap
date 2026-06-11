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

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<DiscordUser> DiscordUsers => Set<DiscordUser>();

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
            entity.HasIndex(user => user.RequiredGuildId);
        });

        modelBuilder.Entity<Spot>(entity =>
        {
            entity.HasKey(spot => spot.Id);
            entity.Property(spot => spot.RegisteredByUserId).HasMaxLength(128).IsRequired();
            entity.Property(spot => spot.Name).HasMaxLength(200).IsRequired();
            entity.Property(spot => spot.Description).IsRequired();
            entity.HasIndex(spot => spot.AreaCode);
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
            entity.HasOne<Spot>()
                .WithMany()
                .HasForeignKey(world => world.SpotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.HasKey(restaurant => restaurant.Id);
            entity.Property(restaurant => restaurant.RegisteredByUserId).HasMaxLength(128).IsRequired();
            entity.Property(restaurant => restaurant.Name).HasMaxLength(300).IsRequired();
            entity.Property(restaurant => restaurant.Address).HasMaxLength(500).IsRequired();
            entity.Property(restaurant => restaurant.ClosedOn).HasMaxLength(200).IsRequired();
            entity.HasIndex(restaurant => restaurant.SpotId);
            entity.HasOne<Spot>()
                .WithMany()
                .HasForeignKey(restaurant => restaurant.SpotId)
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
