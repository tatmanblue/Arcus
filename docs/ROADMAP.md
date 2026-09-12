# Arcus Roadmap

This document supersedes `arch.md`, `V1_Plan.md`, `V2_Plan.md`, `Mobile_Plan.md`, and `watch_list.md` as the
source of truth for where Arcus is headed. Those files remain for historical context but should not be
treated as current planning.

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
| 4 | **Secure sharing between people** — one user deliberately shares a file or folder with another Arcus user/instance, with encryption in transit and at rest so a compromised transport or intermediary can't read the content. | Derived from V2_Plan security notes |
| 5 | **Tamper-evident transfer history** — an auditable, cryptographically verifiable record of what files moved where and when, useful for compliance-sensitive or high-trust scenarios. | Enabled by optional IronBar integration (metadata/integrity ledger) |
| 6 | **URL-sourced content ingestion** — pulling in and normalizing content from external URLs (YouTube today, generic web content planned) directly into the vault. | Existing `Url` RPC + V2_Plan |

Use cases 1–3 are the core value proposition and should drive near-term priority. Use cases 4–6 depend on
foundational work landing first (see Phases, below).

## 2. Current State Snapshot

| Project | Status | Notes |
|---|---|---|
| **Arcus core (cli + windowssvc)** | V1 complete, V2 partial | `List`/`Add`/`Get`/`Remove`/`Url` all implemented over gRPC with streaming (8KB chunks). Config management and CLI logging replacement are done. Encryption, integrity checks, cloud storage, dynamic URL/conversion handlers, and structured error handling are still only described in `V2_Plan.md` — no code exists for any of them yet. |
| **Mobile (Android)** | V3 started | Gradle/Compose project scaffolded with `Splash`, `ListFiles`, `Settings`, and `Standard` screens. No gRPC client wiring found — the app does not yet talk to the service. |
| **Security** | Minimal | `windowssvc/Security/` only contains local file erase/operations helpers, not encryption. Files are stored raw in the vault, matching V1's documented "no security" status. There is no client/service mutual authentication. |
| **Integrations** | Single-purpose | Only `YouTube.cs` exists under `windowssvc/Integrations/`; no generic URL handler, no factory/plugin pattern despite being called out in V2_Plan. |
| **the-briefcase** | Mature (v2.1), but Arcus-unaware | Full MCP file server: list/read/create/update/search/archive, project grouping, file-watcher-driven change notifications, local web UI. Its own roadmap already lists cloud storage backends and a plugin architecture as open items. It has no concept of Arcus today — the sync-between-instances use case (#2) is net-new work on both sides. |
| **IronBar** | Prototype | Distributed ledger (BFT, boot/child nodes), pluggable via `IPlugin`, has its own MCP server exposing ledger operations to agents. Its `WORK.md` explicitly lists **"Integrate with Arcus"** as a possible extension — this is a mutually-acknowledged integration, not one-sided. |

## 3. Gap Analysis

Gaps are grouped by theme rather than by project, since most of them block more than one use case.

**Security**
- No encryption at rest or in transit. V2_Plan documents two candidate models (service-side vs. client-side/onion-layered encryption) but no decision has been made.
- No authentication or authorization between CLI/mobile clients and the service — any client that can reach the gRPC endpoint can act on the vault.
- No integrity verification (checksums/hashes) on stored files.

**Platform reach**
- The service's core logic is currently built directly on top of the Windows Service hosting model (`ArcusWinSvc`), with no separation between "what the service does" and "how it's hosted." Per the resolved decision above, Windows keeps its native Windows Service host, but Linux/macOS need their own idiomatic host — which requires extracting the core logic first. No Linux/macOS host exists yet.
- The mobile app cannot yet perform any vault operation — it's UI scaffolding only.

**Automation**
- Nothing detects new files automatically today. Use case #1 requires a client-side watcher (something monitoring a downloads folder) and a server-side or push-based notification path to other clients — neither exists. The Briefcase already has file-watching + MCP notification infrastructure that could serve as a reference implementation.
- No config push/sync between CLI and service, despite being raised as a V2 idea.

**Extensibility**
- URL ingestion is hardcoded to a single YouTube handler. V2_Plan calls for a dynamic/injectable handler and conversion-type factory; none exists.
- No plugin model on the Arcus side, unlike IronBar (`IPlugin`) and the Briefcase's planned plugin architecture.

**Cross-project integration**
- No code or design connects Arcus to the Briefcase or to IronBar yet. Both integrations are currently only mentioned in planning docs (this one, and IronBar's `WORK.md`).
- The Briefcase has no notion of a remote Arcus backend — its file operations are 100% local-filesystem today.

## 4. Open Decisions

Decision #1 has been resolved (see below); the remainder still need an answer before the phases below can be
scoped in detail.

1. ~~Windows-only vs. cross-platform service~~ — **Resolved:** Windows keeps its native Windows Service host
   (`ArcusWinSvc`); other OSes are not forced into that model. Linux and macOS should each host the service the
   way that's idiomatic there (e.g. a systemd unit, a launchd daemon, or simply a long-running process/container),
   rather than building one lowest-common-denominator host for every platform. This implies the core service
   logic (file operations, indexing, gRPC handling) needs to be decoupled from the Windows Service host wrapper
   it's currently built directly on top of, so each platform's host is a thin shell around the same logic.
2. **Encryption ownership** — client-side (onion-layered, protects against a compromised service but complicates sharing) vs. service-side (simpler, but a service breach exposes everything)? V2_Plan raises both without resolving it.
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

### Phase A — Security & Integrity Hardening
Encrypt data at rest and in transit; add client/service authentication; add integrity checks (checksums/hashes)
on stored files. Depends on: Open Decision #2. Blocks: Phase D and E, both of which involve sharing data across
trust boundaries (other machines, other agents, a public ledger) and shouldn't be built on an unauthenticated,
unencrypted foundation.

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
Add S3/Azure Blob (or similar) as a storage option for the vault, as originally scoped in `V2_Plan.md`. Depends
on: Phase A (don't hand raw, unencrypted files to a third-party cloud provider) and benefits from — but doesn't
strictly require — Phase D, since a shared cloud backend would also simplify the Briefcase's own planned cloud
storage support.

## 6. Appendix — Related Technology Already Identified

These were raised in existing docs and remain relevant candidates; carried forward rather than re-litigated:

- **gRPC** + [protobuf-net.Grpc](https://github.com/protobuf-net/protobuf-net.Grpc) — existing transport, continues as-is.
- **Cloud storage**: S3 vs. Azure Blob (see `V2_Plan.md`'s comparison link); [Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) as a possible orchestration layer, notably also used by IronBar for multi-node local dev.
- **URL ingestion**: [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) (already in use), [ffmpeg](https://www.ffmpeg.org/download.html) for conversions.
- **Reference for sync UX**: [LocalSend](https://github.com/localsend/localsend) — comparable cross-platform file-transfer tool, worth studying for the "detect and push" interaction model in Phase C.
- **Minio** — mentioned in IronBar's own storage roadmap as an abstraction over S3/Azure/GCS; worth evaluating jointly if Phase F and IronBar's storage work end up sharing infrastructure.
