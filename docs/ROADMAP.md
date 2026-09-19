# Arcus Roadmap

This document is the single source of truth for where Arcus is headed. It replaces the earlier `arch.md`,
`V1_Plan.md`, `V2_Plan.md`, `Mobile_Plan.md`, and `watch_list.md`, which were removed once their still-relevant
content was folded in here (see §7); they remain available in the git history.

This is a roadmap, not an implementation plan: it identifies use cases, the gaps standing between Arcus and
those use cases, open decisions that need to be made, and the dependency order projects should be tackled in.
It does not prescribe API shapes, class designs, or task-level breakdowns.

## 1. Vision & Use Cases

Arcus's purpose is secure, automated transfer and synchronization of files between clients, with a
server/service component and clients across Windows, Linux, macOS, Android, and (lower priority) iOS.

Use cases identified for this roadmap:

| # | Use case | Source |
|---|----------|--------|
| 1 | **Purchased-file auto-sync** — a user downloads an MP3 on their PC; the Arcus client detects the download and uploads it; a mobile Arcus app detects the new file and pulls it into the phone's music folder. | Given |
| 2 | **Agent-to-agent file sharing** — an agentic coding assistant writes a file to its local Briefcase instance; the Briefcase pushes it to the Arcus server; other Briefcase instances on other machines detect it and surface it to their own agents. | Given |
| 3 | **Multi-device personal vault** — a user's files (documents, configs, notes) stay in sync across every device they own without manual copying, functioning as the spiritual successor to XVault. | Derived from vision statement |
| 4 | **Secure sharing between people** — one user deliberately shares a file or folder with another Arcus user/instance, with encryption in transit and at rest so a compromised transport or intermediary can't read the content. | Derived from the original V2 security notes |
| 5 | **Tamper-evident transfer history** — an auditable, cryptographically verifiable record of what files moved where and when, useful for compliance-sensitive or high-trust scenarios. | Enabled by optional IronBar integration (metadata/integrity ledger) |
| 6 | **URL-sourced content ingestion** — pulling in and normalizing content from external URLs (YouTube today, generic web content planned) directly into the vault. | Existing `Url` RPC |

Use cases 1–3 are the core value proposition and should drive near-term priority. Use cases 4–6 depend on
foundational work landing first (see Phases, below).

## 2. Current State Snapshot

