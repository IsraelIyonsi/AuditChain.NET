namespace AuditChain.Net.Tests.TestSupport;

/// <summary>
/// A deterministic <see cref="IAuditClock"/> that returns a starting instant on the first
/// call and advances by a fixed step on every call after that. Used so tests can assert
/// exactly which timestamp <see cref="AuditLog"/> stamped onto each record, proving it
/// reads the injected clock and never a hidden one.
/// </summary>
internal sealed class SequentialAuditClock : IAuditClock
{
    private readonly TimeSpan _step;
    private DateTimeOffset _next;

    public SequentialAuditClock(DateTimeOffset start, TimeSpan step)
    {
        _next = start;
        _step = step;
    }

    public DateTimeOffset UtcNow
    {
        get
        {
            DateTimeOffset current = _next;
            _next += _step;
            return current;
        }
    }
}
