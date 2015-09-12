---
title: Modding
layout: content
navbar: false
---

{% include banner.html %}

**UNDER CONSTRUCTION:** Please watch your step!
{: .alert .alert-danger}

# Configuring Parts for RemoteTech

{% include toc.html %}

RemoteTech defines several new part modules appropriate for antennas or probe cores. These can be added via ModuleManager, as done in the default RemoteTech installation, or you can include them in your parts directly.

## ModuleManager Conventions

All patches incorporated as part of RemoteTech are placed in the `:FOR[RemoteTech]` slot. Please use `:BEFORE[RemoteTech]`, `:AFTER[RemoteTech]`, and/or `:NEEDS[RemoteTech]` in your own patches.

## Antenna Modules

### ModuleRTAntenna

This module represents a part that can recieve control transmissions from another vessel. You **must** remove any `ModuleDataTransmitter` modules from the antenna if using `ModuleRTAntenna`. It has the following fields:

`IsRTActive` (default false)
: Determines whether the antenna is turned on when a vessel is loaded.

`Mode0DishRange`, `Mode1DishRange`
: The range of the antenna in meters, used as a dish. `Mode0DishRange` indicates the range when the dish is turned off or retracted, `Mode1DishRange` indicates the range when the dish is turned on or deployed. Do not use together with `Mode*OmniRange`, as unexpected behavior may result.

`Mode0OmniRange`, `Mode1OmniRange`
: The range of the antenna in meters, used as an omnidirectional antenna. `Mode0OmniRange` indicates the range when the antenna is turned off or retracted, `Mode1OmniRange` indicates the range when the antenna is turned on or deployed. Do not use together with `Mode*DishRange`, as unexpected behavior may result.

`DishAngle`
: The diameter of the dish cone, in degrees.

`EnergyCost`
: The amount of ElectricCharge consumed per second when the antenna is turned on.

`MaxQ`
: The ram pressure in Newtons per square meter that will cause the antenna to break when deployed (non-animating antennas are considered always deployed for this purpose). For reference, the ram pressure in Newtons per square meter at Kerbin sea level is 0.62 v<sup>2</sup>, where v is the vessel's speed in meters per second.

`DeployFxModules`
: Like in the stock `ModuleDataTransmitter` module, this must be the number (with 0 representing the first module) of a `ModuleAnimateGeneric` module associated with antenna deployment. Any animation modules used by a RemoteTech antenna must have `allowManualControl = false`.

`ProgressFxModules`
: Like in the stock `ModuleDataTransmitter` module, this must be the number (with 0 representing the first module) of a `ModuleAnimateGeneric` module associated with the antenna transmitting. Any animation modules used by a RemoteTech antenna must have `allowManualControl = false`.

`TRANSMITTER`
: This block, if present, indicates that the antenna can send science transmissions through the communications network. The `PacketInterval`, `PacketSize`, `PacketResourceCost`, and `RequiredResource` fields have the same meanings as those in the stock `ModuleDataTransmitter` module.


### ModuleRTAntennaPassive

This module represents an omnidirectional antenna that is always on, but consumes no power. Intended as a secondary function on non-antenna parts, such as probe cores.

`TechRequired` (default "None")
: The name of a technology that will enable this module. If set to a value other than "None" and the technology is not on the list of researched techs (spelling matters!), the module will have no effect.

`OmniRange`
: The range of the antenna in meters.

`TRANSMITTER`
: This block is identical to that for `ModuleRTAntenna`.

### ModuleSPUPassive

This module allows any vessel with an antenna to participate in a RemoteTech network, even if it does not have a `ModuleSPU`. It should be included in all RemoteTech antennas. Unlike `ModuleSPU`, it does not filter commands or provide a flight computer.

## Control Modules

### ModuleSPU

This module represents the "autopilot" living in a probe core. A vessel will only filter commands according to network availability and time delay if all parts with `ModuleCommand` also have `ModuleSPU`; otherwise, the vessel can be controlled in real time. Having at least one `ModuleSPU` on board is also required to use the flight computer. `ModuleSPU` has the following fields:

`IsRTCommandStation` (default false)
: If set to true, the vessel can serve as a command station if enough kerbals are on board. Otherwise, the vessel acts as an ordinary satellite.

`RTCommandMinCrew` (default 6)
: The minimum number of kerbals needed to operate a command station alongside the ship's systems. Ignored if `IsRTCommandStation = false`.
