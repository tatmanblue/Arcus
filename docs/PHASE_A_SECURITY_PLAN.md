# Phase A Technical Plan — Security & Integrity Hardening

This is the technical plan for Phase A of `ROADMAP.md`. It assumes the decisions already resolved there:
service-side encryption at rest (Model 1) now, designed so client-side encryption (Model 2) can be layered in
later; TLS for transport, currently explicitly disabled; new client/service authentication; new integrity
checksums. This document gets into how, grounded in the actual classes those changes touch — it is still a
technical plan, not task tickets or final code.

## 0. Where things stand today (the concrete starting point)

| Concern | Current state | File(s) |
|---|---|---|
| Encryption at rest | None. Files are written/read as raw bytes. | `LocalDataAccessStream.WriteBytes`/`ReadBytes` |
| Encryption in transit | Explicitly disabled — Kestrel is configured `SslProtocols.None` | `windowssvc/Program.cs` |
| Client/service auth | None — any process reaching the gRPC port can call any RPC | `ActionsServiceImpl`, `AbstractBaseRunner` |
| Integrity | None — no checksum field exists at all | `IndexFileRecord` |
| CLI endpoint | Hardcoded to `http://localhost:5001` — not configurable | `AbstractBaseRunner` constructor |

That last row matters beyond Phase A: a hardcoded `localhost` channel only works when the CLI and service share
a machine, which also blocks Phase B (cross-platform service). Making the endpoint configurable is shared
groundwork between the two phases, not Phase A–only work.

Worth noting: `IFileOperations`'s own doc comment already describes it as covering behaviors like "encrypting"
a file — the seam for this work was anticipated in the code, just never implemented.

## 1. Encryption at Rest (Optional, Model 1, swappable for Model 2 later)

Encryption at rest is **off by default and configurable** — running Arcus unencrypted for low-stakes local use
must stay a legitimate, supported choice, not just a transient pre-launch state. Rather than a separate on/off
flag plus a cipher choice, this is modeled as a single choice of algorithm: `none` (a pass-through no-op) is a
first-class `IStreamCipher` implementation, alongside real ciphers, selected via configuration.

**New interface**, alongside the existing `Interfaces/` folder: an `IStreamCipher` (or similar) with something
like `Stream WrapForWrite(Stream underlying)` / `Stream WrapForRead(Stream underlying)`. Concrete implementations:
`NoneStreamCipher` (default) and `Aes256GcmStreamCipher` (first real option).

**Algorithm selection is configurable, not hardcoded:** introduce e.g. `ARCUS_ENCRYPTION_ALGORITHM`, following the
`ARCUS_*` env var convention already in `Configuration.cs`, read by a small factory that resolves the configured
name to an `IStreamCipher` and registers it in DI. This is the same "dynamic injection/factory" pattern
`V2_Plan.md` already called for on the URL/conversion-handler side — applied here instead of invented fresh.
Default is `none`; a deployment opts in to `aes-256-gcm` (or a later algorithm) explicitly. Adding a second real
algorithm later (e.g. ChaCha20-Poly1305) means one more `IStreamCipher` implementation and one more factory case
— no change to call sites in `LocalDataAccessStream`.

**Hook point:** `LocalDataAccessStream.WriteBytes`/`ReadBytes` currently construct a plain `FileStream` directly.
Route that `FileStream` through the configured cipher's wrapping methods instead (a no-op passthrough when `none`
is selected). `IFileAccess`/`IFileAccessStream` stay exactly as they are today ("write bytes," "read bytes") —
encryption becomes transparent at the point bytes touch disk. This is *why* Model 2 stays viable later: a
client-side cipher would just be a different `IStreamCipher` living on the client instead of the service, without
`ActionsServiceImpl` or the gRPC contract changing at all.

