using System.Text;

namespace AuditChain.Net.Tests.Store;

public sealed class InMemoryAuditStoreTests
{
    private static readonly DateTimeOffset ReferenceTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GetLast_EmptyStore_ReturnsNull()
    {
        var store = new InMemoryAuditStore();

        Assert.Null(store.GetLast());
    }

    [Fact]
    public void GetAll_EmptyStore_ReturnsEmptyCollection()
    {
        var store = new InMemoryAuditStore();

        Assert.Empty(store.GetAll());
    }

    [Fact]
    public void Append_ThenGetLast_ReturnsTheAppendedRecord()
    {
        var store = new InMemoryAuditStore();
        AuditRecord record = BuildRecord(0);

        store.Append(record);

        Assert.Same(record, store.GetLast());
    }

    [Fact]
    public void Append_MultipleRecords_GetAllReturnsThemInAppendOrder()
    {
        var store = new InMemoryAuditStore();
        AuditRecord first = BuildRecord(0);
        AuditRecord second = BuildRecord(1);
        AuditRecord third = BuildRecord(2);

        store.Append(first);
        store.Append(second);
        store.Append(third);

        Assert.Equal(new[] { first, second, third }, store.GetAll());
    }

    [Fact]
    public void Append_MultipleRecords_GetLastReturnsTheMostRecentOne()
    {
        var store = new InMemoryAuditStore();
        store.Append(BuildRecord(0));
        AuditRecord last = BuildRecord(1);
        store.Append(last);

        Assert.Same(last, store.GetLast());
    }

    [Fact]
    public void GetAll_ReturnsASnapshot_LaterAppendsDoNotAffectPreviouslyReturnedCollection()
    {
        var store = new InMemoryAuditStore();
        store.Append(BuildRecord(0));

        IReadOnlyList<AuditRecord> snapshot = store.GetAll();
        store.Append(BuildRecord(1));

        Assert.Single(snapshot);
    }

    [Fact]
    public void Append_NullRecord_ThrowsArgumentNullException()
    {
        var store = new InMemoryAuditStore();

        Assert.Throws<ArgumentNullException>(() => store.Append(null!));
    }

    [Fact]
    public async Task Append_FromManyThreadsConcurrently_LosesNoRecords()
    {
        const int threadCount = 20;
        const int appendsPerThread = 50;

        var store = new InMemoryAuditStore();
        var threads = Enumerable.Range(0, threadCount)
            .Select(threadIndex => Task.Run(() =>
            {
                for (int i = 0; i < appendsPerThread; i++)
                {
                    store.Append(BuildRecord(threadIndex * appendsPerThread + i));
                }
            }))
            .ToArray();

        await Task.WhenAll(threads);

        Assert.Equal(threadCount * appendsPerThread, store.GetAll().Count);
    }

    private static AuditRecord BuildRecord(long sequenceNumber)
    {
        byte[] payload = Encoding.UTF8.GetBytes($"payload-{sequenceNumber}");
        byte[] previousHash = AuditChainConstants.GenesisPreviousHash;
        byte[] hash = AuditHasher.ComputeHash(sequenceNumber, ReferenceTimestamp, payload, previousHash);
        return new AuditRecord(sequenceNumber, ReferenceTimestamp, payload, previousHash, hash);
    }
}
