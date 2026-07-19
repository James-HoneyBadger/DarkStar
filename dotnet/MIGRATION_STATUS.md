# DarkStar .NET Migration Status

This document tracks the ground-up C#/.NET rebuild status relative to the legacy stack.

## Completed

1. Layered architecture (Domain, Application, Infrastructure, API, CLI, Tests).
2. Text crypto use cases:
   - Encrypt
   - Decrypt
   - Sign
   - Verify
3. File crypto use cases:
   - Encrypt file to `.dstar` payload
   - Decrypt file back to plaintext
4. Key management use cases:
   - Create
   - List
   - Delete
5. Contact management use cases:
   - Create
   - List
   - Delete
6. Workspace summary:
   - Key count
   - Contact count
   - Audit count
7. Persistence:
   - File-backed key store (`keys.json`)
   - File-backed contact store (`contacts.json`)
   - File-backed audit log (`audit.jsonl`)
8. API surface:
   - Health
   - Crypto (text + file)
   - Keys
   - Contacts
   - Workspace summary
9. CLI surface:
   - Crypto (text + file)
   - Keys
   - Contacts
   - Workspace summary
10. Tests:
   - Domain/service tests
   - API integration tests (in-process host)

## In Progress / Next

1. Cryptographic parity hardening:
   - Dedicated ChaCha20-Poly1305 implementation
   - Additional key material handling models
2. Advanced security features:
   - Tamper-evident chained audit records
   - Backup/restore archive formats
3. Interoperability:
   - Migration import tools for legacy stores
   - Compatibility vectors against current production outputs
4. Frontend parity:
   - Desktop shell migration (Avalonia recommended)
   - Web static UI migration to .NET host

## Validation Snapshot

- `dotnet build -c Debug`: PASS
- `dotnet test -c Debug`: PASS
  - Domain tests: PASS
  - API integration tests: PASS