**Key management:** only relevant when a real cipher is selected. The service needs to hold a key somewhere other
than the plaintext index file. Recommend a new `IKeyProvider` interface, separate from `IStreamCipher`, so "where
the key lives" and "how encryption is done" can vary independently — and, following the same config-driven
factory pattern as the cipher algorithm in the section above, made pluggable via e.g. `ARCUS_KEY_PROVIDER` rather
than fixed to one mechanism:

- **`file` (default):** a `FileKeyProvider` that reads key bytes from a path given by `ARCUS_ENCRYPTION_KEY_FILE`.
  Simple, works identically on every OS (no per-platform code), and easy for a user to understand, back up, and
  rotate manually. Guidance (not an enforced check, though a startup warning is cheap to add): this path should
  live outside the vault's own `StoreLocation` so the key isn't swept up in whatever backs up or copies the data
  directory itself.
- **`os-native` (opt-in, stronger):** DPAPI or Windows Credential Manager on today's host; libsecret/Keychain
  once Phase B adds Linux/macOS hosts. This protects the key even if the data directory or a backup of it is
  copied off the machine entirely — the key isn't extractable without OS-level access tied to that specific user
  or machine, which a plain key file can't guarantee.

Start with `file` as the default so encryption stays easy to turn on; `os-native` exists for anyone who wants the
stronger guarantee and is willing to accept the extra platform-specific setup. Making this its own interface is
also what keeps Model 2 open later — a client-held key is just a different `IKeyProvider` implementation, not a
redesign.

