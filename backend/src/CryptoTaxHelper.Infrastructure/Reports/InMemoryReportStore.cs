using System.Collections.Concurrent;
using CryptoTaxHelper.Application.Interfaces;

namespace CryptoTaxHelper.Infrastructure.Reports;

public class InMemoryReportStore : IReportStore
{
    private readonly ConcurrentDictionary<string, ReportEntry> _store = new();
    private readonly TimeSpan _retention = TimeSpan.FromMinutes(30);

    public string Store(byte[] reportData, string ownerId)
    {
        var id = Guid.NewGuid().ToString();
        _store[id] = new ReportEntry(reportData, ownerId, DateTimeOffset.UtcNow.Add(_retention));
        return id;
    }

    public byte[]? Consume(string reportId, string ownerId)
    {
        if (!_store.TryGetValue(reportId, out var entry)) return null;
        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            RemoveEntry(reportId, entry);
            return null;
        }
        if (entry.OwnerId != ownerId) return null;
        return RemoveEntry(reportId, entry) ? entry.Data : null;
    }

    private bool RemoveEntry(string reportId, ReportEntry entry) =>
        ((ICollection<KeyValuePair<string, ReportEntry>>)_store)
            .Remove(new KeyValuePair<string, ReportEntry>(reportId, entry));

    private sealed record ReportEntry(byte[] Data, string OwnerId, DateTimeOffset ExpiresAt);
}
