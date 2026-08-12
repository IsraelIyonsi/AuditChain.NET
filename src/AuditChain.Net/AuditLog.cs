using System.Text;

namespace AuditChain;

/// <summary>
/// The default <see cref="IAuditLog"/> implementation: seals each appended payload into a
/// hash-chained <see cref="AuditRecord"/> and persists it through an <see cref="IAuditStore"/>.
/// </summary>
/// <remarks>
/// <see cref="AuditLog"/> never reads the wall clock itself. Every timestamp comes from the
/// <see cref="IAuditClock"/> supplied at construction, so a caller can inject
/// <see cref="SystemAuditClock"/> in production and a fixed or scripted clock in tests.
/// </remarks>
public sealed class AuditLog : IAuditLog
{
    private readonly IAuditStore _store;
    private readonly IAuditClock _clock;
    private readonly object _appendGate = new();

    /// <summary>
    /// Initializes a new audit log backed by the given store and clock.
    /// </summary>
    /// <param name="store">The store used to persist and retrieve sealed records.</param>
    /// <param name="clock">The time source used to stamp newly appended records.</param>
    public AuditLog(IAuditStore store, IAuditClock clock)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(clock);

        _store = store;
        _clock = clock;
    }

    /// <inheritdoc />
    public AuditRecord Append(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        lock (_appendGate)
        {
            AuditRecord? previous = _store.GetLast();

            long sequenceNumber = previous is null
                ? AuditChainConstants.GenesisSequenceNumber
                : previous.SequenceNumber + 1;

            byte[] previousHash = previous is null
                ? AuditChainConstants.GenesisPreviousHash
                : previous.Hash.ToArray();

            DateTimeOffset timestamp = _clock.UtcNow;
            byte[] hash = AuditHasher.ComputeHash(sequenceNumber, timestamp, payload, previousHash);

            var record = new AuditRecord(sequenceNumber, timestamp, payload, previousHash, hash);
            _store.Append(record);
            return record;
        }
    }

    /// <inheritdoc />
    public AuditRecord Append(string payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return Append(Encoding.UTF8.GetBytes(payload));
    }

    /// <inheritdoc />
    public IReadOnlyList<AuditRecord> GetAll() => _store.GetAll();
}
