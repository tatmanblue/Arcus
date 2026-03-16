# Arcus - Claude Code Guide

## Project Overview

Arcus is a secure vault for files built on .NET 8.0. It uses a client-server architecture over gRPC for storing and retrieving files locally or remotely. This is a resurrection of XVault (a ~20-year-old project) and serves as a research POC for secure file storage.

## Instructions for Claude

All changes must be approved before creating these changes.  Please prepare a plan of proposed changes and get confirmation before proceeding.


## Project Structure

```
src/
├── Arcus.sln
├── cli/           # ArcusCli — command-line client
├── grpc/          # Shared Protocol Buffer definitions
├── windowssvc/    # ArcusWinSvc — Windows service (gRPC server)
└── mobile/        # Android app (Kotlin/Compose)
docs/              # Project documentation and version plans
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

- **.NET 8.0** — CLI client and Windows service
- **gRPC** — Client-server communication (streaming for file transfers)
- **Protocol Buffers** — `src/grpc/ActionsService.proto` defines the service contract
- **Kotlin/Compose** — Android mobile app


## Architecture

- **ArcusCli** connects to **ArcusWinSvc** over gRPC
- File transfers use streaming (8KB chunks)
- Service maintains a local file index (`LocalIndexFileManager`) and storage backend (`LocalDataAccess`)
- A work queue (`WorkQueue`) handles async operations
- URL handlers (YouTube, generic) live under `windowssvc/Integrations/`

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

- `src/cli/appsettings.json` — CLI logging/connection config
- `src/windowssvc/appsettings.json` — Service config (storage paths, ports)
- `src/windowssvc/Properties/launchSettings.json` — Dev launch profiles

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
- **V2** (in progress) — Cloud storage, encryption, error handling, URL/YouTube downloads
- **V3** (started) — Android mobile app
- **V4** (planned) — IronBar integration

See `docs/` for detailed version plans.
