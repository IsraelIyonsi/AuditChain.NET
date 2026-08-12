namespace AuditChain;

/// <summary>
/// A single sealed entry in an audit chain.
/// </summary>
/// <remarks>
/// An <see cref="AuditRecord"/> is an immutable snapshot: its <see cref="Hash"/> is
/// computed once, at construction time, from its own fields. Nothing about the record
/// changes after that, so any later mutation of the underlying store is exactly what
/// <see cref="AuditChainVerifier.VerifyChain"/> is built to detect.
/// </remarks>
public sealed class AuditRecord
{
    /// <summary>
    /// Initializes a new sealed audit record.
    /// </summary>
    /// <param name="sequenceNumber">The record's position in the chain.</param>
    /// <param name="timestamp">The instant the record was appended.</param>
    /// <param name="payload">The record's payload bytes.</param>
    /// <param name="previousHash">The hash of the preceding record, or
    /// <see cref="AuditChainConstants.GenesisPreviousHash"/> for the first record.</param>
    /// <param name="hash">The record's own hash, as computed by <see cref="AuditHasher.ComputeHash"/>.</param>
    /// <remarks>
    /// This constructor performs no hashing and no validation: it stores exactly the
    /// bytes it is given. <see cref="AuditLog.Append(byte[])"/> uses it to seal correctly
    /// hashed records, and an <see cref="IAuditStore"/> implementation uses it to
    /// rehydrate records read back from storage. Because it accepts an arbitrary
    /// <paramref name="hash"/>, it is also how test code builds deliberately tampered
    /// records to exercise <see cref="AuditChainVerifier.VerifyChain"/>.
    /// </remarks>
    public AuditRecord(
        long sequenceNumber,
        DateTimeOffset timestamp,
        byte[] payload,
        byte[] previousHash,
        byte[] hash)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(previousHash);
        ArgumentNullException.ThrowIfNull(hash);

        SequenceNumber = sequenceNumber;
        Timestamp = timestamp;
        Payload = (byte[])payload.Clone();
        PreviousHash = (byte[])previousHash.Clone();
        Hash = (byte[])hash.Clone();
    }

    /// <summary>
    /// Gets the record's position in the chain, starting at
    /// <see cref="AuditChainConstants.GenesisSequenceNumber"/> for the first record and
    /// incrementing by one for each subsequent record.
    /// </summary>
    public long SequenceNumber { get; }

    /// <summary>
    /// Gets the instant the record was appended, as supplied by the <see cref="IAuditClock"/>
    /// passed to the <see cref="AuditLog"/> that created it.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the record's payload bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>
    /// Gets the hash of the preceding record in the chain, or
    /// <see cref="AuditChainConstants.GenesisPreviousHash"/> if this is the first record.
    /// </summary>
    public ReadOnlyMemory<byte> PreviousHash { get; }

    /// <summary>
    /// Gets this record's own SHA-256 hash, computed over its sequence number, timestamp,
    /// payload, and previous hash. See <see cref="AuditHasher.ComputeHash"/> for the exact
    /// byte layout.
    /// </summary>
    public ReadOnlyMemory<byte> Hash { get; }
}