**Per-record cipher state is load-bearing, not just a migration artifact.** Because the setting is optional and
can change at any time — off, on, or switched to a different algorithm — each `IndexFileRecord` must record which
cipher (if any) actually produced it (see §4's `CipherVersion` field). A file is always read back using the
cipher recorded on *its own* record, never whatever is currently configured, so toggling or changing the setting
can never break previously stored files.

**No forced migration follows from this:** since `none` is a legitimate steady-state rather than a pre-launch
condition, existing plaintext records simply carry `CipherVersion = none` going forward. Only newly-added files
— or files explicitly re-encrypted by a deliberate, separate operation — pick up whatever algorithm is currently
configured.

## 2. Encryption in Transit (TLS)

- `windowssvc/Program.cs` needs its Kestrel config changed from the current explicit `SslProtocols.None` to a
  real certificate. Self-signed is fine for local/single-machine use; anything crossing a real network needs a
  CA-issued cert or a pinned-thumbprint trust model on the client, since these are small/personal deployments,
  not public CA-friendly servers by default.
- `AbstractBaseRunner`'s hardcoded `GrpcChannel.ForAddress("http://localhost:5001")` needs to become
  configurable (host, port, scheme) and switch to `https://` once TLS is live. This is the same "make the
  endpoint configurable" work flagged in the table above — do it once, use it for both TLS and Phase B.

## 3. Client/Service Authentication

Currently `ActionsServiceImpl` has no notion of caller identity at all — `List`/`Add`/`Get`/`Remove`/`Url` are
open to anything that can reach the port.

Recommend a server-side gRPC `Interceptor` checking a pre-shared key sent as call metadata, and design it around
a *list* of valid keys from day one (not a single shared secret) — this mirrors IronBar's own MCP server, which
already does exactly this (`IRONBAR_MCP_ACCESS_TOKENS`, a comma-separated token list, one token per client
instance, revocable independently). Reusing an already-adopted pattern from this same project family beats
inventing a new one.

- Client side: `AbstractBaseRunner` needs to attach the key as gRPC call credentials/metadata — the same
  constructor already being touched for endpoint configurability in §2.
- Provisioning: key(s) read from config/environment variable, consistent with the existing `ARCUS_*` env var
  convention in `Configuration.cs`.
- Per-client key revocation (vs. one shared key everyone uses) is a reasonable fast-follow rather than a Phase A
  blocker — but since the interceptor is built around a key *list* from the start, adding revocation later is a
  config change, not a redesign.

## 4. Integrity Verification (Checksums)

- Add to `IndexFileRecord` (currently just `Id`/`ShortName`/`OriginFullPath`/`Timestamp`/`Keywords`/`Status`,
  with no integrity or cipher metadata at all): a `Checksum` (SHA-256 hex) and an explicit cipher-state field
  (e.g. `IsEncrypted` / `CipherVersion`) so mixed old/new records can coexist per the migration question in §1.
- Compute the hash while streaming bytes in on `Add`, in the same pass as the cipher wrap from §1 — avoid a
  second read of the file just to hash it.
- Verify on `Get` by recomputing while streaming out and comparing to the stored value. Decide at implementation
  time whether a mismatch hard-fails the download stream or just logs a warning; default recommendation is
  hard-fail, since silently returning content that doesn't match its recorded checksum is worse than an error.
- `LocalIndexFileManager` persists all records as one JSON blob via `JsonConvert.SerializeObject`/
  `DeserializeObject` — adding fields to `IndexFileRecord` deserializes safely against old data (missing fields
  take their type default), so this specific change needs no separate migration step, unlike §1's store contents.

## 5. Suggested Build Order

The four areas above touch overlapping files, so build them in this order rather than in parallel:

1. **Integrity checksums (§4)** first. Purely additive (new fields, no behavior change to existing reads/writes)
   and gives a correctness check to lean on while building the riskier pieces after it.
2. **Encryption at rest (§1)**. The biggest structural change (new interfaces, key storage) — having checksums
   already in place makes it straightforward to confirm encrypt/decrypt round-trips don't corrupt data.
3. **TLS + CLI endpoint configurability (§2)**. Comparatively mechanical (standard Kestrel/`GrpcChannel` config).
4. **Client/service authentication (§3)** last — it reuses the same client-construction work item from step 3
   (attaching a key is one more thing to configure on the same path).

## 6. Explicit Non-Goals for Phase A

- **Per-user/multi-tenant authorization.** This phase is service-wide client authentication (is this caller
  allowed to talk to the service at all), not per-user ACLs on individual files.
- **Client-side (Model 2) encryption itself.** Only the `IStreamCipher`/`IKeyProvider` seam is being laid so it
  *can* be added later — implementing it is out of scope here.
- **Cross-platform hosting (Phase B).** This phase's TLS/key-storage choices should avoid Windows-only
  assumptions baked into core logic (keep cert/key loading behind the same kind of interface used elsewhere), but
  actually building non-Windows hosts is Phase B's job, not this one's.

## 7. Known Gaps to Address Later

Unlike §6, these weren't decided up front -- they're gaps noticed while implementing steps 1-3, deliberately left
alone rather than fixed in passing, since they're bigger than the step that surfaced them.

- **The CLI has no centralized configuration, unlike the server.** `windowssvc` centralizes all its `ARCUS_*`
  settings behind `IConfiguration`/`Configuration`, injected via DI. The CLI has no equivalent:
  `AbstractBaseRunner` reads `ARCUS_SERVICE_URL`/`ARCUS_SERVICE_TLS_THUMBPRINT` directly via
  `Environment.GetEnvironmentVariable`, with the env var names as `private const` fields hidden inside that one
  class. (`CliConfiguration.cs` is *not* this -- despite the similar name, it's argument-parsing/runner-dispatch
  logic, not a settings class.) A follow-up should add an analogous `ICliSettings`/`CliSettings` exposing
  `ServiceUrl`, `TlsThumbprint`, and step 4's upcoming API key as properties, passed into `AbstractBaseRunner<T>`'s
  constructor instead of read inline. Note the CLI doesn't fully resolve runners through DI today --
  `CliConfiguration.GetRunner` constructs each one manually (e.g.
  `new ListRunner(serviceProvider.GetService<ILogger<ListRunner>>())`) -- so the natural fit is one more
  manually-passed constructor argument at each call site, not a DI registration change.
