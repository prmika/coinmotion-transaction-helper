using System.Collections.Concurrent;
using CryptoTaxHelper.Application.Interfaces;

namespace CryptoTaxHelper.Infrastructure.Reports;

public class InMemoryReportStore : IReportStore
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new();

    public string Store(byte[] reportData)
    {
        var id = Guid.NewGuid().ToString();
        _store[id] = reportData;
        return id;
    }

    public byte[]? Retrieve(string reportId)
    {
        _store.TryGetValue(reportId, out var data);
        return data;
    }

    public bool Remove(string reportId)
    {
        return _store.TryRemove(reportId, out _);
    }
}
