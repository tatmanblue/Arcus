# Version 2

## Ideas/Features
1. Support for cloud.  S3 storage, that kind of thing  
2. Service adds checks for integrity  
3. Service encodes or encrypts files
4. Error handling on both the service and cli
5. Review PRs and add technical updates like error handling
6. Dynamic injection for URL downloader and type conversion functions
7. Move service GRPC handlers into separate types

### Completed From list above
1. Replace Default logger in CLI  ___Done___
2. Stream file between CLI and service instead of service copying file directly ___Done___
3. Add configuration management __Done__
4. Service adds checks for integrity (item 2 above) ___Done___ — SHA-256 checksums, Phase A step 1 (PR #15)
5. Service encodes or encrypts files (item 3 above) ___Done___ — optional AES-256-GCM at rest, Phase A step 2 (PR #16)
6. Validate the client before allowing API calls (see "Encodes and encrypts" below) ___Done___ — API-key auth, Phase A step 4 (PR #18)
7. TLS on the gRPC transport and a configurable CLI endpoint (not on the original list) ___Done___ — Phase A step 3 (PR #17)

Items 4–7 are tracked in detail in [PHASE_A_SECURITY_PLAN.md](PHASE_A_SECURITY_PLAN.md), which also lists follow-up
issues found in the shipped code (section 8). All four security controls are opt-in and off by default.

### Still open
- Support for cloud (S3 storage, etc.)
- Error handling on both the service and CLI — note the CLI currently leaves a corrupt file behind when a download
  fails its integrity check (Phase A plan, section 8)
- Review PRs and add technical updates like error handling
- Dynamic injection for URL downloader and type conversion functions
- Move service GRPC handlers into separate types

Current direction and phasing live in [ROADMAP.md](ROADMAP.md), which supersedes this list.

## Support for the Cloud.

Possibly integrate with [Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/).  Google Cloud vs AWS S2 [comparision](https://cloudmounter.net/amazon-s3-vs-google-cloud-storage/).  

## Checks and integrity  

Part of the meta data kept at the service could include checksums, hashes and other information about a file which can be used to validate the file data itself has not been compromised.   The value of these checks are diminshed if the service is compromised.

__Update 2026/09/19:__ implemented as a SHA-256 checksum on each index record, verified on download. As anticipated above, it does not protect against an attacker who can modify both a file and the index; the optional IronBar ledger (ROADMAP Phase E) is the intended answer to that.

## Encodes and encrypts  

Currently files are saved in raw format.  The original intent for XVault was to secure the data from data breaches by encrypting files.  

Little bit of a brain dump on how this might work:  The service itself could through its own autonomy encrypt data.  A breach at the service compromises the entire data store. 
 Adding an onioned layer approach with the CLI providing an already encrypted file protects data at the service level--this adds complexity for sharing files between different CLI and a breach at the client compromises the system.   There may need to be some security between the CLI and the service through service validation of the client prior to allowing api calls, but this adds complexity to setup.  

__Update 2026/09/19:__ the service-side approach was chosen (ROADMAP Open Decision #2) and implemented as optional AES-256-GCM encryption at rest, behind swappable cipher and key-provider interfaces so the client-side "onion" layer can be added later. Client validation is implemented as API keys sent with each call. Configuration is by environment variables, listed in [INSTALL_USE.md](INSTALL_USE.md).

## Configuration  

Some configuration elements are obvious.  Some are specific to CLI or service and others could be shared.  The CLI could provide and update some configuration elements of the service, possibly, or vice versa.  

## Dynamic injection  

On the service side, different urls may need different handlers and conversion options will also have different implementations.  Make this injectable or factory  

## Service GRPC handlers  

The service GRPC implementation should consume the logic, translating the request/response types into data needed by the logic so that the logic is separate from the GRPC handling.  


# Underlying Technical
[YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode)  
[ffmpeg](https://www.ffmpeg.org/download.html)  

---
_Document version: 2026/09/19_
