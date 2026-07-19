# DarkStar .NET Rebuild

This directory is a full ground-up C#/.NET architecture baseline for DarkStar.

## Solution layout

- `src/DarkStar.Domain`: Core entities and value objects.
- `src/DarkStar.Application`: Use cases, orchestration services, and abstractions.
- `src/DarkStar.Infrastructure`: File storage, crypto engine, and dependency injection wiring.
- `src/DarkStar.Api`: ASP.NET Core Web API host exposing application services.
- `src/DarkStar.Cli`: Console host for scripted operations.
- `tests/DarkStar.Domain.Tests`: Unit tests for domain behavior.

## Quick start

```bash
cd dotnet
export PATH="$PWD/../.dotnet:$PATH"
dotnet restore
```

Run API:

```bash
cd dotnet/src/DarkStar.Api
export DARKSTAR_HOME="$PWD/.darkstar-home"
dotnet run
```

Run CLI:

```bash
cd dotnet/src/DarkStar.Cli
export DARKSTAR_HOME="$PWD/.darkstar-home"
dotnet run -- encrypt-text --text "hello" --passphrase "secret"
```

Run tests:

```bash
cd dotnet
dotnet test
```

## Migration status

This is the first migration slice. It establishes project boundaries, endpoint/CLI shape, DI setup, and file-backed repositories. Additional slices should add:

1. Full decrypt/sign/verify flows.
2. Key/contact lifecycle management parity.
3. Hardware-backed key providers.
4. Tamper-evident audit chain and backup archives.
5. Desktop shell (Avalonia) over the same application layer.

## Current capabilities

1. Text crypto: encrypt/decrypt/sign/verify.
2. File crypto: encrypt/decrypt using authenticated payload format.
3. Key CRUD: create/list/delete.
4. Contact CRUD: create/list/delete.
5. Workspace summary counts for keys, contacts, and audit records.
6. API integration tests via in-process ASP.NET host.

See [MIGRATION_STATUS.md](MIGRATION_STATUS.md) for detailed status and next steps.
