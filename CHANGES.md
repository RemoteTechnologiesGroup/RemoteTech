Version 1.8.0
=============
Released October 15, 2016

KSP 1.2 update

What's New?
===========

* Ported the RemoteTech code base to KSP 1.2. [#678]
* Updated ModuleManager to the lastest 2.7.1 version (Nyan Cat ftw!).
* Prevent parsing objects that have no chance (Asteroid, Unknown objects and flags) of being RT controlled.
* Add and option to enable / disable CommNet alongside RemoteTech.
* Moved all RT antennas to the new Communication category in VAB / SPH.
* Added RemoteTech RLA config file. [#469]
* Antenna info in VAB /SPH indicate "range" or "power" depending on the chosen RangeModel ("range" is technically wrong when in Root model). [#471]
* Fixed an issue when the "RTAntennaTarget" field was empty in a save file. [#506]
* Exception bubbling up from KSP or other mods are now caught in RT when staging. [#525]
* Flight Computer window is now closed when switching to another vessel or leaving the flight scene.[#578]
* Added a new API to change the range of ground stations. [#589]
* Antenna range multiplier option slider is no longer snapped to 0 or 1. [#594]
* Antenna range multiplier has now a maximum value of 5 (previously 2).
* "World Scale" option sliders are now using 2 decimal places instead of 6. [#601]
* Added a new option slider: range multiplier for ground stations / mission control. [#602]
* Fixed a NRE spamming issue while transmitting science. [#613]
* Fixed various issues with the connection indicator and various UI scales. [#618 & #668]
* Kerbals can now transfer themselves even if there's no connection and they are not in a command module (this also bypass any signal delay). [#626]
* Fixed an issue when a craft with an uncrewed ModuleCommand but with a ModuleSPU was deemed non controllable. [#628]
* Flight Computer can be switched to "Off" instead of "KillRot" after maneuver execution or abort (configurable through settings file). [#631]
* Added a new "LeadTime" field (in settings fileà to control how much time attitude control is queued ahead of the maneuver (default 3 minutes like the previous hardocded value). [#631]
* Command Stations can now control themselves (if properly crewed and with the right SPU module) even if there's no one in a command module. [#633]
* Fixed various problems with the github readme and the user manual. [#669, #670, #680]
* Added current vessel name to flight computer (when UI is deployed). [#673]


Detailed Changelog
==================

Fixed Issues
------------

* Issue #469: Updated RLA config. Work made by kitoma [requested by: @marosciacchitano]
* Issue #471: Handling GetInfo()'s "range" display when in rangemodel Root [reported by: @NathanKell]
* Issue #506: Blank RTAntennaTarget, Exception stopping the registrations. [reported by: @OzoneGrif]
* Issue #525: Missing parts in staging cause NRE spam, lock staging. [reported by: @NathanKell]
* Issue #578: Flight Computer doesn't close when going back to Space center view. [requested by: @d4rksh4de  & @petersohn (in #673)]
* Issue #589: API - Changing range of GroundStations [requested by: @AlphaAsh]
* Issue #594: MultipleAntennaMultiplier is snapped to 0 or 1. [reported by: @WazWaz]
* Issue #601: World Scale sliders under options are too granular. [reported by: @gnivler]
* Issue #602: Mission Control not affected by Range Multiplier option. [requested by: @gnivler]
* Issue #613: NRE Spamming when transmitting science (RT + other mods) [reported by: @lamont-granquist]
* Issue #618: The connection indicator does not scale properly. [reported by: @tomekpiotrowski]
* Issue #626: "Transfer crew" button in context popup menu should be always available. [reported by: @mscg82]
* Issue #628: ModuleSPU on a crewed-command part with no crew = no control [reported by: @NathanKell]
* Issue #633: Command Station has no local control. [reported by: @FancyMouse]
* Issue #668: UI Scale Not Configurable. [reported by: @evan2645]
* Issue #669: Player's Guide contains dead link to Advanced Settings [reported by: @HupfderFloh; PR #670 by @ahmedcharles]
* Issue #673: Add vessel name to flight computer. [requested by: @krenshala]


Pull Requests
-------------

* PR #631: Various FlightComputer improvements [PR by: @gnivler]
* PR #670: Fix issue #669 (broken links on user manual) [PR by: @ahmedcharles]
* PR #678: Port RT base code to KSP 1.2 [PR by: @neitsa]
* PR #680: Various Readme.md updates [PR by: @keyspace]

Warning
=======

* CommNet is disabled by default if RemoteTech is enabled.
    - CommNet can still be enabled alongside RemoteTech in the RT option window but this has not been tested.
* If you enable CommNet, consider using 2 types of satellites:
    - Satellites with only RT antennas.
    - Satellites with only CommNet antennas.
    - Do not try to mix both: we won't fix these problems with *this* version of RemoteTech.


Version 1.7.1
========================================
Released July 2, 2016

KSP 1.1.3 update

General
--------------------

- Updated to work with KSP 1.1.3
- Updated ModuleManager
- Fixed tracking station dish button
- Support for Procedural Probe Cores
- Wording/typo fixes


Version 1.7.0
========================================
Released May 6, 2016

KSP 1.1.2 update

**NOTE:** Most of the features and bug fixes that are included in this release were implemented by Dennis (Peppie23) last year, before he left the project.

General
--------------------
- The RemoteTech_Settings.cfg will now be saved under the save game folder
- Added an OptionWindow
- Added an AppLauncher Button on the SpaceCenter to open the OptionWindow
- Added disable/enable option for RemoteTech
- RemoteTech is now disabled for training missions
- Remote station dots can now be colored via the settings file (also with the OptionWindow)
- We replaced the label "Energy req." with "ElectricCharge" for the part info on the editor, to be more stock like
- Added more colors to the editor part infos (more stock like)
- Add ManeuverCommands from Node. You can now add more than one maneuver to the flightcomputer directly from the node. Right click the node an click the FC+/FC- Button
- Merged RemoteTechXF you will find the option to toggle on the Cheat-Tab in the option window
- Presets added to the Option window
- The mission control dot on the map view will now be invisible on a defined distance to the camera. See `DistanceToHideGroundStations` on the settings file. This feature can be toggled by the value
`HideGroundStationsOnDistance` in the option window.
- I've added a mouse over info box for each mission control dot on the map view. This feature can be toggled by the value `ShowMouseOverInfoGroundStations` in the option window.
- The default mission control ground station has now three tech level on a career game for the omni antenna. The ranges are: 4Mm, 30Mm and 75Mm
- ManeuverCommands and BurnCommands will now insert an alarm into KerbalAlarmClock.
- ManeuverCommands will now automatically insert a HoldManeuver Command
- Added a new option to activate/deactivate the auto insert to KerbalAlarmClock
- Implemented a new PID controller to solve some of the steering issues
- Deployed antennas will now survive Hyperedit orbit changes
- Sounding Rockets inline probe will now contain a signal processor
- Near Future Construction Truss Octo Drone will now contain a signal processor
- FASA Mercury Antenna Cap will now be a RemoteTech antenna

Modders
--------------------
- Added a new API Method to check whether RemoteTech is enabled or not `bool IsRemoteTechEnabled()`
- Added a new API Method to check an individual antenna for connection `bool AntennaHasConnection(Part part)` (thx to tomekpiotrowski)
- Added a new API Method to get the target from an antenna `Guid GetAntennaTarget(Part part)` (thx to tomekpiotrowski)
- Added a new API Method to switch the antennas target `void SetAntennaTarget(Part part, Guid id)` (thx to tomekpiotrowski)
- Added a new API Method to receive all ground stations `IEnumerable<string> GetGroundStations()` (thx to tomekpiotrowski)
- Added a new API Method to receive a guid from a ground stations name `Guid GetGroundStationGuid(String name)` (thx to tomekpiotrowski)
- Added a new API Method to get the guid from a celestial body `Guid GetCelestialBodyGuid(CelestialBody celestialBody)` (thx to tomekpiotrowski)
- Added a new API Method to get the NoTarget guid `Guid GetNoTargetGuid()` (thx to tomekpiotrowski)
- Added a new API Method to get the ActiveVessel guid `Guid GetActiveVesselGuid()` (thx to tomekpiotrowski)
- You can now add the value `IsNonRetractable = boolean` to the ModuleRTAntenna section on the part.cfg
- You can now configure a dish antenna to mission control. All mission control dishes are only pointed to the Active Vessel (thx to jdmj)
- Ground station antennas can now use the tech level of the tracking station. I've added three new values to the antenna node for a station. Use `UpgradeableOmni`, `UpgradeableDish` and `UpgradeableCosAngle` with a semicolon seperated list of ranges. Example: 4E+06;3.0E+07;7.5E+07

Bugfixes
--------------------
- RSS/RO/RP-0 Player will no longer see a required ElectricCharge of 0.00/s for the longAntenna on the editor part info. It's now defined as minutes (~0.09/min)
- Fixed an issue where the TechPerk is already available only if i researched the node but without purchasing the TechPerk-Part it self and the editor part info will no longer show the TechPerk right from the beginning


Version 1.6.11
========================================
Released April 13, 2016

General
--------------------

- Fix RT build process - invalid .zip contained some extra files and caused bugs

Version 1.6.10
========================================
Released April 12, 2016

General
--------------------

- KSP 1.1.0 update
- Science transmission fixed


Version 1.6.9
========================================
Released November 10, 2015

General
--------------------
- KSP 1.0.5 update
- Small kOS update for invoking events


Version 1.6.8
========================================
Released September 12, 2015

General
--------------------
- Added a new value to RTSettings to keep the throttle on connection loose (ThrottleZeroOnNoConnection=True or False)
- Clean up vessel target handling (thx to geoffromer)
- Added Asteroid Day antennas (thx to phroggster)
- Added NovaPunch antennas (thx to blnk2007)


Version 1.6.7
========================================
Released June 25, 2015

Bug fixes
--------------------
- Fixed a problem for transmitting science data in combination with ScienceAlert

General
--------------------
- KSP 1.0.4 update


Version 1.6.6
========================================
Released June 21, 2015

Bug fixes
--------------------
- Fixed an exception-Spamming for `HideGroundStationsBehindBody = true` with RSS
- Fixed the animation for Fasa's Explorer probe

General
--------------------
- Added CactEye probes (thx xZise)
- Added SignalProcessor to Fasa's Pioneer probe


Version 1.6.5
========================================
Released May 18, 2015

Bug fixes
--------------------
- We've fixed an issue while loading a saved maneuver command
- We've fixed an issue while loading a saved cancel command
- Queued commands will now sorted correctly
- The flight computer will no longer goes crazy if a queued BaseEvent throws an exception
- We've fixed the SoundingRockets config file
- Satellite/Stations will now properly re-registered as a satellite after unloading (>2.5km distance)

General
--------------------
- Textures are now converted to DDS (thx @InsanePlumber)
- We'll no longer throttling back the timewarp if you are on phys.warp
- We'll no longer fix the "roll" position for maneuver, orbit und surface commands


Version 1.6.4
========================================
Released May 07, 2015

- KSP 1.0 compatibility

Flightcomputer
--------------------
- The +/- Buttons on the pitch/head and roll fields are now trigger buttons. You can now hold the mouse button to increase/decrease the value
- You can now use the mousewheel over the pitch/head and roll input fields to increase/decrease the value

General
--------------------
- Added configs for Sounding Rockets
- A bunch of cleanup
- Modulemanager update to v2.6.3
- We'll now support [ControlLock-Addon](http://forum.kerbalspaceprogram.com/threads/108561-0-90-%28Apr12-15%29-Control-Lock-Input-text-into-text-fields-without-issuing-commands-to-your-vessel) created by Diazo
- Added the LLLMicrochip to the LLL_Probes.cfg
- Maneuver burns are now 100% precise by decreasing the throttle at the end of the burn

Modders
--------------------
- Modders can now add their own commands directly to the flightcomputer queue (experimental) For more infos please see this [thread](https://github.com/RemoteTechnologiesGroup/RemoteTech/issues/233)
- We added a new method `HasLocalControl(GUID): bool` to check the vessel is local controlled or not

Bugfixes
--------------------
- We fixed the double definition of LLLCommPole2 on the LLL_Antennas.cfg
- We fixed inaccurate maneuver burns with monoprop engines


Version 1.6.3
========================================
Released February 06, 2015

Bug Fixes:
--------------------
- We've fixed an old issue where unloading a vessel can cause a log spamming with KeyNotFoundException
- We'll now log the current RemoteTech FileVersion to the ksp.log
- Stations will now properly re-registered as a station after unloading
- Stations will now properly registered as a station even if the first part is not the Remote Guidance Unit
- Fix for loading a saved RemoteTech EventCommand like 'activate antenna'


Version 1.6.2
========================================
Released January 24, 2015

Bug Fixes:
--------------------
- Fixed an issue that can cause the KSP UI to be not clickable anymore after docking (thx DaveTSG for reporting)
- Fixed an issue that can cause the flight computer to crash into a small gray dot while loading a saved EventCommand (thx Synighte for reporting)
- Fix for saving/loading a ManeuverCommand
- Reverted a change of the AssemblyVersion from 1.6.1 to 1.6.0 to prevent issues with other mods that use our API (thx jrossignol)
- We fixed an old issue where KSP can freeze by zero cost links between two satellites


Version 1.6.1
========================================
Released January 19, 2015

Bug Fixes:
--------------------
* Fix for the calculation of the manual delay after switching to the vessel with saved commands
* The disappeared satellite switcher on the map view (middle right) will now displayed correctly
* The attitude action-buttons now shows you the current flight mode
* Fix for the stopped timer of commands after a maneuver command


Version 1.6.0
========================================
Released January 11, 2015

Features:
--------------------
##Flightcomputer:
* Save/Restore Flightcomputer values and queued commands.
* Added a new button to every queued command to set the manual delay right after the queued one.
* Added a new button to the manual delay field to set the manual delay.
* The altitude buttons are no longer toggle buttons. To deactivate the current mode please use the small 'X' on the queue-window by the activated command.

##General:
* Added a mouse over tooltip to the antenna target window to show distance, status to the target
* Added configs for AIES, Lack Luster Labs, Near-Future Spacecraft, and NovaPunch
* Possibility to hide ground stations with the new property `HideGroundStationsBehindBody`
* Hide RemoteTech windows,overlays and buttons when the GUI is hidden
* Window positions for Flightcomputer and AntennaWindow will now be saved for the current ksp instance

##Contributors:
* We removed the dependency to the task extensions

##Modders:
**Info** We refactored the namespace definitions of RemoteTech. The API class is no longer on the `RemoteTech` namespace. Please use `RemoteTech.API` for now.
* RTSettings now reads settings from the GameDatabase to tweak settings for specific mods
* Possibility to tint groundstations with the property `MarkColor` `Syntax is R,G,B,A`

Bug Fixes:
--------------------
* Dishes will now attempt to connect to all targets within their field of view
* Cones will now displayed for any target
* Fixed the thrust calculation for flamed out engines
* Some refactoring and small fixes

Version 1.5.2
========================================
Released December 21, 2014

Bug Fixes:
--------------------
* Compatible with KSP 0.90.
* Flight computer now holds the orientation to non-root target part
* Some minor bugfixes

Version 1.5.1
========================================
Released October 9, 2014

**WARNING:** the 1.5 release changes the mod's folder and DLL names from `RemoteTech2` to `RemoteTech`. If you are upgrading from 1.4, you must delete the old `RemoteTech2` directory before installing this version. We take **NO RESPONSIBILITY** for any bugs that may happen from having both `RemoteTech` and `RemoteTech2` in your `GameData` folder.

Bug Fixes:
--------------------
* Can now read settings files from RemoteTech 1.4 or earlier.
* Map view will no longer crash when centering on different vessels.
* FASA antennas won't crash when transmitting science.
* FASA launch clamps now provide a communications line, just like stock clamps.
* Flight computer now uses Kerbin or Earth days, as appropriate.
* Tech tree node for integrated omni antenna now displays correctly.

Version 1.5.0
========================================
Released October 7, 2014

**WARNING:** this release changes the mod's folder and DLL names from `RemoteTech2` to `RemoteTech`. You must delete the old `RemoteTech2` directory before installing this version. We take **NO RESPONSIBILITY** for any bugs that may happen from having both `RemoteTech` and `RemoteTech2` in your `GameData` folder.

Features:
--------------------

* Compatible with KSP 0.25.
* The mod has officially been renamed from RemoteTech 2 to RemoteTech. Ignore the warning above at your own peril.
* Vessel lists in antenna targeting window and in map view can now be customized using the map view filters (thanks to monstah for the suggestion!)
* If you use FAR or NEAR, you can now protect antennas from breaking by putting them inside a fairing.
* The number of crew needed to both operate a command station and fly a ship can now be configured on a part-by-part basis.
* B9 and FASA parts now officially supported.
* The 3 km omni upgrade to probe cores now appears in the tech tree.
* Module Manager patches now support MM2 features, including `:BEFORE`, `:FOR`, and `:AFTER` patch ordering.
* Updated KSP-AVC support, including more flexible KSP version requirements. Can now use KSP-AVC to download release notes in-game.

Rule Changes:
--------------------

* Flight computer clocks will keep running even if the vessel runs out of power (though you still need power to actually *do* anything). This is a workaround for a KSP bug that causes energy consumption to be overestimated at maximum time warp.

Bug Fixes:
--------------------

* Icons in map view easier to understand.
* Research will now be completely transmitted in 64-bit KSP.
* Pointing a dish at Mission Control from a fresh RemoteTech install will no longer corrupt saves.
* Ships will no longer have signal delay many times larger than they should.
* Flight computer will now take engine gimbaling into account when slewing.
* KSP should no longer crash when running the flight computer on a ship with no rotation torque.
* RemoteTech modules will no longer be added twice to the same part.
* If a ship is loaded while out of contact and uncontrollable, you will no longer be able to toggle controls or action groups.
* RemoteTech config files will no longer appear to be on one line when opened in a text editor that only recognizes Windows line endings.
* Nonstandard RemoteTech installations will no longer cause missing (a.k.a. "pink") textures.

Version 1.4.1
========================================
Released August 28, 2014

Features:
--------------------

* Compatible with KSP 0.24. Some part costs have changed.

Bug Fixes:
--------------------

* Flight computer now slews much more accurately and efficiently.
* Flight computer will now take RCS thrust into account when slewing.
* Signal delay window is now sized properly for all Flight GUI settings.
* Ships can now be renamed from the right-click menu at any time. Note that they can still be renamed from the tracking station.
* Reverted 1.4.0 fix for electric charge consumption at higher time-warp factors (fix was causing electricity use to be underestimated).
* Fixed conflicts with mods that depend on RemoteTech.
* Reduced logging should cause less lag.

Version 1.4.0
========================================
Released June 16, 2014

Features:
--------------------

* KSP-AVC Support
* EXEC commands are now executed at the correct time in advance of the maneuver node (accounts for signal delay and vessel acceleration)
* Added logging to API - useful for developers of mods that wish to interact with RT - requires `VERBOSE_DEBUG_LOG = True` in `settings.cfg`

Bug Fixes:
--------------------

* Fixed display of execution delay for simple commands such as running experiments
* **Significantly reduced** occurrences of the dreaded 'vessel duplication' bug
* Fixed display of the Flight Computer icon below the time-warp/clock display in the upper-left of the screen
* Fixed electric charge consumption at higher time-warp factors
* Prevented flight computer from crashing when scheduling a Maneuver Node Execution ('EXEC') command when no engines are active
* Fixed orientation when orienting in Target ('TGT') reference frame
* Fixed calculation of burn time for EXEC commands when engine thrust limit is less than 100%
* Fixed calculation of burn time for EXEC commands when using ModuleEnginesFX parts (eg. NASA 'Kerbodyne' engines)

Version 1.3.3
========================================
Released December 26, 2013

* 0.23 compatible;
* Switch vessel focus on the map view, allowing easy editing of targets;
* Flight Computer rewrite;
* Some UI elements were rewritten;
* Node Execution;
* Tweaks, bug fixes;

Version 1.2.7
========================================

* Tweaked per-frame load-balancing and fixed a divide-by-zero;
* ModuleManager 1.5 by Sarbian. **Please delete the old one;**
* Static class serving as API for future kOS integration;
* Fixed bug in TimeWarp throttling.

Version 1.2.6
========================================

* Fixed settings file not auto-generating;
* Fixed collection modified exception;
* Possibly fixed throttle jitter again?;
* Fixed Target/Maneuver in Flight Computer;
* Disable SAS when Flight Computer is on;
* Tweaked time warp throttling when commands are approaching.

Version 1.2.1
========================================

* Removed kOS integration.

Version 1.2.0
========================================

* Fixed (probably) the bug that humiliated me in one of Scott Manley's videos;
* Flight Computer and kOS integration! (details below); Requires patched dll for now!
* Fixed Delta-V burns; Cilph can't do basic vector math late at night;
* "N/A" on TimeQuadrant when no SPU is available; "Connected" when signal delay is disabled;
* Allow docking node targeting when the other vessel is disconnected;
* Halved antenna power consumption;
* Signal Delay is now on by default;
* Quick hack to not cache lines/cones that are disabled to increase performance for some;
* Possible fix for long range planet targeting being twitchy;
* Two new dish parts by Kommit! The KR-7 and KR-14 replace the SS-5 and LL-5 respectively. The old parts will be deprecated but still included so as not to break existing crafts;
* Settings File is now properly created on load.

###kOS integration:
* All immediate mode actions now require signal delay to pass;
* Steering locks now work when the vessel is disconnected;
* Action groups no longer trigger when kOS has them blocked;
* "BATCH" and "DEPLOY" commands to send a list of commands as one packet;
* ~~Requires patched kOS DLL until I merge the changes. Download here.~~

###Flight Computer:
* Works pretty much like MechJeb's SmartASS;
* Use "Enter" to confirm changes in most text fields;
* Duration format: "12h34m45s" or "12345" for plain seconds;
* Delta-V burn format: "1234m/s";
* Cancel commands by clicking on the X in the queue, sends signal ASAP, but delay still applies.
* Use the queue to view any delayed command; even right-click menu events.


Version 1.1.0
========================================


* Added a settings file; auto-generates ~~once the plugin loads.~~ once forced to save by editing the map filter.
* Filter in map view now properly saves.
* Tracking Station should now save changes made to dishes.
* Fixed NPE in editor due to recent ModuleSPUPassive changes
* ModuleRTAntennaPassive now correctly works when unloaded
* Dishes can now target the active vessel.
* Promised Flight Computer NOT enabled yet. The new Squad SAS is too unstable for auto-piloting.
* Signal Delay can be enabled in settings file. Good luck without that flight computer.

Version 1.0.7
========================================


* Fixed NPE when antennas did not have satellites during rendering.
* ModuleSPUPassive now provides unloaded routing if the craft was controllable as a whole. Might have lead to unintentional bugs. (vardicd)

Version 1.0.6
========================================

* Fixed science transmissions not sending all of the data. (Kethevin)

Version 1.0.5
========================================

* Fix MechJeb fighting over throttle.
* Added ModuleSPU to radial MechJeb AR202 casing. This kills functionality if connection is lost.
* Fix queuing for science transmissions. (Ralathon)
* Added a ScreenMessage if your partmenu events are blocked due to signal loss.

Version 1.0
========================================

* Initial release!
