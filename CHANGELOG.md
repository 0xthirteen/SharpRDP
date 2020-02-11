# Changelog

## 2020-02-11

### Added

* Following extra flags added 
    * nla - add network level authentication
    * takeover - if a user is logged on, prompts to take over session, they are given 30 seconds to respond before signing them out
    * connectdrive - connects drives of RDPing host to target. To access files specify `\\tsclient\c\location\of\files` (credit to @scriptmonkey_)
    * elevated - executes command elevated. Options are winr (currently bugged), and taskmgr. (credit to @mpgn_x64 for taskmgr)
* Better logging for disconnection 

### Fixed

* Fixed exit for cmd and powershell (credit @timhir)

## 2020-01-21

#### Initial release