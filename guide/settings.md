---
title: Settings
layout: content
navbar: false
---
 
{% include banner.html %}

**UNDER CONSTRUCTION:** Please watch your step!
{: .alert .alert-danger}

#RemoteTech Options

{% include toc.html %}

RemoteTech has several configurable options controlled from the file `RemoteTech2/RemoteTech_Settings.cfg`. This file is not included in the RemoteTech download, to keep players from overwriting their settings when they update to a new version. If you do not have a `RemoteTech_Settings.cfg` file, a new one will be automatically created with default settings when you start Kerbal Space Program.

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


`MultipleAntennaMultiplier` (default = 0.0)
: This setting lets multiple omnidirectional antennas on the same craft act as a single, slightly larger antenna. The default value of 0.0 means that omni antennas do not boost each other; a value of 1.0 means that the effective range of the satellite equals the total range of all omni antennas on board. The effective range scales linearly between these two extremes. This option works with both the Standard and Root range models.

## Visual Style

`DishConnectionColor` (RGB&alpha; quadruplet) (default = Amber, fully opaque)
: The color in which links with at least one dish will be drawn on the map view and tracking station

`OmniConnectionColor` (RGB&alpha; quadruplet) (default = Brown-Grey, fully opaque)
: The color in which links between two omni antennas will be drawn on the map view and tracking station

`ActiveConnectionColor` (RGB&alpha; quadruplet) (default = Electric Lime, fully opaque)
: The color in which the working connection to mission control will be drawn on the map view and tracking station

## Miscellaneous

`ThrottleTimeWarp` (default = True)
: If set, the flight computer will automatically come out of time warp a few seconds before executing a queued command. If unset, the player is responsible for making sure the craft is not in time warp during scheduled actions.

##Appendix: Root Range Model

In the root range model, the minimum distance to achieve a link depends on the ranges of both antennas. The following table shows the ranges for each pair of RemoteTech antennas, assuming `RangeMultiplier = 0.5`.

&nbsp;             | Reflectron GX-128 | CommTech-1      | Reflectron KR-14 | Communotron 88-88 | Reflectron KR-7 | Comms DTS-M1   | Mission Control | Communotron 32 | CommTech EXP-VR-2T | Communotron 16 | Reflectron DP-10
:------------------|------------------:|----------------:|-----------------:|------------------:|----------------:|---------------:|----------------:|---------------:|-------------------:|---------------:|-----------------:
Reflectron DP-10   |    25,000&nbsp;km |  25,000&nbsp;km |   25,000&nbsp;km |    25,000&nbsp;km |    3600&nbsp;km |   2800&nbsp;km |    3300&nbsp;km |   1000&nbsp;km |        860&nbsp;km |    810&nbsp;km |      500&nbsp;km
Communotron 16     |   125,000&nbsp;km | 125,000&nbsp;km |  125,000&nbsp;km |   125,000&nbsp;km |    8800&nbsp;km |   6800&nbsp;km |    8100&nbsp;km |   3000&nbsp;km |       2600&nbsp;km |   2500&nbsp;km
CommTech EXP-VR-2T |   150,000&nbsp;km | 150,000&nbsp;km |  150,000&nbsp;km |   150,000&nbsp;km |    9700&nbsp;km |   7600&nbsp;km |    9000&nbsp;km |   3400&nbsp;km |       3000&nbsp;km
Communotron 32     |   250,000&nbsp;km | 250,000&nbsp;km |  250,000&nbsp;km |   230,000&nbsp;km |  13,000&nbsp;km | 10,000&nbsp;km |  12,000&nbsp;km |   5000&nbsp;km
Mission Control    |      2.8M&nbsp;km |    2.6M&nbsp;km |     1.1M&nbsp;km |   900,000&nbsp;km |  79,000&nbsp;km | 56,000&nbsp;km
Comms DTS-M1       |      2.3M&nbsp;km |    2.1M&nbsp;km |     0.9M&nbsp;km |   730,000&nbsp;km |  59,000&nbsp;km | 50,000&nbsp;km
Reflectron KR-7    |      3.0M&nbsp;km |    2.9M&nbsp;km |     1.2M&nbsp;km |   990,000&nbsp;km |  90,000&nbsp;km
Communotron 88-88  |     83M&nbsp;km   |   79M&nbsp;km   |    44M&nbsp;km   |       40M&nbsp;km
Reflectron KR-14   |    110M&nbsp;km   |  100M&nbsp;km   |    60M
CommTech-1         |    360M&nbsp;km   |  350M&nbsp;km
Reflectron GX-128  |    400M&nbsp;km
{:.data .shadecol .sidehead}
