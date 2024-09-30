# Version 1

1. cmdline interface only  
2. cmdline communicates via GRPC to windows service  
3. windows service does all of the work  

## Tech
* dotnet core for core behaviors  
* rust for intensive stuff  

## Functions
|  Name |  Description |  
| ---- | ---- |  
| add | adds a file to the vault |  
| remove | removes a file from the vault |  
| retrieve | retrieves a file from the vault |  
| update | updates existing file in the vault |  
| erase | simply erases the file over writing file with 0s before deleting it |  
| config | sending a json file to configure service--what TBD |  


