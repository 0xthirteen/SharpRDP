# Changelog

## 2026-07-28

### Added

* Full uppercase A-Z character support (Shift+key scancodes)
* 17 missing special characters: = ` ! @ # $ ^ * _ + ~ { } < > ?
* `clipboard=true`, paste command via clipboard + Ctrl+V instead of individual keystrokes, falls back to keystrokes on failure
* `delay=N`, timing delay multiplier for slow/high-latency connections (default: 1)
* `timeout=N`, connection timeout in seconds to prevent hanging on unreachable targets
* `port=N`, custom RDP port support (default: 3389)
* `gateway=host`, RDP Gateway support for reaching internal targets through a gateway server
* `output=path`, command output capture via drive redirect, reads output back and prints to console (use with connectdrive=true)
* Clipboard save/restore, previous clipboard content is preserved when using clipboard mode

### Fixed

* Removed cmd.ToLower()
* Fixed crash on unmapped characters, now warns and skips instead of throwing KeyNotFoundException
* Fixed elevated execution via Win+R (Ctrl+Shift+Enter), was sending modifier keys and Enter as separate events
* Fixed session takeover, was releasing COM objects and returning without executing the command
* Fixed Environment.Exit(0) from COM event handlers, replaced with clean form close via BeginInvoke to properly unwind and release resources
* Fixed duplicate "winr" condition check in argument parsing
* Fixed COM object leak, GetOcx() was called 3 times creating multiple references
* Fixed disconnect reason using magic number, now uses DisconnectReasons enum
* Fixed null reference on unknown logon/disconnect error codes
* Fixed assembly resolver crash when embedded resource stream is null
* New Client instance per target, was reusing single instance with stale state across comma-separated hosts
* Empty hostnames from trailing commas in computername are now filtered out

### Updated

* Extracted ExecuteCommand() as shared method between normal login and takeover paths
* Replaced Thread.Sleep polling loop with Thread.Join for cleaner thread management
* All timing delays now route through Delay() helper that respects the delay multiplier
* Removed dead code: if(true) block, unused System.IO and System.Diagnostics imports

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