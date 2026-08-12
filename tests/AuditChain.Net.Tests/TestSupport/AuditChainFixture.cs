using System.Text;

namespace AuditChain.Net.Tests.TestSupport;

/// <summary>
/// Builds small, deterministic, valid audit chains for tests to tamper with.
/// </summary>
internal static class AuditChainFixture
{
    private static readonly DateTimeOffset FixtureStart = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan FixtureStep = TimeSpan.FromMinutes(1);

    public static List<AuditRecord> BuildValidChain(int recordCount)
    {
        var store = new InMemoryAuditStore();
        var clock = new SequentialAuditClock(FixtureStart, FixtureStep);
        var log = new AuditLog(store, clock);

        for (int i = 0; i < recordCount; i++)
        {
            log.Append(Encoding.UTF8.GetBytes($"fixture-event-{i}"));
        }

        return store.GetAll().ToList();
    }

    public static AuditRecord WithPayload(this AuditRecord record, byte[] payload) =>
        new(record.SequenceNumber, record.Timestamp, payload, record.PreviousHash.ToArray(), record.Hash.ToArray());

    public static AuditRecord WithHash(this AuditRecord record, byte[] hash) =>
        new(record.SequenceNumber, record.Timestamp, record.Payload.ToArray(), record.PreviousHash.ToArray(), hash);

    public static AuditRecord WithPreviousHash(this AuditRecord record, byte[] previousHash) =>
        new(record.SequenceNumber, record.Timestamp, record.Payload.ToArray(), previousHash, record.Hash.ToArray());

    public static AuditRecord WithSequenceNumber(this AuditRecord record, long sequenceNumber) =>
        new(sequenceNumber, record.Timestamp, record.Payload.ToArray(), record.PreviousHash.ToArray(), record.Hash.ToArray());

    public static byte[] AllBytes(byte value) => Enumerable.Repeat(value, AuditChainConstants.HashSizeInBytes).ToArray();
}
