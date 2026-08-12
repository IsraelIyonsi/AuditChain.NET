namespace AuditChain;

/// <summary>
/// Why <see cref="AuditChainVerifier.VerifyChain"/> rejected a chain.
/// </summary>
public enum ChainBreakReason
{
    /// <summary>
    /// A record's <see cref="AuditRecord.SequenceNumber"/> does not equal the previous
    /// record's sequence number plus one (or <see cref="AuditChainConstants.GenesisSequenceNumber"/>
    /// for the first record). This is what a deleted record (a sequence gap) and a
    /// reordered record both look like from the verifier's point of view: either tamper
    /// breaks the strictly increasing sequence at the same position.
    /// </summary>
    SequenceNumberMismatch,

    /// <summary>
    /// A record's <see cref="AuditRecord.PreviousHash"/> does not equal the previous
    /// record's <see cref="AuditRecord.Hash"/> (or <see cref="AuditChainConstants.GenesisPreviousHash"/>
    /// for the first record). The sequence numbers line up, but the hash link between the
    /// two records does not.
    /// </summary>
    PreviousHashMismatch,

    /// <summary>
    /// A record's own <see cref="AuditRecord.Hash"/> does not equal the hash recomputed
    /// from its sequence number, timestamp, payload, and previous hash. This is what a
    /// mutated payload and a directly forged hash both look like from the verifier's point
    /// of view: either tamper breaks the same equality check.
    /// </summary>
    RecordHashMismatch,
}
