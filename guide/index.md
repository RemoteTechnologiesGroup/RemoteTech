---
title: Player's Guide
layout: content
navbar: true
---

{% include banner.html %}

**Oh shit son!** This page is still under development!
{: .alert .alert-danger}

{% include toc.html %}

##Playing RemoteTech

###Antenna Configuration

###The Map View

###The Flight Computer

##Connection Rules

###Line of Sight

###Range

###Targeting

##List of Parts

###Probe Cores

All stock probe cores serve as [signal processors](#signal_processors). In addition, the RC-L01 Remote Guidance Unit can serve as a [command station](#command_stations), provided a crew of 6 or more kerbals is available to split the jobs of running the ship and sending instructions to nearby probes.

###Omnidirectional Antennas

{::comment}
Yes, the non-breaking spaces are necessary; the table won't set column widths sensibly without them
{:/comment}

Part                | Cost | Mass            | Drag | Range          | Power Drain   | Notes
:-------------------|-----:|:----------------|------|---------------:|:--------------|:------
Reflectron DP-10    | 80   | 0.005 tons | 0.2  |    500 km | 0.01 e/s | Activated on mission start. Not damaged by atmospheric flight
Communotron 16      | 150  | 0.005 tons | 0.2  |   2500 km | 0.13 e/s | 
CommTech EXP-VR-2T  | 550  | 0.02 tons  | 0.0  |   3000 km | 0.18 e/s | 
Communotron 32      | 150  | 0.01 tons  | 0.2  |   5000 km | 0.6 e/s  | 
KSC Mission Control |      |                 |      | 75,000 km |               | Command Station

All science transmissions with stock or RemoteTech antennas cost 7.5 charge per Mit, and they all drain 50 charge per second while transmitting science. This is in addition to the power drain listed in the table, which is for keeping the antenna active and searching for links.

###Dish Antennas

<!--Yes, the non-breaking spaces are necessary; the table won't set column widths sensibly without them-->

Antenna           | Cost | Mass            | Drag | Cone Angle | Range          | Power Drain   | Notes
:-----------------|-----:|:----------------|------|:-----------|---------------:|:--------------|:------
Comms DTS-M1      | 100  | 0.3&nbsp;tons   | 0.2  | 45&deg;    | 50,000&nbsp;km | 0.82&nbsp;e/s | 
Reflectron KR-7   | 100  | 0.5&nbsp;tons   | 0.2  | 25&deg;    | 90,000&nbsp;km | 0.82&nbsp;e/s | Not damaged by atmospheric flight
Communotron 88-88 | 900  | 0.025&nbsp;tons | 0.2  | 0.06&deg;  | 40M&nbsp;km    | 0.93&nbsp;e/s | 
Reflectron KR-14  | 100  | 1.0&nbsp;tons   | 0.2  | 0.04&deg;  | 60M&nbsp;km    | 0.93&nbsp;e/s | Not damaged by atmospheric flight
CommTech-1        | 800  | 1.0&nbsp;tons   | 0.2  | 0.006&deg; | 350M&nbsp;km   | 2.6&nbsp;e/s  | Not damaged by atmospheric flight
Reflectron GX-128 | 800  | 0.5&nbsp;tons   | 0.2  | 0.005&deg; | 400M&nbsp;km   | 2.8&nbsp;e/s  | 

All science transmissions with stock or RemoteTech antennas cost 7.5 charge per Mit, and they all drain 50 charge per second while transmitting science. This is in addition to the power drain listed in the table, which is for keeping the antenna active and searching for links.

##Modding Parts to Work With RemoteTech

