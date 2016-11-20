---
title: Settings
layout: content
navbar: false
---
 
{% include banner.html %}

**UNDER CONSTRUCTION:** Please watch your step!
{: .alert .alert-danger}

# RemoteTech Options

{% include toc.html %}

RemoteTech has several configurable options controlled from the file `RemoteTech/RemoteTech_Settings.cfg`. This file is not included in the RemoteTech download, to keep players from overwriting their settings when they update to a new version. If you do not have a `RemoteTech_Settings.cfg` file, a new one will be automatically created with default settings when you start Kerbal Space Program.

The settings are as follows:

## World Scale

`ConsumptionMultiplier` (default = 1.0)
: If set to a value other than 1, the power consumption of all antennas will be increased or decreased by this factor. Does not affect energy consumption for science transmissions.

`RangeMultiplier` (default = 1.0)
: If set to a value other than 1, the range of all antennas will be increased or decreased by this factor. Does not affect Mission Control range; change that separately.

`SpeedOfLight` (default = 3&nbsp;&times;&nbsp;10<sup>8</sup>)
: The speed of light in meters per second. If `EnableSignalDelay` is set, the signal delay will equal the length of the communications path divided by this value. No effect if `EnableSignalDelay` is unset.

## Alternative Rules

`EnableSignalDelay` (default = True)
: If set, then all commands sent to RemoteTech-compatible probe cores will be delayed, depending on the distance to the probe and the `SpeedOfLight`. If unset, then all commands will be executed instantaneously, so long as there is a connection of any length between the probe and Mission Control.

`RangeModelType` (default = Standard)
: This setting controls how the game determines whether two antennas are in range of each other. The options are:

   `Standard`
   : The game works as described in the [Player's Guide](../../guide/overview/#range): a link is only possible if the distance between two ships is less than the *smaller* of the two antennas' ranges.

   `Root`
   : The two antennas can communicate as long as they are within ![Min(r1, r2) + Sqrt(r1 r2)](rootmodel.png) of each other, where r<sub>1</sub> and r<sub>2</sub> are the ranges of the two antennas, up to a limit of 100 times the omni range or 1000 times the dish range, whichever is smallest. A table of effective ranges for all pairs of antennas is given in an [appendix](#appendix-root-range-model). Since this formula doubles the effective range between two identical antennas, it is recommended to use `RangeMultiplier = 0.5` with this mode to preserve part balance.

   `Additive`
   : This is another name for `Root`, and works exactly the same way.


`MultipleAntennaMultiplier` (default = 0.0)
: This setting lets multiple omnidirectional antennas on the same craft act as a single, slightly larger antenna. The default value of 0.0 means that omni antennas do not boost each other; a value of 1.0 means that the effective range of the satellite equals the total range of all omni antennas on board. The effective range scales linearly between these two extremes. This option works with both the Standard and Root range models.

## Visual Style

`DishConnectionColor` (RGB&alpha; quadruplet) (default = Amber, fully opaque)
: The color in which links with at least one dish will be drawn on the map view and tracking station

`OmniConnectionColor` (RGB&alpha; quadruplet) (default = Brown-Grey, fully opaque)
: The color in which links between two omni antennas will be drawn on the map view and tracking station

`ActiveConnectionColor` (RGB&alpha; quadruplet) (default = Electric Lime, fully opaque)
: The color in which the working connection to mission control will be drawn on the map view and tracking station

`HideGroundStationsBehindBody` (default = False)
: If true, ground stations occulued by the body they're on will not be displayed. This prevents ground stations on the other side of the planet being visible through the planet itself.

## Miscellaneous

`ThrottleTimeWarp` (default = True)
: If set, the flight computer will automatically come out of time warp a few seconds before executing a queued command. If unset, the player is responsible for making sure the craft is not in time warp during scheduled actions.

`ThrottleZeroOnNoConnection` (default = True)
: If true, the flight computer cuts the thrust if you have no connection to mission control.

## Appendix: Root Range Model

In the root range model, the minimum distance to achieve a link depends on the ranges of both antennas. The following table shows the ranges for each antenna pair, assuming `RangeMultiplier = 1.0`.

&nbsp; | DP-10 | 16-S | 16 | EXP-VR-2T | 32 | HG-5 | DTS-M1 | Mission<br/>Control | KR-7 | RA-2 | RA-15 | HG-55 | 88-88 | KR-14 | RA-100 | CT-1 | GX-128
---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---:
GX-128 | 
CT-1 | 
RA-100 | 
KR-14 |
88-88 | 
HG-55 |
RA-15 | 
RA-2 | 
KR-7 | 
Mission<br/>Control | 
DTS-M1 | 
32 | 
EXP-VR-2T | 
16 | 
16-S | 
DP-10 | 
{:.data .shadecol .sidehead}
