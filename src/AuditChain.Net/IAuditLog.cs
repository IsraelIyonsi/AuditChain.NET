namespace AuditChain;

/// <summary>
/// An append-only, hash-chained audit log.
/// </summary>
public interface IAuditLog
{
    /// <summary>
    /// Seals a new record and appends it to the chain.
    /// </summary>
    /// <param name="payload">The payload bytes to record.</param>
    /// <returns>The newly sealed <see cref="AuditRecord"/>.</returns>
    AuditRecord Append(byte[] payload);

    /// <summary>
    /// Seals a new record from a UTF-8 encoded string and appends it to the chain.
    /// </summary>
    /// <param name="payload">The payload text to record.</param>
    /// <returns>The newly sealed <see cref="AuditRecord"/>.</returns>
    AuditRecord Append(string payload);

    /// <summary>
    /// Gets every record currently in the chain, in append order.
    /// </summary>
    /// <returns>A read-only snapshot of all records, oldest first.</returns>
    IReadOnlyList<AuditRecord> GetAll();
}
