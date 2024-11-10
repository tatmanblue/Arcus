# Version 1

Version 1 serves as POC the project is interesting enough to continue.

1. cmdline interface only  
2. cmdline communicates via GRPC to windows service  
3. windows service does all of the work, including file copying (no streaming)  
4. minimal setup instructions via markdown, no installers
5. maybe some github actions for tests

## Tech
* dotnet core for core behaviors  
* rust for intensive stuff  

## V1 Functions
|  Name |  Description |  
| ---- | ---- |  
| add | adds a file to the vault, minimally encrypted |  
| remove | removes a file from the vault, no confirmation, no backup |  
| get | retrieves a file from the vault |  
| update | updates existing file in the vault |  
| list | shows that is available in the vault |  

## V1 Nice to have Functions
|  Name |  Description |  
| ---- | ---- |  
| erase | simply erases a local file over writing file with 0s before deleting it |  
| config | sending a json file to configure service--what TBD |  

## Tech
[Grpc](https://learn.microsoft.com/en-us/aspnet/core/grpc/client?view=aspnetcore-8.0)  
[Grpic Wrapper](https://github.com/protobuf-net/protobuf-net.Grpc)  

Project structure is defined on as AS NEEDED basis.  This means it may not always follow best practices.  Get over it.
