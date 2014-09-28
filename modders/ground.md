---
title: Adding Ground Stations
layout: content
navbar: false
---
 
{% include banner.html %}

# Adding Ground Stations

The `GroundStations` section of `RemoteTech2/RemoteTech_Settings.cfg` (which will be created when you start KSP) controls the number, range, and placement of satellite uplink stations. Each `STATION{}` block represents a station. By default, there is only one ground station, coinciding with the KSC Tracking Station.

Each `STATION{}` block needs the following fields:

`Guid`
: A unique idenfier for the station. **Must** be unique, or your network will exhibit undefined behavior.

`Name`
: The name that shows up in the target selection menu.

`Latitude`, `Longitude`
: The position of the station on the planet's surface in degrees north and degrees east, respectively.

`Height`
: The station's altitude above sea level, in meters.

`Body`
: The internal ID number of the planet on which the station is located. You can find a list of ID numbers for stock Kerbal Space Program [here](https://github.com/Anatid/XML-Documentation-for-the-KSP-API/blob/master/src/FlightGlobals.cs#L72). If you are playing with Real Solar System or other planet packs, edit this value with caution.

`Antennas`
: This block should contain a single `ANTENNA` block, which itself should contain a single `Omni` field. The value of the field is the antenna range in meters.
