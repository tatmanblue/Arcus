# Version 2

## Ideas/Features
1. Stream file between CLI and service instead of service copying file directly ___Done___
2. Support for cloud.  S3 storage, that kind of thing  
3. Service adds checks for integrity  
4. Service encodes or encrypts files
5. Error handling on both the service and cli
6. Review PRs and add technical updates like error handling
7. Add configuration management 
8. Dynamic injection for URL downloader and type conversion functions


## Support for the Cloud.

Possibly integrate with [Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/).  Google Cloud vs AWS S2 [comparision](https://cloudmounter.net/amazon-s3-vs-google-cloud-storage/).  

## Checks and integrity  

Part of the meta data kept at the service could include checksums, hashes and other information about a file which can be used to validate the file data itself has not been compromised.   The value of these checks are diminshed if the service is compromised.

## Encodes and encrypts  

Currently files are saved in raw format.  The original intent for XVault was to secure the data from data breaches by encrypting files.  

Little bit of a brain dump on how this might work:  The service itself could through its own autonomy encrypt data.  A breach at the service compromises the entire data store. 
 Adding an onioned layer approach with the CLI providing an already encrypted file protects data at the service level--this adds complexity for sharing files between different CLI and a breach at the client compromises the system.   There may need to be some security between the CLI and the service through service validation of the client prior to allowing api calls, but this adds complexity to setup.

## Configuration  

Some configuration elements are obvious.  Some are specific to CLI or service and others could be shared.  The CLI could provide and update some configuration elements of the service, possibly, or vice versa.

## Dynamic injection 

On the service side, different urls may need different handlers and conversion options will also have different implementations.  Make this injectable or factory


# Underlying Technical
[SharpGrabber](https://github.com/dotnettools/SharpGrabber)  
[LibVideo](https://github.com/omansak/libvideo)  