| Project | Status | Notes |
|---|---|---|
| **Arcus core (cli + windowssvc)** | V1 complete, V2 partial | `List`/`Add`/`Get`/`Remove`/`Url` all implemented over gRPC with streaming (8KB chunks). Config management and CLI logging replacement are done. **Phase A landed (PRs #15–#18):** SHA-256 integrity checksums, optional AES-256-GCM encryption at rest, optional TLS with a configurable CLI endpoint, and API-key client authentication — all opt-in and off by default. All projects now target .NET 10, with xUnit test projects for both the service and CLI. Cloud storage, dynamic URL/conversion handlers, and structured error handling are not yet implemented. See `PHASE_A_SECURITY_PLAN.md` §8 for follow-up issues found in the shipped Phase A code. |
| **Mobile (Android)** | V3 started | Gradle/Compose project scaffolded with `Splash`, `ListFiles`, `Settings`, and `Standard` screens. No gRPC client wiring found — the app does not yet talk to the service. Once it does, it must support the Phase A controls (API key header, TLS, pinned self-signed certificate). |
| **Security** | Phase A implemented, opt-in | `windowssvc/Security/` now holds the AES-256-GCM cipher (`Ciphers/`), `FileKeyProvider`, and `ApiKeyAuthInterceptor` alongside the local file erase helpers. Nothing is enabled by default: files are still stored raw, and the gRPC endpoint is cleartext and unauthenticated, until `ARCUS_ENCRYPTION_ALGORITHM`, `ARCUS_TLS_CERT_PATH`, and `ARCUS_API_KEYS` are set (see `INSTALL_USE.md`). Authentication is a shared-key list, not mutual TLS. Known gaps: no key rotation/key ID, no OS-native key storage, plaintext index — see `PHASE_A_SECURITY_PLAN.md` §8. |
| **Integrations** | Single-purpose | Only `YouTube.cs` exists under `windowssvc/Integrations/`; no generic URL handler, no factory/plugin pattern despite having been planned since V2. |
| **the-briefcase** | Mature (v2.1), but Arcus-unaware | Full MCP file server: list/read/create/update/search/archive, project grouping, file-watcher-driven change notifications, local web UI. Its own roadmap already lists cloud storage backends and a plugin architecture as open items. It has no concept of Arcus today — the sync-between-instances use case (#2) is net-new work on both sides. |
| **IronBar** | Prototype | Distributed ledger (BFT, boot/child nodes), pluggable via `IPlugin`, has its own MCP server exposing ledger operations to agents. Its `WORK.md` explicitly lists **"Integrate with Arcus"** as a possible extension — this is a mutually-acknowledged integration, not one-sided. |

## 3. Gap Analysis

Gaps are grouped by theme rather than by project, since most of them block more than one use case.

**Security**
- ~~No encryption at rest.~~ **Addressed (Phase A, PR #16):** service-side (Model 1) AES-256-GCM behind the swappable `IStreamCipher`/`IKeyProvider` interfaces, so client-side encryption (Model 2) can still be added later — see Open Decision #2. Opt-in; off by default. Remaining: key rotation/key ID, OS-native key storage, an encrypted index.
- ~~No encryption in transit.~~ **Addressed (Phase A, PR #17):** optional TLS via `ARCUS_TLS_CERT_PATH`, with a configurable CLI endpoint and pinned-thumbprint support for self-signed certificates. Opt-in; the default is still cleartext HTTP/2. (The earlier claim that `SslProtocols.None` "explicitly disables" TLS was a misreading — it means "OS chooses"; TLS was off only because no certificate was configured.)
- ~~No authentication between CLI/mobile clients and the service.~~ **Addressed for the CLI (Phase A, PR #18):** shared-key list via `ARCUS_API_KEYS` / `ARCUS_API_KEY`. Opt-in; the API key is only protected in transit when TLS is also on. Still open: the mobile client (no gRPC client yet) and per-user authorization (an explicit Phase A non-goal).
- ~~No integrity verification (checksums/hashes) on stored files.~~ **Addressed (Phase A, PR #15):** SHA-256 recorded on `Add`/`Url` and verified on `Get`. Remaining: the checksum is stored in the plaintext, unprotected index (see the IronBar ledger, Phase E), and a failed check currently leaves the corrupt file on the CLI side.

**Platform reach**
- The service's core logic is currently built directly on top of the Windows Service hosting model (`ArcusWinSvc`), with no separation between "what the service does" and "how it's hosted." Per the resolved decision above, Windows keeps its native Windows Service host, but Linux/macOS need their own idiomatic host — which requires extracting the core logic first. No Linux/macOS host exists yet.
- The mobile app cannot yet perform any vault operation — it's UI scaffolding only.

**Automation**
- Nothing detects new files automatically today. Use case #1 requires a client-side watcher (something monitoring a downloads folder) and a server-side or push-based notification path to other clients — neither exists. The Briefcase already has file-watching + MCP notification infrastructure that could serve as a reference implementation.
- No config push/sync between CLI and service, despite being raised as a V2 idea.

**Extensibility**
- URL ingestion is hardcoded to a single YouTube handler. The original V2 plan called for a dynamic/injectable handler and conversion-type factory; none exists.
- No plugin model on the Arcus side, unlike IronBar (`IPlugin`) and the Briefcase's planned plugin architecture.

**Code health** (carried forward from the original V2 plan; not tied to a use case, but they make everything else safer to build)
- No structured error handling on either side. The service returns results or throws inconsistently, and the CLI has no consistent way to surface failures — for example, a download that fails its integrity check leaves the corrupt file at the destination.
- `ActionsServiceImpl` mixes gRPC request/response translation with the vault logic itself. Moving the logic into separate types the gRPC handlers call would decouple it from the transport, and is a natural first step of the host-extraction work in Phase B.

**Cross-project integration**
- No code or design connects Arcus to the Briefcase or to IronBar yet. Both integrations are currently only mentioned in planning docs (this one, and IronBar's `WORK.md`).
- The Briefcase has no notion of a remote Arcus backend — its file operations are 100% local-filesystem today.

## 4. Open Decisions

All four decisions below have been resolved; kept here (rather than folded away) since the reasoning behind
each still matters for scoping the phases that depend on them. Decision #2's implementation (Phase A) has since
shipped — see the Phase A entry in §5.

1. ~~Windows-only vs. cross-platform service~~ — **Resolved:** Windows keeps its native Windows Service host
   (`ArcusWinSvc`); other OSes are not forced into that model. Linux and macOS should each host the service the
   way that's idiomatic there (e.g. a systemd unit, a launchd daemon, or simply a long-running process/container),
   rather than building one lowest-common-denominator host for every platform. This implies the core service
   logic (file operations, indexing, gRPC handling) needs to be decoupled from the Windows Service host wrapper
   it's currently built directly on top of, so each platform's host is a thin shell around the same logic.
2. ~~Encryption ownership~~ — **Resolved:** Service-side encryption at rest (Model 1) for now — the service
   encrypts files it stores; clients send plaintext to the service. Designed so client-side/onion-layered
   encryption (Model 2) can be layered in later without a rearchitecture: at-rest encryption should sit behind a
   swappable interface (consistent with the existing `Interfaces/` pattern in `windowssvc`) so "who holds the key"
   can change later without touching storage/transfer plumbing. Encryption *in transit* is a separate, lower-risk
   concern handled at the transport layer via TLS on the gRPC channel — not a payload-level decision — but note
   this isn't automatic: it needs a certificate configured on the service. (This decision originally described the
   `SslProtocols.None` setting in `windowssvc/Program.cs` as TLS being "explicitly disabled"; that was a
   misreading — the setting means "let the OS pick the protocol", and TLS was simply unconfigured.) Enabling it was
   ordinary work for Phase A and is now done, opt-in via `ARCUS_TLS_CERT_PATH`.
3. ~~IronBar's role~~ — **Resolved:** IronBar is optional and additive, never required for core Arcus
   functionality. Its role is a metadata/integrity ledger — a tamper-evident, BFT-backed copy of file metadata
   (checksums, version history, transfer records) that gives a guarantee Arcus's own local metadata can't: multiple
   independent nodes attesting a record hasn't been altered after the fact. Arcus's own integrity checks (Phase A)
   and encryption remain fully self-contained and IronBar-independent — a widely-wanted use case (#4, secure
   sharing) must not depend on an optional project that's currently prototype-stage with irregular updates. If
   IronBar later audits anything encryption-related, the narrower and safer target is key-rotation/access-grant
   history, not gating whether encryption is usable at all. Whether IronBar also becomes a storage backend in its
   own right is explicitly deferred — a separate decision to revisit only after the metadata-ledger integration
   proves out, rather than taking on both integration shapes at once.
4. ~~Briefcase relationship~~ — **Resolved:** Arcus becomes a storage backend for the Briefcase, using the same
   storage-backend abstraction point the Briefcase's own roadmap already reserves for its planned cloud backends
   (S3, OneDrive, Google Drive, etc.) — Arcus is simply another backend behind that same seam, not a special
   case. The Briefcase remains the sole agent-facing surface (its existing `list_files`/`read_file`/`create_file`/
   `search_files`/notifications/projects tools); agents continue talking only to the Briefcase and never to Arcus
   directly. This was chosen over giving Arcus its own MCP server (the pattern IronBar uses for its ledger)
   specifically because it matches the use case as originally scoped — Briefcase-to-Briefcase sync via Arcus —
   rather than introducing a second, overlapping agent-facing tool surface.

## 5. Roadmap Phases

Phases are ordered by dependency, not calendar time. Later phases assume earlier ones are functionally
complete, since they build on top of the trust and automation model established earlier.

### Phase A — Security & Integrity Hardening — **Implemented (2026/09/19)**
**Status:** all four steps are merged to `main` — integrity checksums (#15), encryption at rest (#16), TLS +
configurable CLI endpoint (#17), and API-key client authentication (#18), plus a config-bug fix and env-var
documentation (#19). Every control is opt-in and off by default, so an unconfigured install behaves as before.
Before this phase is treated as *closed*, the follow-ups in `PHASE_A_SECURITY_PLAN.md` §8 should be triaged — most
importantly: reject an unrecognized `ARCUS_ENCRYPTION_ALGORITHM` instead of silently falling back to plaintext,
fail fast on key problems at startup, decide on a key ID/rotation story, and warn when API keys are configured
without TLS. Not built from the original plan: the `os-native` key provider and `ARCUS_KEY_PROVIDER` selector.

Original scope, per Open Decision #2 (resolved): add service-side encryption at rest, behind a swappable interface so client-side
encryption can be layered in later without touching storage/transfer code; enable TLS on the gRPC transport
(ordinary configuration work, not an open design question). Also add client/service authentication and integrity checks (checksums/hashes)
on stored files. Blocks: Phase D and E, both of which involve sharing data across trust boundaries (other
machines, other agents, a public ledger) and shouldn't be built on an unauthenticated, unencrypted foundation.
Because the shipped controls are opt-in, D and E should not assume they are switched on in any given deployment.

### Phase B — Cross-Platform Service & Real Mobile Client
Extract the service's core logic from the Windows Service host it's currently built on, so it can be re-hosted:
keep the native Windows Service host as-is, and add an idiomatic host per other OS (systemd unit or equivalent
long-running process on Linux, launchd daemon on macOS). Also wire the Android app's existing screens to actual
gRPC calls (upload/download/list/delete). Depends on: Open Decision #1 (resolved). Blocks: use case #1 (phone-side
sync has no cross-platform value if the service can't run near where the user actually is) and use case #3.

### Phase C — Automation (Watchers & Push)
Add client-side folder watching (detect new downloads) and a push/notification path from service to clients
so new files propagate without polling. The Briefcase's `FileWatcher`/`NotificationDispatcher` pattern is a
proven reference design for this. Depends on: Phase A (don't push files over an unauthenticated channel) and
Phase B (needs working clients on both ends). Enables: use case #1 fully, and is a prerequisite building block
for #2.

### Phase D — Briefcase Integration
Per Open Decision #4 (resolved), this phase has two parts: (1) the Briefcase needs a storage-backend abstraction
built — it doesn't have one today, it's local-filesystem-only — the same seam its own roadmap earmarks for cloud
backends, so this benefits the Briefcase independent of Arcus; (2) implement Arcus as one such backend, giving
the sync path described in use case #2: a Briefcase instance's file operations flow through to an Arcus server,
and other Briefcase instances (each still the sole interface their own agents use) detect and surface the change.
Depends on: Phase C (this is fundamentally the same watch-and-notify problem, applied across a Briefcase↔Arcus
boundary instead of Arcus client↔client).

### Phase E — IronBar Integration (Optional)
Per Open Decision #3 (resolved), implement IronBar strictly as an optional, additive metadata/integrity ledger:
if configured, Arcus writes checksums, version history, and transfer records to IronBar's ledger (use case #5);
if not configured, Arcus behaves exactly as it does without IronBar — nothing about core Arcus functionality,
including its own encryption and integrity checks from Phase A, depends on IronBar being present. The integration
should be a single narrow seam (e.g. an interface Arcus calls into, no-op when IronBar isn't configured) so
IronBar's concepts don't leak into core storage/transfer logic. IronBar-as-storage-backend is explicitly out of
scope for this phase — revisit only after the ledger integration has proven out, rather than taking on both
integration shapes at once. Depends on: Phase A (a meaningful audit trail requires the data being audited to
already be authenticated and integrity-checked at the source).

### Phase F — Cloud Storage Backends
Add S3/Azure Blob (or similar) as a storage option for the vault, as originally scoped for V2. Depends
on: Phase A (don't hand raw, unencrypted files to a third-party cloud provider) and benefits from — but doesn't
strictly require — Phase D, since a shared cloud backend would also simplify the Briefcase's own planned cloud
storage support.

## 6. Appendix — Related Technology Already Identified

These were raised in existing docs and remain relevant candidates; carried forward rather than re-litigated:

- **gRPC** + [protobuf-net.Grpc](https://github.com/protobuf-net/protobuf-net.Grpc) — existing transport, continues as-is.
- **Cloud storage**: S3 vs. Azure Blob ([comparison](https://cloudmounter.net/amazon-s3-vs-google-cloud-storage/)); [Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) as a possible orchestration layer, notably also used by IronBar for multi-node local dev.
- **URL ingestion**: [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) (already in use), [ffmpeg](https://www.ffmpeg.org/download.html) for conversions.
- **Reference for sync UX**: [LocalSend](https://github.com/localsend/localsend) — comparable cross-platform file-transfer tool, worth studying for the "detect and push" interaction model in Phase C.
- **Minio** — mentioned in IronBar's own storage roadmap as an abstraction over S3/Azure/GCS; worth evaluating jointly if Phase F and IronBar's storage work end up sharing infrastructure.


## 7. Carried Forward from Earlier Docs

`arch.md`, `V1_Plan.md`, `V2_Plan.md`, `Mobile_Plan.md`, and `watch_list.md` were removed once this roadmap
superseded them (they remain in the git history). What was still worth keeping:

- **Project principle:** Arcus stays open source and is not a commercial product.
- **V1 (complete):** a proof of concept — a command-line client talking gRPC to a Windows service, with `add`,
  `remove`, `get`, and `list`. Files were stored raw with no security, which Phase A has since started to address.
  V1 "nice to have" ideas that never shipped: an `update` command, a local `erase` command, and a `config` command
  for sending settings to the service.
- **Open V2 items now tracked above:** cloud storage (Phase F), dynamic URL/conversion handlers (Extensibility gap),
  error handling and separating gRPC handlers from vault logic (Code health gap). Encryption, integrity checks, and
  client validation from the same list were delivered by Phase A.
- **Dropped:** the storage-mechanism brainstorm in `arch.md` (Windows-only vs. OS-independent vs. hosted vs. cloud vs.
  blockchain-backed) is superseded by Open Decision #1 and Phases B and F. Its "Rust or C++ for cryptography" and
  Unity-UI ideas were not pursued; encryption uses .NET's built-in `AesGcm`.

---
_Document version: 2026/09/19_
