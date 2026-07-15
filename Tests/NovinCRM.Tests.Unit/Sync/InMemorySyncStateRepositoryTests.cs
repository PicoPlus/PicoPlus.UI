using Microsoft.Extensions.Caching.Memory;
using NovinCRM.Infrastructure.Sync;
using Xunit;
using FluentAssertions;

namespace NovinCRM.Tests.Unit.Sync;

/// <summary>
/// Unit tests for <see cref="InMemorySyncStateRepository"/>.
/// Verifies idempotency and basic CRUD semantics.
/// </summary>
public class InMemorySyncStateRepositoryTests : IDisposable
{
    private readonly IMemoryCache              _cache;
    private readonly InMemorySyncStateRepository _sut;

    public InMemorySyncStateRepositoryTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut   = new InMemorySyncStateRepository(_cache);
    }

    [Fact]
    public async Task GetVersionAsync_NonExistentKey_ReturnsNull()
    {
        var result = await _sut.GetVersionAsync("contact", "42");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAndGetVersion_RoundTrips()
    {
        await _sut.SetVersionAsync("deal", "99", versionMs: 1_000L, eventId: "evt-1");

        var result = await _sut.GetVersionAsync("deal", "99");

        result.Should().Be(1_000L);
    }

    [Fact]
    public async Task IsProcessedAsync_AfterSet_ReturnsTrue()
    {
        await _sut.SetVersionAsync("contact", "10", versionMs: 500L, eventId: "unique-event-id");

        var processed = await _sut.IsProcessedAsync("unique-event-id");

        processed.Should().BeTrue();
    }

    [Fact]
    public async Task IsProcessedAsync_BeforeSet_ReturnsFalse()
    {
        var processed = await _sut.IsProcessedAsync("not-yet-seen");

        processed.Should().BeFalse();
    }

    [Fact]
    public async Task MarkDeletedAsync_ThenIsDeleted_ReturnsTrue()
    {
        await _sut.MarkDeletedAsync("contact", "deleted-id");

        var deleted = await _sut.IsDeletedAsync("contact", "deleted-id");

        deleted.Should().BeTrue();
    }

    [Fact]
    public async Task IsDeletedAsync_NotDeleted_ReturnsFalse()
    {
        var deleted = await _sut.IsDeletedAsync("contact", "alive-id");

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task SetVersionAsync_CalledTwice_UpdatesVersion()
    {
        await _sut.SetVersionAsync("deal", "77", versionMs: 100L, eventId: "evt-a");
        await _sut.SetVersionAsync("deal", "77", versionMs: 200L, eventId: "evt-b");

        var result = await _sut.GetVersionAsync("deal", "77");

        result.Should().Be(200L);
    }

    public void Dispose() => _cache.Dispose();
}
