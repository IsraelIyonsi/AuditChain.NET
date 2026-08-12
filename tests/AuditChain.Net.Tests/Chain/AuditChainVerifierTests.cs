using AuditChain.Net.Tests.TestSupport;

namespace AuditChain.Net.Tests.Chain;

public sealed class AuditChainVerifierTests
{
    private const int FixtureChainLength = 5;
    private static readonly byte[] ForgedHashBytes = AuditChainFixture.AllBytes(0xFF);
    private static readonly byte[] ForgedPreviousHashBytes = AuditChainFixture.AllBytes(0xAB);
    private static readonly byte[] TamperedPayloadBytes = { 0x54, 0x41, 0x4D, 0x50, 0x45, 0x52, 0x45, 0x44 };

    [Fact]
    public void VerifyChain_EmptySequence_IsValid()
    {
        ChainVerificationResult result = AuditChainVerifier.VerifyChain(Array.Empty<AuditRecord>());

        Assert.True(result.IsValid);
        Assert.Null(result.BrokenRecordIndex);
        Assert.Null(result.Reason);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(50)]
    public void VerifyChain_UntamperedChainOfAnyLength_IsValid(int recordCount)
    {
        List<AuditRecord> chain = AuditChainFixture.BuildValidChain(recordCount);

        ChainVerificationResult result = AuditChainVerifier.VerifyChain(chain);

        Assert.True(result.IsValid);
    }

    public static IEnumerable<object[]> TamperClassCases()
    {
        yield return new object[]
        {
            "mutated payload",
            (Func<List<AuditRecord>, List<AuditRecord>>)(chain => Tamper(chain, 2, r => r.WithPayload(TamperedPayloadBytes))),
            2,
            ChainBreakReason.RecordHashMismatch,
        };

        yield return new object[]
        {
            "forged hash",
            (Func<List<AuditRecord>, List<AuditRecord>>)(chain => Tamper(chain, 3, r => r.WithHash(ForgedHashBytes))),
            3,
            ChainBreakReason.RecordHashMismatch,
        };

        yield return new object[]
        {
            "deleted record (sequence gap)",
            (Func<List<AuditRecord>, List<AuditRecord>>)(chain => Delete(chain, 2)),
            2,
            ChainBreakReason.SequenceNumberMismatch,
        };

        yield return new object[]
        {
            "reordered record",
            (Func<List<AuditRecord>, List<AuditRecord>>)(chain => Swap(chain, 1, 2)),
            1,
            ChainBreakReason.SequenceNumberMismatch,
        };

        yield return new object[]
        {
            "forged previous-hash pointer",
            (Func<List<AuditRecord>, List<AuditRecord>>)(chain => Tamper(chain, 2, r => r.WithPreviousHash(ForgedPreviousHashBytes))),
            2,
            ChainBreakReason.PreviousHashMismatch,
        };

        yield return new object[]
        {
            "forged genesis previous hash",
            (Func<List<AuditRecord>, List<AuditRecord>>)(chain => Tamper(chain, 0, r => r.WithPreviousHash(ForgedPreviousHashBytes))),
            0,
            ChainBreakReason.PreviousHashMismatch,
        };

        yield return new object[]
        {
            "tampered genesis sequence number",
            (Func<List<AuditRecord>, List<AuditRecord>>)(chain => Tamper(chain, 0, r => r.WithSequenceNumber(7))),
            0,
            ChainBreakReason.SequenceNumberMismatch,
        };

        yield return new object[]
        {
            "mutated payload on the last record",
            (Func<List<AuditRecord>, List<AuditRecord>>)(chain => Tamper(chain, FixtureChainLength - 1, r => r.WithPayload(TamperedPayloadBytes))),
            FixtureChainLength - 1,
            ChainBreakReason.RecordHashMismatch,
        };
    }

    [Theory]
    [MemberData(nameof(TamperClassCases))]
    public void VerifyChain_EachTamperClass_ReportsFirstBreakIndexAndReason(
        string tamperClassName,
        Func<List<AuditRecord>, List<AuditRecord>> tamper,
        int expectedIndex,
        ChainBreakReason expectedReason)
    {
        List<AuditRecord> validChain = AuditChainFixture.BuildValidChain(FixtureChainLength);
        List<AuditRecord> tamperedChain = tamper(validChain);

        ChainVerificationResult result = AuditChainVerifier.VerifyChain(tamperedChain);

        Assert.False(result.IsValid, $"tamper class '{tamperClassName}' should have broken the chain");
        Assert.Equal(expectedIndex, result.BrokenRecordIndex);
        Assert.Equal(expectedReason, result.Reason);
    }

    [Fact]
    public void VerifyChain_StopsAtFirstBreak_LaterTamperIsNotReported()
    {
        List<AuditRecord> chain = AuditChainFixture.BuildValidChain(FixtureChainLength);
        List<AuditRecord> tampered = Tamper(chain, 1, r => r.WithPayload(TamperedPayloadBytes));
        tampered = Tamper(tampered, 3, r => r.WithHash(ForgedHashBytes));

        ChainVerificationResult result = AuditChainVerifier.VerifyChain(tampered);

        Assert.False(result.IsValid);
        Assert.Equal(1, result.BrokenRecordIndex);
        Assert.Equal(ChainBreakReason.RecordHashMismatch, result.Reason);
    }

    [Fact]
    public void VerifyChain_NullSequence_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => AuditChainVerifier.VerifyChain(null!));
    }

    private static List<AuditRecord> Tamper(List<AuditRecord> chain, int index, Func<AuditRecord, AuditRecord> mutate)
    {
        var copy = new List<AuditRecord>(chain);
        copy[index] = mutate(copy[index]);
        return copy;
    }

    private static List<AuditRecord> Delete(List<AuditRecord> chain, int index)
    {
        var copy = new List<AuditRecord>(chain);
        copy.RemoveAt(index);
        return copy;
    }

    private static List<AuditRecord> Swap(List<AuditRecord> chain, int indexA, int indexB)
    {
        var copy = new List<AuditRecord>(chain);
        (copy[indexA], copy[indexB]) = (copy[indexB], copy[indexA]);
        return copy;
    }
}
