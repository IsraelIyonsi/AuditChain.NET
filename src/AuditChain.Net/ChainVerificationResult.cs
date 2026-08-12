namespace AuditChain;

/// <summary>
/// The outcome of <see cref="AuditChainVerifier.VerifyChain"/>.
/// </summary>
public sealed class ChainVerificationResult
{
    private ChainVerificationResult(bool isValid, int? brokenRecordIndex, ChainBreakReason? reason)
    {
        IsValid = isValid;
        BrokenRecordIndex = brokenRecordIndex;
        Reason = reason;
    }

    /// <summary>
    /// Gets a value indicating whether the chain passed verification.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the zero-based index, within the verified sequence, of the first record at
    /// which the chain broke, or <see langword="null"/> when <see cref="IsValid"/> is
    /// <see langword="true"/>.
    /// </summary>
    public int? BrokenRecordIndex { get; }

    /// <summary>
    /// Gets why the chain broke at <see cref="BrokenRecordIndex"/>, or
    /// <see langword="null"/> when <see cref="IsValid"/> is <see langword="true"/>.
    /// </summary>
    public ChainBreakReason? Reason { get; }

    /// <summary>
    /// Creates a result reporting a valid chain.
    /// </summary>
    /// <returns>A <see cref="ChainVerificationResult"/> with <see cref="IsValid"/> set to
    /// <see langword="true"/>.</returns>
    public static ChainVerificationResult Success() => new(isValid: true, brokenRecordIndex: null, reason: null);

    /// <summary>
    /// Creates a result reporting the first break found in a chain.
    /// </summary>
    /// <param name="brokenRecordIndex">The zero-based index of the first record at which
    /// the chain broke.</param>
    /// <param name="reason">Why the chain broke at that index.</param>
    /// <returns>A <see cref="ChainVerificationResult"/> with <see cref="IsValid"/> set to
    /// <see langword="false"/>.</returns>
    public static ChainVerificationResult Failure(int brokenRecordIndex, ChainBreakReason reason) =>
        new(isValid: false, brokenRecordIndex, reason);
}
