# Installation

__NOTE__ assumes you know how to run dotnet applications from the command line.  Someday, there will be better info.

There is some better documentation on [DeepWiki](https://deepwiki.com/tatmanblue/Arcus/1.2-getting-started:-installation-and-usage)   

## Separate downloads

None at this time

## Creating the service
1. download the code
2. build the entire solution
3. (suggestion) copy the service build to another directory
4. run it
5. TBF -- how to setup the service via commandline

## Requirements

- .NET 10 SDK to build (all projects target `net10.0`); the .NET 10 runtime to run.
- Run the tests with `dotnet test src/Arcus.sln` (xUnit projects: `windowssvc.tests`, `cli.tests`).

## Configuration

All configuration is via environment variables. Nothing needs to be set to run
Arcus locally with default settings -- every variable below is optional, and unset
means "off" or "use the default," never a required setup step.

### Service (`ArcusWinSvc`)

| Variable | Default | Description |
|---|---|---|
| `ARCUS_STORE_LOC` | OS local app data folder | Where vault files and the index are stored. |
| `ARCUS_GPRC_PORT` | `5001` | Port the gRPC endpoint listens on. Must be `> 5001` and `<= 65535` to take effect -- an out-of-range or invalid value falls back to the default. (Note the name: `GPRC`, not `GRPC` -- a pre-existing typo kept as-is since it's the actual variable the code reads.) |
| `ARCUS_GRPC_MSG_SIZE` | `10240` (10KB) | Maximum gRPC message size in bytes. Must be `> 1024` to take effect. |
| `ARCUS_ENCRYPTION_ALGORITHM` | `none` | Encryption at rest. `none` (no encryption) or `aes-256-gcm`. Per-file: changing this later never breaks files already written under a different setting. |
| `ARCUS_ENCRYPTION_KEY_FILE` | *(none)* | Path to the key file, required only when `ARCUS_ENCRYPTION_ALGORITHM` is set to a real algorithm. Keep this file outside `ARCUS_STORE_LOC` so it isn't swept up in backups of the vault itself. |
| `ARCUS_TLS_CERT_PATH` | *(none)* | Path to a `.pfx` certificate to enable TLS on the gRPC endpoint. Unset keeps the default cleartext HTTP/2 behavior. |
| `ARCUS_TLS_CERT_PASSWORD` | *(none)* | Password for `ARCUS_TLS_CERT_PATH`, if the certificate needs one. |
| `ARCUS_API_KEYS` | *(none)* | Comma-separated list of API keys clients must present (one key per client, so any single one can be revoked later). Unset disables authentication entirely -- any client that can reach the port is allowed. |

### CLI (`ArcusCli`)

| Variable | Default | Description |
|---|---|---|
| `ARCUS_SERVICE_URL` | `http://localhost:5001` | The service endpoint to connect to. Use `https://` when the service has TLS enabled. |
| `ARCUS_SERVICE_TLS_THUMBPRINT` | *(none)* | Pins the server certificate to this thumbprint -- needed when connecting over `https://` to a self-signed certificate, which otherwise fails default certificate validation. |
| `ARCUS_API_KEY` | *(none)* | The single API key this client presents. Must match one of the service's configured `ARCUS_API_KEYS`. |

## Security notes

Every security control above is **off by default** so a local install works with no setup. For anything beyond a
single trusted machine, be aware of the following (details in
[PHASE_A_SECURITY_PLAN.md](PHASE_A_SECURITY_PLAN.md), section 8):

- **Use TLS whenever you use API keys.** The key is sent as ordinary request metadata, so over plain `http://` it
  is readable on the network. Set `ARCUS_TLS_CERT_PATH` on the service and use an `https://` `ARCUS_SERVICE_URL`.
- **The service listens on all network interfaces**, not only loopback. With TLS and API keys unset, anyone who can
  reach the port can use the vault.
- **Double-check the value of `ARCUS_ENCRYPTION_ALGORITHM`.** An unrecognized value currently falls back to `none`
  without warning, so a typo means files are stored unencrypted.
- **The encryption key must be exactly 32 raw bytes** (AES-256) in the file named by `ARCUS_ENCRYPTION_KEY_FILE`. A
  missing or wrong-sized key is only reported the first time a file is added or read, not at startup. Back the key
  file up separately from the vault: losing or replacing it makes previously encrypted files unreadable, and there is
  no key rotation yet.
- **The index file is not encrypted.** File names, origin paths, keywords and checksums are stored in plaintext even
  when file contents are encrypted.
- **A download that fails its integrity check** is reported as an error by the CLI, but the corrupt file may remain
  at the destination path. Delete it and do not use it.

---
_Version: 2026.09.19_

