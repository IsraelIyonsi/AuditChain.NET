namespace AuditChain;

/// <summary>
/// Verifies that a sequence of <see cref="AuditRecord"/> instances forms an intact hash
/// chain.
/// </summary>
public static class AuditChainVerifier
{
    /// <summary>
    /// Walks a sequence of records in order and checks that each one correctly chains to
    /// the one before it.
    /// </summary>
    /// <param name="records">The records to verify, in the order they claim to occupy in
    /// the chain. An empty sequence is trivially valid.</param>
    /// <returns>
    /// <see cref="ChainVerificationResult.Success"/> if every record chains correctly, or
    /// the first break found via <see cref="ChainVerificationResult.Failure"/> otherwise.
    /// </returns>
    /// <remarks>
    /// For each record, in order, this method checks three things: that its sequence
    /// number equals the expected next sequence number, that its previous hash equals the
    /// preceding record's hash, and that its own hash matches one recomputed from its
    /// fields. The first check that fails determines the reported
    /// <see cref="ChainBreakReason"/>, and verification stops there without inspecting the
    /// remainder of the sequence.
    /// </remarks>
    public static ChainVerificationResult VerifyChain(IEnumerable<AuditRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        AuditRecord? previous = null;
        int index = 0;

        foreach (AuditRecord record in records)
        {
            long expectedSequenceNumber = previous is null
                ? AuditChainConstants.GenesisSequenceNumber
                : previous.SequenceNumber + 1;

            if (record.SequenceNumber != expectedSequenceNumber)
            {
                return ChainVerificationResult.Failure(index, ChainBreakReason.SequenceNumberMismatch);
            }

            ReadOnlySpan<byte> expectedPreviousHash = previous is null
                ? AuditChainConstants.GenesisPreviousHash
                : previous.Hash.Span;

            if (!record.PreviousHash.Span.SequenceEqual(expectedPreviousHash))
            {
                return ChainVerificationResult.Failure(index, ChainBreakReason.PreviousHashMismatch);
            }

            byte[] recomputedHash = AuditHasher.ComputeHash(
                record.SequenceNumber,
                record.Timestamp,
                record.Payload.Span,
                record.PreviousHash.Span);

            if (!record.Hash.Span.SequenceEqual(recomputedHash))
            {
                return ChainVerificationResult.Failure(index, ChainBreakReason.RecordHashMismatch);
            }

            previous = record;
            index++;
        }

        return ChainVerificationResult.Success();
    }
}
