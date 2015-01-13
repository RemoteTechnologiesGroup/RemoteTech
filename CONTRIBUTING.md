Contributing to RemoteTech
===========

Thank you for helping make RemoteTech better! We'd like this to be a community effort. However, we do have a few rules we need you to follow in order to keep things running smoothly, whether you're reporting a bug or submitting code.

Bug Reports
-----------

Before issuing a bug report, please follow the following steps:
* Read the titles of the open issues, does it sound familiar to your problem? Comment there instead of making a new one.
* Make sure the issue title is descriptive. "Losing connection above 10km" instead of "connection doesn't work".
* Please list the exact steps or conditions to reproduce the bug. Instructions of the form "1. Load stock KSP. 2. Load this ship 3. Do this" are best. The easier we can make the bug happen on our end, the easier it is to fix it.
* List all installed mods (preferably WITH version number) either in the issue body, on [gist](https://gist.github.com/) (Easy to edit later), [pastebin](http://pastebin.com/), or any similar site ([Fedora pastebin](http://fpaste.org),  [pastie](http://pastie.org/)).
* Provide your persistance save! This is ```Kerbal Space Program\saves\[yoursave]\persistent.sfs```, add it to your gist as a new file or as a seperate pastebin(-esque).
* If KSP is crashing or getting corrupted, include your log file. This is ```Kerbal Space Program\KSP_Data\output_log.txt``` if you play 32-bit KSP and ```Kerbal Space Program\KSP_x64_Data\output_log.txt``` if you play 64-bit KSP. Please don't send ```KSP.log```, it's missing valuable debugging information.
* (optional) If it's easy to see in an image, please attach a screenshot.

Pull Requests
-----------

When writing new code for RemoteTech, please follow the following steps:
* Read the titles of open issues and pull requests, to see if somebody's already thought of your idea. If the issue has somebody assigned (see the rightmost column of the page), it's already being taken care of. Otherwise, assign yourself and/or add a comment saying you'll do it.
* Fork RemoteTech and do any code updates. Like most KSP plugins, RemoteTech's code is organized around a solution (`.sln`) file, which can be opened with [any of several C# development tools](http://wiki.kerbalspaceprogram.com/wiki/Plugins). Third-party developers can ignore the `build.remotetech.sh` script; it is needed only to support automatic builds from the online repository.
* What happens next depends on what the new code does:
    - If your code fixes a bug in RemoteTech, create a pull request to the `master` branch (this should be the default).
    - If your code adds a new feature in RemoteTech, create a pull request to the branch of the next release (e.g., `1.7.0`; if no such branch exists, please contact us).
* One of the Remote Technologies Group members will merge the pull request; if you yourself are a member, please wait one week to give others a chance to give feedback.
