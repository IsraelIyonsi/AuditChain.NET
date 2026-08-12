using System.Text;
using AuditChain.Net.Tests.TestSupport;

namespace AuditChain.Net.Tests.Chain;

public sealed class AuditLogTests
{
    private static readonly DateTimeOffset FixtureStart = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan FixtureStep = TimeSpan.FromSeconds(30);

    [Fact]
    public void Append_FirstRecord_IsGenesisRecord()
    {
        var log = new AuditLog(new InMemoryAuditStore(), new SequentialAuditClock(FixtureStart, FixtureStep));

        AuditRecord record = log.Append(Encoding.UTF8.GetBytes("first-event"));

        Assert.Equal(AuditChainConstants.GenesisSequenceNumber, record.SequenceNumber);
        Assert.Equal(AuditChainConstants.GenesisPreviousHash, record.PreviousHash.ToArray());
        Assert.Equal(AuditChainConstants.HashSizeInBytes, record.Hash.Length);
    }

    [Fact]
    public void Append_UsesInjectedClock_NeverTheSystemClock()
    {
        var farFuture = new DateTimeOffset(2099, 12, 31, 23, 59, 59, TimeSpan.Zero);
        var log = new AuditLog(new InMemoryAuditStore(), new SequentialAuditClock(farFuture, FixtureStep));

        AuditRecord record = log.Append(Encoding.UTF8.GetBytes("future-event"));

        Assert.Equal(farFuture, record.Timestamp);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(50)]
    public void Append_ManyRecords_ProducesStrictlyIncreasingSequenceAndValidChain(int recordCount)
    {
        var store = new InMemoryAuditStore();
        var log = new AuditLog(store, new SequentialAuditClock(FixtureStart, FixtureStep));

        for (int i = 0; i < recordCount; i++)
        {
            log.Append(Encoding.UTF8.GetBytes($"event-{i}"));
        }

        IReadOnlyList<AuditRecord> all = log.GetAll();

        Assert.Equal(recordCount, all.Count);
        for (int i = 0; i < recordCount; i++)
        {
            Assert.Equal(i, all[i].SequenceNumber);
        }

        ChainVerificationResult result = AuditChainVerifier.VerifyChain(all);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Append_SecondRecord_ChainsToFirstRecordsHash()
    {
        var store = new InMemoryAuditStore();
        var log = new AuditLog(store, new SequentialAuditClock(FixtureStart, FixtureStep));

        AuditRecord first = log.Append(Encoding.UTF8.GetBytes("event-a"));
        AuditRecord second = log.Append(Encoding.UTF8.GetBytes("event-b"));

        Assert.Equal(first.Hash.ToArray(), second.PreviousHash.ToArray());
        Assert.Equal(first.SequenceNumber + 1, second.SequenceNumber);
    }

    [Fact]
    public void Append_RecordHash_MatchesIndependentlyComputedHash()
    {
        var store = new InMemoryAuditStore();
        var clock = new SequentialAuditClock(FixtureStart, FixtureStep);
        var log = new AuditLog(store, clock);

        byte[] payload = Encoding.UTF8.GetBytes("independently-verifiable-event");
        AuditRecord record = log.Append(payload);

        byte[] expectedHash = AuditHasher.ComputeHash(
            record.SequenceNumber,
            record.Timestamp,
            payload,
            AuditChainConstants.GenesisPreviousHash);

        Assert.Equal(expectedHash, record.Hash.ToArray());
    }

    [Fact]
    public void Append_StringOverload_UsesUtf8EncodingOfThePayload()
    {
        var log = new AuditLog(new InMemoryAuditStore(), new SequentialAuditClock(FixtureStart, FixtureStep));

        AuditRecord record = log.Append("héllo wörld");

        Assert.Equal(Encoding.UTF8.GetBytes("héllo wörld"), record.Payload.ToArray());
    }

    [Fact]
    public void Append_MutatingCallersPayloadArrayAfterward_DoesNotAffectStoredRecord()
    {
        var log = new AuditLog(new InMemoryAuditStore(), new SequentialAuditClock(FixtureStart, FixtureStep));
        byte[] payload = Encoding.UTF8.GetBytes("original");

        AuditRecord record = log.Append(payload);
        payload[0] = 0x00;

        Assert.Equal(Encoding.UTF8.GetBytes("original"), record.Payload.ToArray());
    }

    [Fact]
    public void Constructor_NullStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AuditLog(null!, new SystemAuditClock()));
    }

    [Fact]
    public void Constructor_NullClock_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AuditLog(new InMemoryAuditStore(), null!));
    }

    [Fact]
    public void Append_NullBytePayload_ThrowsArgumentNullException()
    {
        var log = new AuditLog(new InMemoryAuditStore(), new SequentialAuditClock(FixtureStart, FixtureStep));

        Assert.Throws<ArgumentNullException>(() => log.Append((byte[])null!));
    }

    [Fact]
    public void Append_NullStringPayload_ThrowsArgumentNullException()
    {
        var log = new AuditLog(new InMemoryAuditStore(), new SequentialAuditClock(FixtureStart, FixtureStep));

        Assert.Throws<ArgumentNullException>(() => log.Append((string)null!));
    }

    [Fact]
    public async Task Append_ConcurrentCallers_ProduceAGaplessValidChain()
    {
        const int callerCount = 16;
        const int appendsPerCaller = 25;
        const int totalAppends = callerCount * appendsPerCaller;

        var store = new InMemoryAuditStore();
        var log = new AuditLog(store, new SequentialAuditClock(FixtureStart, FixtureStep));

        var callers = Enumerable.Range(0, callerCount)
            .Select(callerIndex => Task.Run(() =>
            {
                for (int i = 0; i < appendsPerCaller; i++)
                {
                    log.Append(Encoding.UTF8.GetBytes($"caller-{callerIndex}-event-{i}"));
                }
            }))
            .ToArray();

        await Task.WhenAll(callers);

        IReadOnlyList<AuditRecord> all = log.GetAll();

        Assert.Equal(totalAppends, all.Count);
        Assert.Equal(Enumerable.Range(0, totalAppends).Select(i => (long)i), all.Select(r => r.SequenceNumber));
        Assert.True(AuditChainVerifier.VerifyChain(all).IsValid);
    }
}
