# Version 1

Version 1 serves as POC the project is interesting enough to continue.

1. cmdline interface only  
2. cmdline communicates via GRPC to windows service  
3. windows service does all of the work  
4. minimal instructions via markdown, no installers
5. maybe some github actions for tests

## Tech
* dotnet core for core behaviors  
* rust for intensive stuff  

## Functions
|  Name |  Description |  
| ---- | ---- |  
| add | adds a file to the vault, minimally encrypted |  
| remove | removes a file from the vault, no confirmation, no backup |  
| retrieve | retrieves a file from the vault |  
| update | updates existing file in the vault |  
| erase | simply erases the file over writing file with 0s before deleting it |  
| config | sending a json file to configure service--what TBD |  


