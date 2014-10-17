---
title: API
layout: content
navbar: false
---

{% include banner.html %}

**UNDER CONSTRUCTION:** Please watch your step!
{: .alert .alert-danger}

#RemoteTech Software API

{% include toc.html %}

RemoteTech presents a public API for use by other mods. All API functions can be found in the `RemoteTech.API` static class.

##Vessel State

###`bool API.HasFlightComputer(Guid id)`

This function tests whether a vessel has a flight computer on board.

**Parameters**

`id`
: The pid value of the vessel being tested.

**Return Value**

True if `id` is the id of a known vessel with a flight computer; false otherwise. In particular, returns false if `id` is not a vessel.

**Exceptions**

Does not throw exceptions.

###`bool API.HasAnyConnection(Guid id)`

This function tests whether a vessel can recieve commands from a ground station or command station.

**Parameters**

`id`
: The pid value of the vessel being tested.

**Return Value**

True if `id` is the id of a known vessel with a working connection; false otherwise. In particular, returns false if `id` is not a vessel.

**Exceptions**

Does not throw exceptions.

###`bool API.HasConnectionToKSC(Guid id)`

This function tests whether a vessel can transmit science data to a ground station. Despite the name, if the game has multiple ground stations then any of them will qualify.

**Parameters**

`id`
: The pid value of the vessel being tested.

**Return Value**

True if `id` is the id of a known vessel with a working connection to a ground station; false otherwise. In particular, returns false if `id` is not a vessel, or if the vessel designated by `id` can connect only to a vessel command station.

**Exceptions**

Does not throw exceptions.

###`double API.GetShortestSignalDelay(Guid id)`

This function measures the signal delay experienced by a vessel when sending commands.

**Parameters**

`id`
: The pid value of the vessel whose signal delay is desired.

**Return Value**

If [`HasAnyConnection(id)`](#bool-apihasanyconnectionguid-id), the number of seconds of signal delay introduced. Otherwise (including nonexistent vessels), returns positive infinity.

**Exceptions**

Does not throw exceptions.

###`double API.GetSignalDelayToKSC(Guid id)`

This function measures the signal delay experienced by a vessel were commands to be sent from a ground station. Despite the name, if the game has multiple ground stations then any of them will qualify.

**Parameters**

`id`
: The pid value of the vessel whose signal delay is desired.

**Return Value**

If [`HasConnectionToKSC(id)`](#bool-apihasconnectiontokscguid-id), the number of seconds of signal delay introduced. Otherwise (including nonexistent vessels), returns positive infinity.

**Exceptions**

Does not throw exceptions.

###`double API.GetSignalDelayToSatellite(Guid a, Guid b)`

This function measures the signal delay between two arbitrary vessels or ground stations.

**Parameters**

`a`, `b`
: The pid values of the vessels or ground stations whose signal delay is desired.

**Return Value**

If a connection is possible between the two vessels or ground stations, the number of seconds of signal delay introduced. Otherwise (including if one of the Guids is neither a vessel nor a ground station), returns positive infinity.

**Exceptions**

Does not throw exceptions.

##Autopilot Support

###`void API.AddSanctionedPilot(Guid id, Action<FlightCtrlState> autopilot)`

This function allows an external function to bypass signal delay and apply control input to a vessel in real time.

**Parameters**

`id`
: The pid value of the vessel being controlled

`autopilot`
: A function that takes a `FlightCtrlState` as an argument and updates it according to a piloting algorithm.

**Precondition**

**PostCondition**

All flight computers on board vessel `id` will allow `autopilot` real-time control of the vessel. `autopilot` will be run *after* RemoteTech's flight computer has made its own changes to a vessel's controls.

No effect if `id` is not a vessel.

**Exceptions**

Does not throw exceptions.

###`void API.RemoveSanctionedPilot(Guid id, Action<FlightCtrlState> autopilot)`

This function revokes the signal delay bypass granted by [`AddSanctionedPilot()`](#void-apiaddsanctionedpilotguid-id-actionflightctrlstate-autopilot).

**Parameters**

`id`
: The pid value of the vessel to control

`autopilot`
: A function that takes a `FlightCtrlState` as an argument and updates it according to a piloting algorithm.

**Precondition**

**PostCondition**

No flight computer on board vessel `id` will allow `autopilot` real-time control of the vessel.

No effect if `id` is not a vessel.

**Exceptions**

Does not throw exceptions.

