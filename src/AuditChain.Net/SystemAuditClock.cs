namespace AuditChain;

/// <summary>
/// An <see cref="IAuditClock"/> backed by the operating system's real-time clock.
/// </summary>
/// <remarks>
/// This is the obvious production choice, but it is never wired in implicitly: every
/// <see cref="AuditLog"/> requires an explicit <see cref="IAuditClock"/>, and using
/// <see cref="SystemAuditClock"/> is an explicit opt-in by the caller rather than a hidden
/// default.
/// </remarks>
public sealed class SystemAuditClock : IAuditClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
