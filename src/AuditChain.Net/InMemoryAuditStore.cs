namespace AuditChain;

/// <summary>
/// A thread-safe <see cref="IAuditStore"/> backed by an in-process list.
/// </summary>
/// <remarks>
/// Records are held only in memory: they do not survive process restart. Use this store
/// for tests, short-lived processes, or as a building block behind a durable
/// <see cref="IAuditStore"/> implementation.
/// </remarks>
public sealed class InMemoryAuditStore : IAuditStore
{
    private readonly List<AuditRecord> _records = new();
    private readonly object _gate = new();

    /// <inheritdoc />
    public void Append(AuditRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (_gate)
        {
            _records.Add(record);
        }
    }

    /// <inheritdoc />
    public AuditRecord? GetLast()
    {
        lock (_gate)
        {
            return _records.Count == 0 ? null : _records[^1];
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<AuditRecord> GetAll()
    {
        lock (_gate)
        {
            return _records.ToArray();
        }
    }
}
