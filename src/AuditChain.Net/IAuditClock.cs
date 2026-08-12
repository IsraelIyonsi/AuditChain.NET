namespace AuditChain;

/// <summary>
/// Supplies the timestamp stamped onto each newly appended <see cref="AuditRecord"/>.
/// </summary>
/// <remarks>
/// <see cref="AuditLog"/> never reads the system clock directly. It asks an injected
/// <see cref="IAuditClock"/> instead, so tests can supply fixed or scripted timestamps and
/// production code can route time through whatever source it already trusts.
/// </remarks>
public interface IAuditClock
{
    /// <summary>
    /// Gets the current instant to stamp onto the next appended record.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
