# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-08-12

### Added

- `AuditLog`, the default `IAuditLog` implementation: appends a payload and seals it into a hash-chained `AuditRecord`, with the sequence number and previous-hash link maintained automatically.
- `AuditRecord`, an immutable sealed chain entry carrying its sequence number, timestamp, payload, previous hash, and own SHA-256 hash.
- `AuditChainVerifier.VerifyChain`, which walks a sequence of records and reports either success or the first break: index and `ChainBreakReason` (`SequenceNumberMismatch`, `PreviousHashMismatch`, or `RecordHashMismatch`).
- `AuditHasher.ComputeHash`, the canonical, deterministic, length-prefixed hash function used for every record. No culture-dependent string formatting is involved anywhere in the hash path.
- `IAuditStore` and the shipped `InMemoryAuditStore` implementation, a thread-safe in-process store.
- `IAuditClock` and `SystemAuditClock`. `AuditLog` never reads the system clock directly; every timestamp is supplied by an injected clock.
- The genesis record chains to a fixed all-zero previous hash (`AuditChainConstants.GenesisPreviousHash`).

### Notes

- v0.1 ships an in-memory store only. Entity Framework and file-backed stores are out of scope for this release; implement `IAuditStore` against your own storage.
- Target framework: `net8.0`. Zero runtime NuGet dependencies.
