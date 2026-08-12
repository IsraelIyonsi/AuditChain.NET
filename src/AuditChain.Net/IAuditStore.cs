namespace AuditChain;

/// <summary>
/// Persists and retrieves the sealed records of an audit chain, in append order.
/// </summary>
/// <remarks>
/// An <see cref="IAuditStore"/> is a dumb, order-preserving ledger. It does not validate
/// the chain: it trusts the <see cref="AuditRecord"/> instances it is given and hands them
/// back unchanged. Chain construction lives in <see cref="AuditLog"/>; chain validation
/// lives in <see cref="AuditChainVerifier"/>. This package ships
/// <see cref="InMemoryAuditStore"/>; durable backends (a database, a file, an
/// append-only object store) implement this interface separately.
/// </remarks>
public interface IAuditStore
{
    /// <summary>
    /// Appends a sealed record to the end of the store.
    /// </summary>
    /// <param name="record">The record to persist.</param>
    void Append(AuditRecord record);

    /// <summary>
    /// Gets the most recently appended record, or <see langword="null"/> if the store is
    /// empty.
    /// </summary>
    /// <returns>The last record in append order, or <see langword="null"/>.</returns>
    AuditRecord? GetLast();

    /// <summary>
    /// Gets every record in the store, in append order.
    /// </summary>
    /// <returns>A read-only snapshot of all stored records, oldest first.</returns>
    IReadOnlyList<AuditRecord> GetAll();
}
