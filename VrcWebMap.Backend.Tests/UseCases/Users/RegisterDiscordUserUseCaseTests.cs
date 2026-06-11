using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Users;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.UseCases.Users;

public sealed class RegisterDiscordUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_GuildMember_CreatesUser()
    {
        var repository = new FakeDiscordUserRepository();
        var useCase = new RegisterDiscordUserUseCase(repository);
        var request = new RegisterDiscordUser.Request(
            " 123456789012345678 ",
            " test-user ",
            " Test User ",
            " avatar_hash ",
            " 987654321098765432 ",
            IsRequiredGuildMember: true);

        var before = DateTimeOffset.UtcNow;
        var result = await useCase.ExecuteAsync(request);
        var after = DateTimeOffset.UtcNow;

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("123456789012345678", result.Value.User.DiscordUserId);
        Assert.Equal("test-user", result.Value.User.Username);
        Assert.Equal("Test User", result.Value.User.GlobalName);
        Assert.Equal("avatar_hash", result.Value.User.AvatarHash);
        Assert.Equal("987654321098765432", result.Value.User.RequiredGuildId);
        Assert.True(result.Value.User.IsGuildMember);
        Assert.False(result.Value.User.IsAdmin);
        Assert.InRange(result.Value.User.RegisteredAt, before, after);
        Assert.InRange(result.Value.User.LastSeenAt, before, after);
        Assert.Single(repository.SavedUsers);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingUser_PreservesRegisteredAt()
    {
        var registeredAt = DateTimeOffset.UtcNow.AddDays(-1);
        var existing = new DiscordUser(
            "123456789012345678",
            "old-user",
            null,
            null,
            "987654321098765432",
            IsGuildMember: true,
            IsAdmin: false,
            registeredAt,
            registeredAt);
        var repository = new FakeDiscordUserRepository(existing);
        var useCase = new RegisterDiscordUserUseCase(repository);
        var request = new RegisterDiscordUser.Request(
            "123456789012345678",
            "new-user",
            null,
            null,
            "987654321098765432",
            IsRequiredGuildMember: true,
            IsAdmin: true);

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(registeredAt, result.Value.User.RegisteredAt);
        Assert.True(result.Value.User.LastSeenAt > registeredAt);
        Assert.Equal("new-user", result.Value.User.Username);
        Assert.True(result.Value.User.IsAdmin);
    }

    [Fact]
    public async Task ExecuteAsync_NotGuildMember_ReturnsForbidden()
    {
        var repository = new FakeDiscordUserRepository();
        var useCase = new RegisterDiscordUserUseCase(repository);
        var request = new RegisterDiscordUser.Request(
            "123456789012345678",
            "test-user",
            null,
            null,
            "987654321098765432",
            IsRequiredGuildMember: false);

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Forbidden, result.Error.Kind);
        Assert.Empty(repository.SavedUsers);
    }

    [Fact]
    public async Task ExecuteAsync_MissingRequiredValues_ReturnsValidation()
    {
        var repository = new FakeDiscordUserRepository();
        var useCase = new RegisterDiscordUserUseCase(repository);
        var request = new RegisterDiscordUser.Request(
            " ",
            "test-user",
            null,
            null,
            "987654321098765432",
            IsRequiredGuildMember: true);

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Validation, result.Error.Kind);
        Assert.Empty(repository.SavedUsers);
    }
}
