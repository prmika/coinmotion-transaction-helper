using CryptoTaxHelper.Infrastructure.Reports;
using FluentAssertions;
using Xunit;

namespace CryptoTaxHelper.Application.Tests;

public class InMemoryReportStoreTests
{
    [Fact]
    public void Retrieve_ExpiresReportAfterConfiguredLifetime()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var store = new InMemoryReportStore(TimeSpan.FromMinutes(60), clock);
        var reportId = store.Store([1, 2, 3]);

        clock.Advance(TimeSpan.FromMinutes(59));
        store.Retrieve(reportId).Should().NotBeNull();

        clock.Advance(TimeSpan.FromMinutes(1));
        store.Retrieve(reportId).Should().BeNull();
    }

    [Fact]
    public void DefaultLifetime_ExpiresReportAfter60Minutes()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var store = new InMemoryReportStore(timeProvider: clock);
        var reportId = store.Store([1, 2, 3]);

        clock.Advance(TimeSpan.FromMinutes(60));

        store.Retrieve(reportId).Should().BeNull();
    }

    [Fact]
    public void RemoveExpired_CleansExpiredReportsFromMemoryStore()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var store = new InMemoryReportStore(TimeSpan.FromMinutes(60), clock);
        var reportId = store.Store([1, 2, 3]);

        clock.Advance(TimeSpan.FromHours(1));

        store.RemoveExpired().Should().Be(1);
        store.Retrieve(reportId).Should().BeNull();
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now += duration;
    }
}
