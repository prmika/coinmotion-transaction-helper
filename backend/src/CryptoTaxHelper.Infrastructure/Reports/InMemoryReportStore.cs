using System.Collections.Concurrent;
using CryptoTaxHelper.Application.Interfaces;

namespace CryptoTaxHelper.Infrastructure.Reports;

public class InMemoryReportStore : IReportStore, IExpiringReportStore
{
    private readonly ConcurrentDictionary<string, StoredReport> _store = new();
    private readonly TimeSpan _lifetime;
    private readonly TimeProvider _timeProvider;

    public InMemoryReportStore(TimeSpan? lifetime = null, TimeProvider? timeProvider = null)
    {
        _lifetime = lifetime ?? TimeSpan.FromMinutes(60);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public string Store(byte[] reportData)
    {
        var id = Guid.NewGuid().ToString();
        DateTimeOffset? expiresAt = _lifetime > TimeSpan.Zero ? _timeProvider.GetUtcNow() + _lifetime : null;
        _store[id] = new StoredReport(reportData, expiresAt);
        return id;
    }

    public byte[]? Retrieve(string reportId)
    {
        if (!_store.TryGetValue(reportId, out var report))
            return null;

        if (report.ExpiresAt is { } expiry && _timeProvider.GetUtcNow() >= expiry)
        {
            _store.TryRemove(reportId, out _);
            return null;
        }

        return report.Data;
    }

    public bool Remove(string reportId)
    {
        return _store.TryRemove(reportId, out _);
    }

    public int RemoveExpired()
    {
        var now = _timeProvider.GetUtcNow();
        var removed = 0;

        foreach (var (id, report) in _store)
        {
            if (report.ExpiresAt is { } expiry && now >= expiry && _store.TryRemove(id, out _))
                removed++;
        }

        return removed;
    }

    private sealed record StoredReport(byte[] Data, DateTimeOffset? ExpiresAt);
}
