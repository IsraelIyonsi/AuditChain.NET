using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace AuditChain;

/// <summary>
/// Computes the canonical SHA-256 hash of an audit record from its constituent fields.
/// </summary>
/// <remarks>
/// The byte layout is fixed and length-prefixed rather than built from culture-dependent
/// string formatting (no <see cref="object.ToString()"/> of numbers or dates anywhere in
/// this type), so the same inputs always hash to the same output on every machine,
/// operating system, and .NET culture setting.
/// </remarks>
public static class AuditHasher
{
    private static readonly byte[] DomainTagBytes = Encoding.UTF8.GetBytes(AuditChainConstants.HashDomainTag);

    private const int SequenceNumberFieldLength = sizeof(long);
    private const int TimestampFieldLength = sizeof(long);
    private const int LengthPrefixFieldLength = sizeof(int);

    /// <summary>
    /// Computes the deterministic SHA-256 hash for a record with the given fields.
    /// </summary>
    /// <param name="sequenceNumber">The record's position in the chain, starting at
    /// <see cref="AuditChainConstants.GenesisSequenceNumber"/>.</param>
    /// <param name="timestamp">The instant the record was appended, as supplied by an
    /// <see cref="IAuditClock"/>.</param>
    /// <param name="payload">The record's payload bytes.</param>
    /// <param name="previousHash">The hash of the preceding record, or
    /// <see cref="AuditChainConstants.GenesisPreviousHash"/> for the first record.</param>
    /// <returns>A new <see cref="AuditChainConstants.HashSizeInBytes"/>-byte SHA-256 digest.</returns>
    public static byte[] ComputeHash(
        long sequenceNumber,
        DateTimeOffset timestamp,
        ReadOnlySpan<byte> payload,
        ReadOnlySpan<byte> previousHash)
    {
        int bufferLength =
            LengthPrefixFieldLength + DomainTagBytes.Length +
            SequenceNumberFieldLength +
            TimestampFieldLength +
            LengthPrefixFieldLength + payload.Length +
            LengthPrefixFieldLength + previousHash.Length;

        byte[] rented = new byte[bufferLength];
        Span<byte> buffer = rented;
        int offset = 0;

        offset += WriteLengthPrefixed(buffer[offset..], DomainTagBytes);

        BinaryPrimitives.WriteInt64BigEndian(buffer.Slice(offset, SequenceNumberFieldLength), sequenceNumber);
        offset += SequenceNumberFieldLength;

        BinaryPrimitives.WriteInt64BigEndian(buffer.Slice(offset, TimestampFieldLength), timestamp.UtcTicks);
        offset += TimestampFieldLength;

        offset += WriteLengthPrefixed(buffer[offset..], payload);

        offset += WriteLengthPrefixed(buffer[offset..], previousHash);

        return SHA256.HashData(buffer[..offset]);
    }

    private static int WriteLengthPrefixed(Span<byte> destination, ReadOnlySpan<byte> value)
    {
        BinaryPrimitives.WriteInt32BigEndian(destination, value.Length);
        value.CopyTo(destination[LengthPrefixFieldLength..]);
        return LengthPrefixFieldLength + value.Length;
    }
}
