# Mobile

## Idea
App on mobile device is able to send and recieve files from a Arcus Service using the same
patterns and interfaces as the CLI.

Since Arcus Service already supports gRPC, the preference is the mobile app can use gRPC to communicate with the service.

There are a couple of other options as documented in [watch_list.md](watch_list.md).

## Features
- [ ] File upload
- [ ] File download
- [ ] File delete, locally and from service
- [ ] Automatic push or notification of new files
- [ ] Service has ability to watch for new files and add, notify.  Probably use some type of plugin model

