using System.Text;

namespace AuditChain.Net.Tests.Records;

public sealed class AuditRecordTests
{
    private static readonly DateTimeOffset ReferenceTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly byte[] ReferencePayload = Encoding.UTF8.GetBytes("payload");
    private static readonly byte[] ReferencePreviousHash = new byte[AuditChainConstants.HashSizeInBytes];
    private static readonly byte[] ReferenceHash = Enumerable.Repeat((byte)0x42, AuditChainConstants.HashSizeInBytes).ToArray();

    [Fact]
    public void Constructor_StoresAllFieldsExactly()
    {
        var record = new AuditRecord(7, ReferenceTimestamp, ReferencePayload, ReferencePreviousHash, ReferenceHash);

        Assert.Equal(7, record.SequenceNumber);
        Assert.Equal(ReferenceTimestamp, record.Timestamp);
        Assert.Equal(ReferencePayload, record.Payload.ToArray());
        Assert.Equal(ReferencePreviousHash, record.PreviousHash.ToArray());
        Assert.Equal(ReferenceHash, record.Hash.ToArray());
    }

    [Fact]
    public void Constructor_MutatingCallersPayloadArrayAfterConstruction_DoesNotAffectTheRecord()
    {
        byte[] payload = Encoding.UTF8.GetBytes("original");
        var record = new AuditRecord(0, ReferenceTimestamp, payload, ReferencePreviousHash, ReferenceHash);

        payload[0] = 0xFF;

        Assert.Equal(Encoding.UTF8.GetBytes("original"), record.Payload.ToArray());
    }

    [Fact]
    public void Constructor_MutatingCallersPreviousHashArrayAfterConstruction_DoesNotAffectTheRecord()
    {
        byte[] previousHash = new byte[AuditChainConstants.HashSizeInBytes];
        var record = new AuditRecord(0, ReferenceTimestamp, ReferencePayload, previousHash, ReferenceHash);

        previousHash[0] = 0xFF;

        Assert.Equal(new byte[AuditChainConstants.HashSizeInBytes], record.PreviousHash.ToArray());
    }

    [Fact]
    public void Constructor_MutatingCallersHashArrayAfterConstruction_DoesNotAffectTheRecord()
    {
        byte[] hash = Enumerable.Repeat((byte)0x42, AuditChainConstants.HashSizeInBytes).ToArray();
        var record = new AuditRecord(0, ReferenceTimestamp, ReferencePayload, ReferencePreviousHash, hash);

        hash[0] = 0x00;

        Assert.Equal(ReferenceHash, record.Hash.ToArray());
    }

    public static IEnumerable<object[]> NullArgumentCases()
    {
        yield return new object[]
        {
            (Action)(() => new AuditRecord(0, ReferenceTimestamp, null!, ReferencePreviousHash, ReferenceHash)),
        };
        yield return new object[]
        {
            (Action)(() => new AuditRecord(0, ReferenceTimestamp, ReferencePayload, null!, ReferenceHash)),
        };
        yield return new object[]
        {
            (Action)(() => new AuditRecord(0, ReferenceTimestamp, ReferencePayload, ReferencePreviousHash, null!)),
        };
    }

    [Theory]
    [MemberData(nameof(NullArgumentCases))]
    public void Constructor_NullByteArrayArgument_ThrowsArgumentNullException(Action constructRecordWithNullArgument)
    {
        Assert.Throws<ArgumentNullException>(constructRecordWithNullArgument);
    }

    [Fact]
    public void Constructor_EmptyPayload_IsAllowed()
    {
        var record = new AuditRecord(0, ReferenceTimestamp, Array.Empty<byte>(), ReferencePreviousHash, ReferenceHash);

        Assert.Empty(record.Payload.ToArray());
    }
}
