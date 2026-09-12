# Installation

__NOTE__ assumes you know how to run dotnet applications from the command line.  Someday, there will be better info.

## Separate downloads

None at this time

## Creating the service
1. download the code
2. build the entire solution
3. (suggestion) copy the service build to another directory
4. run it
5. TBF -- how to setup the service via commandline

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

