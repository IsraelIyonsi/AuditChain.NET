using System.Text;

namespace AuditChain.Net.Tests.Hashing;

public sealed class AuditHasherTests
{
    private static readonly DateTimeOffset ReferenceTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly byte[] ReferencePayload = Encoding.UTF8.GetBytes("hello");
    private static readonly byte[] GenesisPreviousHash = AuditChainConstants.GenesisPreviousHash;

    [Fact]
    public void ComputeHash_KnownVector_MatchesRecordedRegressionValue()
    {
        const string expectedHex = "459468D3D58223F02995D3B65AC53BB55F9F6899D1F6A05879F8936E15EBD7FC";

        byte[] hash = AuditHasher.ComputeHash(
            sequenceNumber: 0,
            timestamp: ReferenceTimestamp,
            payload: ReferencePayload,
            previousHash: GenesisPreviousHash);

        Assert.Equal(expectedHex, Convert.ToHexString(hash));
    }

    [Fact]
    public void ComputeHash_SameInputsCalledTwice_ProducesTheSameBytes()
    {
        byte[] first = AuditHasher.ComputeHash(3, ReferenceTimestamp, ReferencePayload, GenesisPreviousHash);
        byte[] second = AuditHasher.ComputeHash(3, ReferenceTimestamp, ReferencePayload, GenesisPreviousHash);

        Assert.Equal(first, second);
    }

    [Fact]
    public void ComputeHash_AlwaysReturnsThirtyTwoBytes()
    {
        byte[] hashOfEmptyPayload = AuditHasher.ComputeHash(0, ReferenceTimestamp, Array.Empty<byte>(), GenesisPreviousHash);
        byte[] hashOfLargePayload = AuditHasher.ComputeHash(0, ReferenceTimestamp, new byte[10_000], GenesisPreviousHash);

        Assert.Equal(AuditChainConstants.HashSizeInBytes, hashOfEmptyPayload.Length);
        Assert.Equal(AuditChainConstants.HashSizeInBytes, hashOfLargePayload.Length);
    }

    [Fact]
    public void ComputeHash_SameInstantExpressedWithDifferentUtcOffsets_ProducesTheSameBytes()
    {
        var utc = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var plusOneRepresentation = new DateTimeOffset(2026, 6, 15, 13, 0, 0, TimeSpan.FromHours(1));

        byte[] hashFromUtc = AuditHasher.ComputeHash(1, utc, ReferencePayload, GenesisPreviousHash);
        byte[] hashFromOffsetRepresentation = AuditHasher.ComputeHash(1, plusOneRepresentation, ReferencePayload, GenesisPreviousHash);

        Assert.Equal(hashFromUtc, hashFromOffsetRepresentation);
    }

    public static IEnumerable<object[]> FieldMutationCases()
    {
        yield return new object[]
        {
            "sequence number",
            (Func<byte[]>)(() => AuditHasher.ComputeHash(1, ReferenceTimestamp, ReferencePayload, GenesisPreviousHash)),
        };
        yield return new object[]
        {
            "timestamp",
            (Func<byte[]>)(() => AuditHasher.ComputeHash(0, ReferenceTimestamp.AddSeconds(1), ReferencePayload, GenesisPreviousHash)),
        };
        yield return new object[]
        {
            "payload",
            (Func<byte[]>)(() => AuditHasher.ComputeHash(0, ReferenceTimestamp, Encoding.UTF8.GetBytes("hellp"), GenesisPreviousHash)),
        };
        yield return new object[]
        {
            "previous hash",
            (Func<byte[]>)(() => AuditHasher.ComputeHash(0, ReferenceTimestamp, ReferencePayload, Enumerable.Repeat((byte)0x01, AuditChainConstants.HashSizeInBytes).ToArray())),
        };
    }

    [Theory]
    [MemberData(nameof(FieldMutationCases))]
    public void ComputeHash_ChangingAnySingleField_ChangesTheHash(string fieldName, Func<byte[]> computeWithOneFieldChanged)
    {
        byte[] baseline = AuditHasher.ComputeHash(0, ReferenceTimestamp, ReferencePayload, GenesisPreviousHash);
        byte[] mutated = computeWithOneFieldChanged();

        Assert.False(baseline.AsSpan().SequenceEqual(mutated), $"changing the {fieldName} should change the hash");
    }

    [Fact]
    public void ComputeHash_EmptyPayloadAndEmptyPreviousHash_DoesNotThrow()
    {
        byte[] hash = AuditHasher.ComputeHash(0, ReferenceTimestamp, Array.Empty<byte>(), Array.Empty<byte>());

        Assert.Equal(AuditChainConstants.HashSizeInBytes, hash.Length);
    }

    [Fact]
    public void ComputeHash_NegativeSequenceNumber_StillProducesADeterministicHash()
    {
        byte[] first = AuditHasher.ComputeHash(-1, ReferenceTimestamp, ReferencePayload, GenesisPreviousHash);
        byte[] second = AuditHasher.ComputeHash(-1, ReferenceTimestamp, ReferencePayload, GenesisPreviousHash);

        Assert.Equal(first, second);
    }
}
