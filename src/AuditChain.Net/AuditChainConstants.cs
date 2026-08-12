namespace AuditChain;

/// <summary>
/// Fixed values that define the shape of an AuditChain.NET hash chain. These values are
/// part of the wire format: changing any of them changes every hash the library produces.
/// </summary>
public static class AuditChainConstants
{
    /// <summary>
    /// The length, in bytes, of a SHA-256 digest. Every <see cref="AuditRecord.Hash"/> and
    /// <see cref="AuditRecord.PreviousHash"/> value is exactly this many bytes long.
    /// </summary>
    public const int HashSizeInBytes = 32;

    /// <summary>
    /// The sequence number assigned to the first record appended to a chain (the genesis
    /// record).
    /// </summary>
    public const long GenesisSequenceNumber = 0L;

    /// <summary>
    /// The domain separation tag mixed into every hash computation. It ties a hash to the
    /// AuditChain.NET wire format so a digest produced by this library can never collide
    /// with a digest computed the same way by an unrelated hashing scheme.
    /// </summary>
    public const string HashDomainTag = "AuditChain.Net/v1";

    /// <summary>
    /// The fixed, all-zero previous-hash value that the genesis record chains to. There is
    /// no real predecessor for the first record, so the chain anchors to this well-known
    /// constant instead of a null or omitted value.
    /// </summary>
    public static byte[] GenesisPreviousHash => new byte[HashSizeInBytes];
}
