# AuditChain.NET

A tamper-evident, hash-chained append-only audit log for .NET. Every entry is SHA-256 chained to the one before it, so if anyone edits a record, deletes one, reorders one, or forges a hash after the fact, verifying the chain tells you exactly where it broke. Zero external dependencies.

Regulators are starting to ask for this directly. The EU AI Act's Article 12 record-keeping requirement expects logs that a provider cannot quietly rewrite after the fact, and the same expectation shows up informally in every SOC 2 or internal compliance audit: "can you prove nobody touched this." A database row with an `UpdatedAt` column cannot prove that. A hash chain can. Most teams reach for a blockchain library or roll a bespoke solution for this, when what they actually need is the twenty-year-old idea behind git commits and Certificate Transparency logs: chain each record to the hash of the one before it. AuditChain.NET is that idea, packaged as a small, dependency-free .NET library instead of a byte from a much heavier tool.

## Install

```
dotnet add package AuditChain.Net
```

## Quickstart

```csharp
using AuditChain;

var log = new AuditLog(new InMemoryAuditStore(), new SystemAuditClock());

AuditRecord entry = log.Append("user 4471 approved loan application LN-2091");

Console.WriteLine(entry.SequenceNumber); // 0
Console.WriteLine(Convert.ToHexString(entry.Hash.Span));
```

Each call to `Append` seals a new record: it stamps the sequence number, asks the injected clock for a timestamp, links it to the previous record's hash, and computes its own SHA-256 hash over all of that. Nothing about a sealed record can change afterward without the hash no longer matching.

## Verifying the chain

```csharp
using AuditChain;

IReadOnlyList<AuditRecord> records = log.GetAll();
ChainVerificationResult result = AuditChainVerifier.VerifyChain(records);

if (!result.IsValid)
{
    Console.WriteLine($"chain broke at record {result.BrokenRecordIndex}: {result.Reason}");
}
```

`VerifyChain` walks the records in order and stops at the first one that does not correctly chain to its predecessor. `BrokenRecordIndex` tells you which record, and `Reason` tells you what kind of break it was: a sequence number out of order (a deleted or reordered record), a previous-hash pointer that does not match (a forged link), or a record whose own hash no longer matches its contents (a mutated payload or a forged hash).

## Injecting time instead of hiding a clock

`AuditLog` never calls `DateTime.UtcNow` internally. It asks the `IAuditClock` you give it, every time. In production that is `SystemAuditClock`, but the point is that it is an explicit dependency, not a hidden one, so your tests can hand it a fixed or scripted clock and assert on exact timestamps instead of "some time around now":

```csharp
using AuditChain;

public sealed class FixedAuditClock : IAuditClock
{
    public DateTimeOffset UtcNow { get; set; }
}

var clock = new FixedAuditClock { UtcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) };
var log = new AuditLog(new InMemoryAuditStore(), clock);

AuditRecord record = log.Append("scheduled maintenance started");
Assert.Equal(clock.UtcNow, record.Timestamp);
```

## API surface

| Type | Purpose |
|---|---|
| `AuditLog` | Appends payloads and seals them into a hash-chained `AuditRecord`. |
| `AuditRecord` | An immutable, sealed chain entry: sequence number, timestamp, payload, previous hash, own hash. |
| `AuditChainVerifier.VerifyChain(records)` | Walks a sequence of records and reports the first break, if any. |
| `ChainVerificationResult` | `IsValid`, plus `BrokenRecordIndex` and `Reason` when it is not. |
| `ChainBreakReason` | `SequenceNumberMismatch`, `PreviousHashMismatch`, or `RecordHashMismatch`. |
| `IAuditStore` / `InMemoryAuditStore` | Pluggable persistence. The in-memory store ships with the package; a database or file-backed store is your own `IAuditStore` implementation. |
| `IAuditClock` / `SystemAuditClock` | The injected time source described above. |
| `AuditHasher.ComputeHash(...)` | The canonical hash function itself, exposed so you can verify a record independently or build a store that recomputes hashes on read. |

## What "hash-chained" actually buys you

The genesis record chains to a fixed all-zero previous hash. Every record after that hashes its own sequence number, timestamp, payload, and the previous record's hash together with SHA-256. That last part is the whole trick: because each hash depends on the one before it, you cannot edit a record in the middle of the chain without every hash after it becoming invalid too. An attacker with write access to your database can delete rows or edit values, but they cannot make the chain verify again without recomputing every hash from that point forward, and if they do not have the log's private continuation (which does not exist here, by design, because this is a log, not a signature scheme), the tamper is visible the moment someone calls `VerifyChain`.

To be precise about what this does and does not guarantee: AuditChain.NET detects tampering after the fact, deterministically and offline. It does not stop someone with database write access from tampering, and it does not itself prove *when* the tamper happened relative to when the record was verified. If you need that stronger guarantee, anchor the chain's latest hash somewhere the log owner cannot rewrite: a separate append-only store, a periodic external timestamp, or a signed checkpoint published elsewhere. AuditChain.NET gives you the chain; where you anchor it is your call.

## Hashing is deterministic, not culture-dependent

The hash input is a fixed, length-prefixed byte layout: a domain tag, the sequence number and timestamp as big-endian integers, then the length-prefixed payload and previous hash. There is no `ToString()` of a number or a date anywhere in the hash path, so the same record hashes identically on any machine, in any culture, on any OS. A `DateTimeOffset` representing the same instant with a different UTC offset also hashes identically, because timestamps are normalized to UTC ticks before hashing.

## Zero dependencies, AOT-friendly

The package has no runtime NuGet dependencies: it is built entirely on `System.Security.Cryptography` and `System.Buffers.Binary` from the BCL. There is no reflection, no dynamic code generation, and no reliance on JIT-only features, so it trims and publishes cleanly under Native AOT.

## Scope of v0.1

This release ships `InMemoryAuditStore` only. A durable backend (Entity Framework, a flat file, object storage) is a matter of implementing `IAuditStore` against your own storage; the interface is deliberately small so that is not a large task. If you build one you are happy with, a PR is welcome.

## License

MIT. See [LICENSE](LICENSE).
