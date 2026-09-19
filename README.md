# Arcus
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/tatmanblue/Arcus)  

Arcus is a secure vault for files. Arcus is designed to run on any OS capable of running dotnet core applications. It supports networking multiple arcus clients with an arcus server.  While there are commercially available solutions, Arcus was designed as a research and POC and originated from a [resurrected idea of long ago](https://github.com/tatmanblue/Arcus?tab=readme-ov-file#history).

The Arcus solution comprises of applications and services to securely store files, accessed remotely and locally.



## Versions, ideas, plans

1. Version 1 served as a POC.  Calling this ***DONE***
2. Version 2: whats next?  Bit's and pieces have been completed now.  Integrity checksums, optional encryption at rest, optional TLS and API-key authentication are implemented (see [Phase A security plan](docs/PHASE_A_SECURITY_PLAN.md)); cloud storage and error handling are still open.
3. Version 3: add mobile device support
4. \[Version 4] Integration with [IronBar](https://github.com/tatmanblue/ironbar)
5. [Roadmap](docs/ROADMAP.md) is the source of truth for direction, phasing and open issues, and replaces the earlier per-version plan documents  



## Installing and using

There is not much documentation on this topic, yet.  Feel free to reach out to me or create a [github issue](https://github.com/tatmanblue/Arcus/issues) and I will work with you.  There is a start of a [install/use doc](https://github.com/tatmanblue/Arcus/blob/main/docs/INSTALL_USE.md), which now lists every `ARCUS_*` environment variable and some security notes, but is otherwise terse.  



## Issue tracking

All issues are tracked in YouTrack.   Will consider shifting to github issues, if there are public contributions.  

1. [YouTrack](https://tatmangames.youtrack.cloud/agiles/159-6/current)  



## History

This project is a reincaration of the idea or version two of [XVault](https://github.com/tatmanblue/xvault).  I never got very far with XVault and not sure why I stopped working on it.  After reading an
intriging real life story, recently, I started thinking about this project again.  XVault was an idea created 20 yrs ago around the idea of keeping data
safe from "hackers".  Today, this idea is more relevant.



## Legal

If you have any questions about the content of the repository, please email [matt.raffel@gmail.com](mailto:matt.raffel@gmail.com). I can assure you all content is either open source or has been purchased and licensed to me. Proof will be made available on request. Repeated DCMA counterfit and harassment claims will result in counter suits per Section 512(f) of the DMCA penalties for *misrepresentation can include actual damages and attorney’s fees*.  



## Status

Active research POC -- V1 complete; V2 security work (Phase A) implemented and opt-in, with follow-up hardening tracked in the docs.  Limited updates -- this project is created to explore the idea.

---
_Version: 2026.09.19_

