# Arcus - Claude Code Guide

## Project Overview

Arcus is a secure vault for files built on .NET 10.0. It uses a client-server architecture over gRPC for storing and retrieving files locally or remotely. This is a resurrection of XVault (a ~20-year-old project) and serves as a research POC for secure file storage.

## Instructions for Claude

All changes must be approved before creating these changes.  Please prepare a plan of proposed changes and get confirmation before proceeding.


## Project Structure

```
src/
├── Arcus.sln
├── cli/           # ArcusCli — command-line client
├── cli.tests/     # xUnit tests for the CLI
├── grpc/          # Shared Protocol Buffer definitions
├── windowssvc/    # ArcusWinSvc — Windows service (gRPC server)
├── windowssvc.tests/ # xUnit tests for the service (parallelization disabled — tests set ARCUS_* env vars)
└── mobile/        # Android app (Kotlin/Compose)
docs/              # ROADMAP.md (direction and phasing), PHASE_A_SECURITY_PLAN.md, INSTALL_USE.md
.github/workflows/ # CI/CD pipelines
```

## Build & Run

### .NET Projects

```bash
dotnet restore src/Arcus.sln
dotnet build src/Arcus.sln
dotnet test src/Arcus.sln
```

### Mobile (Android)

```bash
cd src/mobile
./gradlew build      # Linux/macOS
gradlew.bat build    # Windows
```

## Key Tech

- **.NET 10.0** — CLI client and Windows service
- **gRPC** — Client-server communication (streaming for file transfers)
- **Protocol Buffers** — `src/grpc/ActionsService.proto` defines the service contract
- **Kotlin/Compose** — Android mobile app


## Architecture

- **ArcusCli** connects to **ArcusWinSvc** over gRPC
- File transfers use streaming (8KB chunks)
- Service maintains a local file index (`LocalIndexFileManager`) and storage backend (`LocalDataAccess`)
- A work queue (`WorkQueue`) handles async operations
- URL handlers (YouTube, generic) live under `windowssvc/Integrations/`
- Security (Phase A, all opt-in and off by default) lives under `windowssvc/Security/`: `IStreamCipher` implementations in `Ciphers/` (`none`, `aes-256-gcm`), `FileKeyProvider` (`IKeyProvider`), and `ApiKeyAuthInterceptor`; the CLI has the matching `ApiKeyClientInterceptor` and `PinnedThumbprintValidator`
- Each `IndexFileRecord` carries a plaintext SHA-256 `Checksum` (verified on `Get`) and a `CipherVersion` — reads always use the cipher recorded on the record, never the currently configured one

## gRPC Service Methods

Defined in `src/grpc/ActionsService.proto`:

| Method   | Pattern            | Description                          |
|----------|--------------------|--------------------------------------|
| `List`   | Unary              | List files in the vault              |
| `Add`    | Client streaming   | Upload a file                        |
| `Get`    | Server streaming   | Download a file                      |
| `Remove` | Unary              | Delete a file                        |
| `Url`    | Unary              | Download from a URL (YouTube/generic)|

## Configuration

- `src/cli/appsettings.json` — CLI logging config
- `src/windowssvc/appsettings.json` — Service logging/host config
- `src/windowssvc/Properties/launchSettings.json` — Dev launch profiles
- Service and CLI behavior (store path, port, encryption, TLS, API keys, service URL) is configured through `ARCUS_*` environment variables, not appsettings — see `docs/INSTALL_USE.md` for the full list. Note the port variable is spelled `ARCUS_GPRC_PORT` in the code (pre-existing typo)

## CI/CD

- **dotnet.yml** — Runs on Ubuntu; triggers on push/PR to `main`; runs restore, build, test
- **branch-PR.yml** — Runs on Windows; triggers on PR events; validates Windows compatibility

## Development Conventions

- Work in feature branches; merge via PR
- Keep PRs small and focused
- Discuss significant changes before starting work
- See `CONTRIBUTING.md` for full guidelines

## Versioning Roadmap

- **V1** (complete) — Core vault operations: add, remove, get, list
- **V2** (in progress) — Integrity checks, optional encryption at rest, optional TLS, and API-key auth are done (Phase A); cloud storage and error handling remain; URL/YouTube downloads work
- **V3** (started) — Android mobile app (UI scaffold only, no gRPC client yet)
- **V4** (planned) — IronBar integration

`docs/ROADMAP.md` is the source of truth for phasing (it replaced the older per-version plan docs); `docs/PHASE_A_SECURITY_PLAN.md` §8 lists known follow-up issues in the shipped security work.

---
_Version: 2026.09.19_
