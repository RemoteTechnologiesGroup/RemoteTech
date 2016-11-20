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

In the root range model, the minimum distance to achieve a link depends on the ranges of both antennas. The following table shows the ranges in **Mm** for each antenna pair, assuming `RangeMultiplier = 1.0`.

&nbsp; | DP-10 | 16-S | 16 | EXP-VR-2T | 32 | HG-5 | DTS-M1 | Mission<br/>Control | KR-7 | RA-2 | RA-15 | HG-55 | 88-88 | KR-14 | RA-100 | CT-1 | GX-128
---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---:
GX-128 | 447.7 | 776.1 | 1,002.5 | 1,098.4 | 1,419.2 | 2,848.4 | 4,522.1 | 5,552.2 | 6,090.0 | 9,144.3 | 73,245.6 | 125,000.0 | 166,491.1 | 214,919.3 | 300,000.0 | 724,165.7 | 800,000.0
CT-1 | 418.8 | 726.1 | 937.9 | 1,027.7 | 1,327.9 | 2,665.8 | 4,233.3 | 5,198.5 | 5,702.5 | 8,566.6 | 69,160.8 | 118,541.4 | 158,321.6 | 204,913.8 | 287,082.9 | 700,000.0
RA-100 | 224.1 | 388.8 | 502.5 | 550.7 | 712.1 | 1,434.2 | 2,286.1 | 2,813.6 | 3,090.0 | 4,672.1 | 41,622.8 | 75,000.0 | 103,245.6 | 137,459.7 | 200,000.0
KR-14 | 173.7 | 301.5 | 389.8 | 427.3 | 552.7 | 1,115.4 | 1,782.1 | 2,196.3 | 2,413.8 | 3,664.1 | 34,494.9 | 63,729.8 | 88,989.8 | 120,000.0
88-88 | 141.9 | 246.4 | 318.7 | 349.4 | 452.2 | 914.4 | 1,464.2 | 1,807.1 | 1,987.4 | 3,028.4 | 30,000.0 | 56,622.8 | 80,000.0
HG-55 | 112.3 | 195.1 | 252.5 | 276.9 | 358.6 | 727.1 | 1,168.0 | 1,444.3 | 1,590.0 | 2,436.1 | 25,811.4 | 50,000.0
RA-15 | 71.2 | 124.0 | 160.6 | 176.2 | 228.6 | 467.2 | 757.1 | 941.0 | 1,038.7 | 1,614.2 | 20,000.0
RA-2 | 10.5 | 18.8 | 24.9 | 27.5 | 36.6 | 83.2 | 150.0 | 197.5 | 224.2 | 400.0
KR-7 | 7.2 | 13.1 | 17.5 | 19.4 | 26.2 | 62.4 | 117.1 | 157.2 | 180.0
Mission<br/>Control | 6.6 | 12.1 | 16.2 | 18.0 | 24.4 | 58.7 | 111.2 | 150.0
DTS-M1 | 5.5 | 10.2 | 13.7 | 15.2 | 20.8 | 51.6 | 100.0
HG-5 | 3.7 | 7.0 | 9.6 | 10.7 | 15.0 | 40.0
32 | 2.1 | 4.2 | 6.0 | 6.9 | 10.0
EXP-VR-2T | 1.7 | 3.6 | 5.2 | 6.0
16 | 1.6 | 3.4 | 5.0
16-S | 1.4 | 3.0
DP-10 | 1.0
{:.data .shadecol .sidehead}
