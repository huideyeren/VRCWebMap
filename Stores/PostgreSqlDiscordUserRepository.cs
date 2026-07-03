using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Stores;

/// <summary>
/// PostgreSQL を使う Discord ユーザーリポジトリです。
/// </summary>
public sealed class PostgreSqlDiscordUserRepository(AppDbContext db) : IDiscordUserRepository
{
    public DiscordUser[] List() =>
        db.DiscordUsers
            .AsNoTracking()
            .OrderBy(user => user.VRChatDisplayName == null)
            .ThenBy(user => user.NormalizedVRChatDisplayName)
            .ThenBy(user => user.Username)
            .ToArray();

    public bool TryGetByDiscordUserId(string discordUserId, [NotNullWhen(true)] out DiscordUser? user)
    {
        user = db.DiscordUsers.AsNoTracking().FirstOrDefault(candidate => candidate.DiscordUserId == discordUserId);
        return user is not null;
    }

    public bool TryGetByNormalizedVRChatDisplayName(
        string normalizedVRChatDisplayName,
        [NotNullWhen(true)] out DiscordUser? user)
    {
        user = db.DiscordUsers
            .AsNoTracking()
            .FirstOrDefault(candidate =>
                candidate.NormalizedVRChatDisplayName == normalizedVRChatDisplayName);
        return user is not null;
    }

    public void Upsert(DiscordUser user)
    {
        var exists = db.DiscordUsers
            .AsNoTracking()
            .Any(candidate => candidate.DiscordUserId == user.DiscordUserId);

        if (exists)
        {
            db.Update(user);
        }
        else
        {
            db.Add(user);
        }

        db.SaveChanges();
        db.ChangeTracker.Clear();
    }
}
