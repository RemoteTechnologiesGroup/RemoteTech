# Contributing to RemoteTech


Thank you for helping make RemoteTech better! We'd like this to be a community effort. However, we do have a few rules we need you to follow in order to keep things running smoothly, whether you're reporting a bug or submitting code.

## Bug Reports


Before issuing a bug report, please follow the following steps:

* Check if your issue was already reported in the [existing issues](https://github.com/RemoteTechnologiesGroup/RemoteTech/issues?utf8=%E2%9C%93&q=is%3Aissue).
 * If you can find one that is related to your problem, please comment there instead of making a new one.
 
* Make sure the issue title is descriptive, for example "Losing connection above 10km" instead of "Connection doesn't work". 

* Post your issue using this template:

```
O.S: <system type> <version> <bitness (32 / 64 bits)> 
KSP: <version> <bitness (32 / 64 bits)>
Problem: <describe precisely your problem>

Reproduction steps:
    <how can we reproduce your problem; describe exactly the steps involved>
        
Logs:
 <link>Output_log.txt

Installed Mods: 
 <list> or <link to list> 

Persistent save:
 <link to persitent save>

Screenshot:
    Note: optional / if meaningful only.
    <when your reproduce your problem, take a screenshot in KSP (F1 key)>
```

* In `Reproduction steps` list the exact steps or conditions to reproduce the bug. 
     * Instructions of the form "1. Load stock KSP. 2. Load this ship 3. Do this" are best. The easier we can make the bug happen on our end, the easier it is to fix it.

* In `Logs` provide your KSP log file:
      * **Windows**: `KSP_win\KSP_Data\output_log.txt` (32bit) or `KSP_win64\KSP_x64_Data\output_log.txt` (64bit)
      * **Mac OS**: Open Console, on the left side of the window there is a menu that says 'files'. Scroll down the list and find the Unity drop down, under Unity there will be `Player.log` ( Files>`~/Library/Logs>Unity>Player.log` )
      * **Linux**: `~/.config/unity3d/Squad/Kerbal Space Program/Player.log`     

* In `Installed Mods`, list all installed mods (preferably WITH version number) 
      * If you use `ckan` then use it to list your mods (`File` > `Export Installed mods`).
      * You might want to use [pykan](https://github.com/ajventer/pyKAN/releases/tag/0.1.0) if you're not using `ckan`.
      * Put the list either in the issue body, on [gist](https://gist.github.com/) (easier to edit later), [pastebin](http://pastebin.com/), or any similar site ([Fedora pastebin](http://fpaste.org),  [pastie](http://pastie.org/)).

* In `Persistent Save` provide a link to your current persistent save file. This should be ```Kerbal Space Program\saves\[yoursave]\persistent.sfs```, add it to your gist as a new file or as a separate pastebin(-esque).

* In `Screenshot` (optional) If it's easy to see in an image, please attach a screen shot (use F1 key when in KSP).

### Triming down the problem

Despite how complicated it may seem, it is actually quite simple to do (even for Steam users), as you can simply copy your install directory elsewhere as needed and remove non problematic mods. It's especially useful since interaction bugs are some of the hardest to nail down; something that appears to be a bug in one mod might actually be caused by something else.

If the issue stems from only `RemoteTech`, try to replicate this issue with only `RemoteTech` in a clean KSP install. 

If the issue stems from the use of several different mods (e.g. `RemoteTech` and ScanSat or FAR, etc.) then get a clean install of KSP and install only these mods (and their dependencies, if applicable).

This may seem like an extreme measure, but it ensures that only `RemoteTech`and these mods (or maybe the stock game) are to blame.

## Pull Requests

When writing new code for RemoteTech, please follow the following steps:
* Read the titles of open issues and pull requests, to see if somebody's already thought of your idea. If the issue has somebody assigned (see the rightmost column of the page), it's already being taken care of. Otherwise, assign yourself and/or add a comment saying you'll do it.
* Fork RemoteTech and do any code updates. Like most KSP plugins, RemoteTech's code is organized around a solution (`.sln`) file, which can be opened with [any of several C# development tools](http://wiki.kerbalspaceprogram.com/wiki/Plugins). Third-party developers can ignore the `build.remotetech.sh` script; it is needed only to support automatic builds from the online repository.
* What happens next depends on what the new code does:
    - If your code fixes a bug in RemoteTech, create a pull request to the `develop` branch (this should be the default).
    - If your code adds a new feature in RemoteTech, create a pull request to the branch of the next release (e.g., `1.7.0`; if no such branch exists, please contact us).
* One of the Remote Technologies Group members will merge the pull request; if you yourself are a member, please wait one week to give others a chance to give feedback.
