# Phase A Technical Plan — Security & Integrity Hardening

This is the technical plan for Phase A of `ROADMAP.md`. It assumes the decisions already resolved there:
service-side encryption at rest (Model 1) now, designed so client-side encryption (Model 2) can be layered in
later; TLS for transport, currently explicitly disabled; new client/service authentication; new integrity
checksums. This document gets into how, grounded in the actual classes those changes touch — it is still a
technical plan, not task tickets or final code.

## Status (as of 2026/09/19)

**All four Phase A steps are implemented and merged to `main`** (PRs #15–#18), plus a config bug fix and env-var
documentation (#19). Every control is opt-in and off by default, so an unconfigured single-machine install behaves
exactly as it did before Phase A.

| Step | Area | Status | PR |
|---|---|---|---|
| 1 | Integrity checksums (§4) | Done | #15 |
| 2 | Encryption at rest (§1) | Done — `aes-256-gcm` + `file` key provider; `os-native` key provider **not** built | #16 |
| 3 | TLS + configurable CLI endpoint (§2) | Done | #17 |
| 4 | Client/service authentication (§3) | Done — key *list* supported; per-client revocation is a config change | #18 |
| — | Fix ignored `ARCUS_GPRC_PORT`/`ARCUS_GRPC_MSG_SIZE`; document all env vars | Done | #19 |

Also landed alongside: two xUnit test projects (`windowssvc.tests`, `cli.tests`), and the upgrade of every project
and both CI workflows from .NET 8 to .NET 10. New issues and gaps discovered while reviewing the shipped code are
collected in §8; the earlier-noted CLI configuration gap in §7 is still open.

Sections 1–7 below are kept as the original plan for context; each carries an **Implemented** note where what
shipped differs from, or narrows, what was planned.

## 0. Where things stood at the start of Phase A (the concrete starting point)

> Historical: this table describes the pre-Phase A codebase. See the Status section above for the current state.

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
the original V2 plan already called for on the URL/conversion-handler side — applied here instead of invented fresh.
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

**Implemented (PR #16):** `IStreamCipher`, `IStreamCipherFactory`, and `IKeyProvider` exist in
`windowssvc/Interfaces/`. `NoneStreamCipher` (default) and `Aes256GcmStreamCipher` are the two ciphers; because
`AesGcm` has no native streaming mode, data is chunked into 64KB blocks framed as
`[length][nonce][ciphertext][tag]` (`Aes256GcmEncryptingStream`/`Aes256GcmDecryptingStream`). `StreamCipherFactory`
picks the default for *new* writes from `ARCUS_ENCRYPTION_ALGORITHM` and can `Resolve` any known cipher by name, so
reads always use the cipher recorded on the record's `CipherVersion` (a test writes under `aes-256-gcm`, flips the
setting to `none`, and confirms the record still reads back). `FileKeyProvider` reads `ARCUS_ENCRYPTION_KEY_FILE`
lazily as a raw 32-byte key. The `Url` RPC's `LocalCopy` path is routed through the cipher too, not just streamed
`Add`.

**Not implemented from this section:** the `ARCUS_KEY_PROVIDER` selector and the `os-native` provider (DPAPI /
Credential Manager) — `Program.cs` registers `FileKeyProvider` directly; the startup warning about a key file living
inside `StoreLocation`; and any deliberate re-encrypt/migration operation for existing records.

## 2. Encryption in Transit (TLS)

- `windowssvc/Program.cs` needs its Kestrel config changed from the current explicit `SslProtocols.None` to a
  real certificate. Self-signed is fine for local/single-machine use; anything crossing a real network needs a
  CA-issued cert or a pinned-thumbprint trust model on the client, since these are small/personal deployments,
  not public CA-friendly servers by default.
- `AbstractBaseRunner`'s hardcoded `GrpcChannel.ForAddress("http://localhost:5001")` needs to become
  configurable (host, port, scheme) and switch to `https://` once TLS is live. This is the same "make the
  endpoint configurable" work flagged in the table above — do it once, use it for both TLS and Phase B.

**Implemented (PR #17):** TLS is opt-in via `ARCUS_TLS_CERT_PATH` (+ `ARCUS_TLS_CERT_PASSWORD`, a `.pfx`). Unset,
Kestrel keeps cleartext HTTP/2. The CLI reads its endpoint from `ARCUS_SERVICE_URL` (default
`http://localhost:5001`) and supports pinning a self-signed server certificate with `ARCUS_SERVICE_TLS_THUMBPRINT`
(`PinnedThumbprintValidator`). Verified manually against a real self-signed certificate: plain HTTP is unaffected,
HTTPS + pinned thumbprint connects, HTTPS without pinning is rejected (`UntrustedRoot`). The existing
`ConfigureHttpsDefaults(SslProtocols.None)` call was kept — `None` means "let the OS choose the most secure
protocol", not "disabled", and it only applies once a certificate is configured. So the plan's framing of
`SslProtocols.None` as "explicitly disabled" was a misreading: the endpoint was cleartext because no certificate was
configured, not because of that setting. (`ROADMAP.md` carries the same wording and is corrected there.)

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

**Implemented (PR #18):** `ApiKeyAuthInterceptor` (server, covers all four gRPC call shapes) checks the `x-api-key`
metadata entry against the comma-separated `ARCUS_API_KEYS` list and returns `Unauthenticated` otherwise; with no
keys configured, enforcement is off. `ApiKeyClientInterceptor` (CLI) attaches the single `ARCUS_API_KEY` to every
outgoing call. Verified manually against a running service: no key and a wrong key are both rejected, either of two
configured keys succeeds, and a server with no keys configured accepts an unauthenticated client as before.

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

**Implemented (PR #15):** `IndexFileRecord` gained `Checksum` (SHA-256, lowercase hex) and `CipherVersion` (default
`"none"`). `Add` and `Url` hash while writing; `Get` re-hashes while streaming out. Records with an empty `Checksum`
(pre-existing data) are treated as unverifiable rather than broken. On mismatch `Get` fails the RPC with
`StatusCode.DataLoss` — but see §8 item 8: the failure can only occur *after* the bytes have been streamed. The
checksum is taken over the **plaintext** (before the cipher on write, after it on read).

## 5. Suggested Build Order

The four areas above touch overlapping files, so build them in this order rather than in parallel:

1. **Integrity checksums (§4)** first. Purely additive (new fields, no behavior change to existing reads/writes)
   and gives a correctness check to lean on while building the riskier pieces after it.
2. **Encryption at rest (§1)**. The biggest structural change (new interfaces, key storage) — having checksums
   already in place makes it straightforward to confirm encrypt/decrypt round-trips don't corrupt data.
3. **TLS + CLI endpoint configurability (§2)**. Comparatively mechanical (standard Kestrel/`GrpcChannel` config).
4. **Client/service authentication (§3)** last — it reuses the same client-construction work item from step 3
   (attaching a key is one more thing to configure on the same path).

**Outcome:** built in exactly this order (PRs #15 → #18); no reordering was needed.

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
  `ServiceUrl`, `TlsThumbprint`, and step 4's API key as properties, passed into `AbstractBaseRunner<T>`'s
  constructor instead of read inline. Note the CLI doesn't fully resolve runners through DI today --
  `CliConfiguration.GetRunner` constructs each one manually (e.g.
  `new ListRunner(serviceProvider.GetService<ILogger<ListRunner>>())`) -- so the natural fit is one more
  manually-passed constructor argument at each call site, not a DI registration change.

  **Update (2026/09/19):** still open, and slightly larger now — step 4 shipped with `ARCUS_API_KEY` also read
  inline in `AbstractBaseRunner`, so it reads three env vars directly instead of two.

## 8. Issues and Gaps Raised by the Implementation

Found by reviewing the shipped code against this plan (2026/09/19). None are regressions from pre-Phase A behavior;
each is either a gap between what was planned and what shipped, or a weakness that only became visible once the
controls existed. Ordered roughly by security relevance. **None have been fixed yet** — they are candidates for
follow-up work, not commitments.

**Security-relevant**

1. **A misspelled `ARCUS_ENCRYPTION_ALGORITHM` silently falls back to `none`.** `StreamCipherFactory` resolves the
   configured name with `TryGetValue(...) ? cipher : none`, so a typo such as `aes-256-gsm` means new files are
   written in plaintext with no warning. An unrecognized value should fail startup, or at minimum log loudly.
2. **Key/config errors surface late, not at startup.** `FileKeyProvider` loads the key lazily, and the 32-byte length
   check happens in `Aes256GcmStreamCipher.GetValidatedKey` on first use. A missing, unreadable, or wrong-sized key
   file is only discovered on the first `Add`/`Get`, not when the service starts. Validate eagerly whenever a real
   cipher is configured.
3. **No key identity or rotation.** `CipherVersion` records the *algorithm* only. Replace or lose the key file and
   every previously encrypted file becomes unreadable (GCM authentication fails), with nothing on the record saying
   which key it needed. Rotation/re-encryption is also unbuilt (see §1). Decide on a key-id field before real data
   is encrypted at scale.
4. **The API key travels in cleartext unless TLS is also enabled.** `x-api-key` is ordinary call metadata; over
   `http://` (the default) anyone on the network path can read it. Authentication is only meaningful with TLS on for
   any non-loopback deployment. Consider refusing, or at least warning on, a configured `ARCUS_API_KEYS` with no
   certificate.
5. **The default posture is network-exposed.** Kestrel listens on `IPAddress.Any`, and with TLS and API keys both
   off by default (deliberately, for local use) an unconfigured service accepts unauthenticated cleartext calls on
   every network interface. A loopback-only default bind, with wider binding as an explicit opt-in, would preserve
   the local-use story.
6. **API key comparison is not constant-time, and secrets live in plain environment variables.** Validation is a
   `HashSet<string>.Contains`; API keys and the TLS certificate password sit in environment variables visible to
   administrators. Low risk for this project's current threat model, but worth resolving before wider deployment.
7. **The index is plaintext and now holds a plaintext hash.** `LocalIndexFileManager`'s JSON index stores file
   names, origin paths, keywords, and the SHA-256 of each file's *plaintext* unencrypted, even when file contents
   are encrypted — anyone who can read the index can confirm whether a known file is in the vault. The index is also
   not integrity-protected: an attacker with write access to the store can alter a file *and* its recorded checksum
   together (a limitation the original V2 plan already anticipated; IronBar, Phase E, is the intended answer).

**Correctness / usability**

8. **A checksum failure can't stop the bytes already sent, and the CLI leaves the bad file behind.** The hash isn't
   final until the last byte is read, so `Get` streams the whole file and only then throws `DataLoss`. The CLI's
   `FileTransferClient.DownloadFileAsync` has already written those bytes to the destination, and nothing removes
   the corrupt file when the RPC fails. The CLI should download to a temporary path and rename on success (or
   delete on failure), and report `DataLoss` as a clear message.
9. **Tests that touch `ARCUS_*` variables share process-global state.** They raced each other under xUnit's default
   parallelism; PR #19 disabled parallelization for the whole `windowssvc.tests` assembly as a blunt fix. A shared
   collection or an injectable environment reader would restore parallel runs.
10. **`Url` hashes the source file separately from the write path.** It computes the checksum from the downloaded
    file before `LocalCopy` rather than in the same pass as the cipher wrap (as §4 intended). Correct today, but it
    costs a second read of the file and is a second code path to keep in step with `Add`.

**Impact on later phases**

11. **Mobile (Phase B) must support the same controls.** The Android app has no gRPC client yet; when it gets one it
    will need to send `x-api-key`, connect over TLS, and support the pinned-certificate model for self-signed
    servers. Android has no equivalent of the CLI's `ARCUS_*` variables, so it needs a settings surface for these.
12. **No OS-native key storage exists on any platform.** Only the `file` key provider shipped. The `IKeyProvider`
    seam is ready for DPAPI/Credential Manager (Windows) and libsecret/Keychain (Phase B hosts), but none of those
    implementations are built.

---
_Version: 2026.09.19_
