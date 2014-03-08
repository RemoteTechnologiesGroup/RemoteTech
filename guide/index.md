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

All stock probe cores serve as [signal processors](../#signal_processors). In addition, the RC-L01 Remote Guidance Unit can serve as a [command station](../#command_stations), provided a crew of 6 or more kerbals is available to split the jobs of running the ship and sending instructions to nearby probes.

###Omnidirectional Antennas

{::comment}
Yes, the non-breaking spaces are necessary. Without them, when printing the table on a narrow screen, browsers won't be smart enough to realize that notes is the only column that word-wraps well, and will try to create eye-wrenching entries like 2500
km
{:/comment}

Part                | Cost | Mass            | Drag | Range          | Power Drain   | Notes
:-------------------|-----:|:----------------|------|---------------:|:--------------|:------
[Reflectron DP-10](#reflectron-dp-10) | 80   | 0.005&nbsp;tons | 0.2  |    500&nbsp;km | 0.01&nbsp;e/s | Activated on mission start. Not damaged by atmospheric flight
[Communotron 16](#communotron-16) | 150  | 0.005&nbsp;tons | 0.2  |   2500&nbsp;km | 0.13&nbsp;e/s | 
[CommTech EXP-VR-2T](#commtech-exp-vr-2t) | 550  | 0.02&nbsp;tons  | 0.0  |   3000&nbsp;km | 0.18&nbsp;e/s | 
[Communotron 32](#communotron-32) | 150  | 0.01&nbsp;tons  | 0.2  |   5000&nbsp;km | 0.6&nbsp;e/s  | 
KSC Mission Control |      |                 |      | 75,000&nbsp;km |               | Command Station

All science transmissions with stock or RemoteTech antennas cost 7.5 charge per Mit, and they all drain 50 charge per second while transmitting science. This is in addition to the power drain listed in the table, which is for keeping the antenna active and searching for links.

####Reflectron DP-10

The Reflectron DP-10 is a lightweight omnidirectional antenna. Its omnidirectional nature and its ability to function in atmosphere even at high speeds make it an excellent choice for launches and landings, but its short range means it rapidly becomes useless outside low Kerbin orbit. Unlike other antennas, the DP-10 is active by default, although this state can be toggled in the antenna's right-click menu.

> When moving this antenna to a test site, engineers always forgot to turn it on. Frustrated with having to walk back and forth, they had this antenna be active by default.

![Picture of Reflectron DP-10](antenna_dp10.png)
VAB Category: Science Parts
Tech to Unlock: [Flight Control](http://wiki.kerbalspaceprogram.com/wiki/Flight_Control)
Manufacturer: Parabolic Industries
Cost: 80
Mass: 0.005 tons
Drag: 0.2
Comlink power: 0.01 charge/s
Science power: 50 charge/s
Science efficiency: 7.5 charge/Mit

**Transmission Properties**
Maximum Range: 500 km
Reach: Any unbroken line of sight to KSC Mission Control, if below 150 km altitude

**Atmosphere Performance**
Does not break in atmospheric flight.

####Communotron 16

As in the stock game, the Communotron 16 is the starting omnidirectional antenna, essential for transmitting science from those early flights. It also forms the backbown of most player's low-orbit communications networks until the CommTech EXP-VR-2T and Communotron 32 are researched.

![Picture of Communotron 16](antenna_com16.png)
VAB Category: Science Parts
Tech to Unlock: [None](http://wiki.kerbalspaceprogram.com/wiki/Start)
Manufacturer: Ionic Protonic Electronics
Cost: 150
Mass: 0.005 tons
Drag: 0.2
Comlink power: 0.13 charge/s
Science power: 50 charge/s
Science efficiency: 7.5 charge/Mit

**Transmission Properties**
Maximum Range: 2500 km
Reach: Low Kerbin Orbit

**Atmosphere Performance**
Maximum ram pressure when deployed: 6 kN/m<sup>2</sup>
Maximum safe speed at sea level: 99 m/s
Maximum safe speed at 10 km: 269 m/s
Minimum safe altitude at 2300 m/s: 32.5 km

####CommTech EXP-VR-2T

The CommTech EXP-VR-2T is an advanced antenna unlocked late in the tech tree. It is mounted on an extendable boom, making it much more compact than the Communotron series when retracted, but slightly larger when deployed. It is slightly more powerful than the Communotron 16.

> This effective and compact folding antenna is highly recommended for your research missions.

![Picture of EXP-VR-2T](antenna_expvr2t.png)
VAB Category: Science Parts
Tech to Unlock: [Specialized Electrics](http://wiki.kerbalspaceprogram.com/wiki/Tech_tree#Specialized_Electrics)
Manufacturer: AIES Aerospace
Cost: 150
Mass: 0.005 tons
Drag: 0.2
Comlink power: 0.13 charge/s
Science power: 50 charge/s
Science efficiency: 7.5 charge/Mit

**Transmission Properties**
Maximum Range: 3000 km
Reach: Low Kerbin Orbit

**Atmosphere Performance**
Maximum ram pressure when deployed: 6 kN/m<sup>2</sup>
Maximum safe speed at sea level: 99 m/s
Maximum safe speed at 10 km: 269 m/s
Minimum safe altitude at 2300 m/s: 32.5 km

####Communotron 32

The Communotron 32 is the the most powerful omnidirectional antenna available in RemoteTech 2, capable of reaching past kerbosynchonous orbit and filling many moons' spheres of influence. However, it consumes a lot of energy when active.

> The Communotron 32 is a longer range version of the last generation, now with even more spying potential. If you don't believe us, ask the Kerbal Security Agency.

![Picture of Communotron 32](antenna_com32.png)
VAB Category: Science Parts
Tech to Unlock: [Large Electrics](http://wiki.kerbalspaceprogram.com/wiki/Tech_tree#Large_Electrics)
Manufacturer: Ionic Protonic Electronics
Cost: 150
Mass: 0.01 tons
Drag: 0.2
Comlink power: 0.6 charge/s
Science power: 50 charge/s
Science efficiency: 7.5 charge/Mit

**Transmission Properties**
Maximum Range: 5000 km
Reach: Near-Kerbin space, synchronous orbit

**Atmosphere Performance**
Maximum ram pressure when deployed: 3 kN/m<sup>2</sup>
Maximum safe speed at sea level: 70 m/s
Maximum safe speed at 10 km: 190 m/s
Minimum safe altitude at 2300 m/s: 34.9 km

###Dish Antennas

{::comment}
Yes, the non-breaking spaces are necessary. Without them, when printing the table on a narrow screen, browsers won't be smart enough to realize that notes is the only column that word-wraps well, and will try to create eye-wrenching entries like 2500
km
{:/comment}

Antenna           | Cost | Mass            | Drag | Cone Angle | Range          | Power Drain   | Notes
:-----------------|-----:|:----------------|------|:-----------|---------------:|:--------------|:------
[Comms DTS-M1](#comms-dts-m1) | 100  | 0.03&nbsp;tons  | 0.2  | 45&deg;    | 50,000&nbsp;km | 0.82&nbsp;e/s | 
[Reflectron KR-7](#reflectron-kr-7) | 100  | 0.5&nbsp;tons   | 0.2  | 25&deg;    | 90,000&nbsp;km | 0.82&nbsp;e/s | Not damaged by atmospheric flight
[Communotron 88-88](#communotron-88-88) | 900  | 0.025&nbsp;tons | 0.2  | 0.06&deg;  | 40M&nbsp;km    | 0.93&nbsp;e/s | 
[Reflectron KR-14](#reflectron-kr-14) | 100  | 1.0&nbsp;tons   | 0.2  | 0.04&deg;  | 60M&nbsp;km    | 0.93&nbsp;e/s | Not damaged by atmospheric flight
[CommTech-1](#commtech-1) | 800  | 1.0&nbsp;tons   | 0.2  | 0.006&deg; | 350M&nbsp;km   | 2.6&nbsp;e/s  | Not damaged by atmospheric flight
[Reflectron GX-128](#reflectron-gx-128) | 800  | 0.5&nbsp;tons   | 0.2  | 0.005&deg; | 400M&nbsp;km   | 2.8&nbsp;e/s  | 

All science transmissions with stock or RemoteTech antennas cost 7.5 charge per Mit, and they all drain 50 charge per second while transmitting science. This is in addition to the power drain listed in the table, which is for keeping the antenna active and searching for links.

####Comms DTS-M1

The Comms DTS-M1 is the shortest-ranged of the directional dishes. Its wide beam makes it perfect for maintaining contact with multiple satellites within Kerbin's sphere of influence.

![Picture of Comms DTS-M1](antenna_dtsm1.png)
VAB Category: Science Parts
Tech to Unlock: [Science Tech](http://wiki.kerbalspaceprogram.com/wiki/Science_Tech)
Manufacturer: Ionic Symphonic Protonic Electronics
Cost: 100
Mass: 0.03 tons
Drag: 0.2
Comlink power: 0.82 charge/s
Science power: 50 charge/s
Science efficiency: 7.5 charge/Mit

**Transmission Properties**
Maximum Range: 50,000 km
Cone Angle: 45&deg;
Cone covers Kerbin at: 1600 km
Cone covers kerbostationary orbit at: 9100 km
Reach: Minmus orbit

**Atmosphere Performance**
Maximum ram pressure when deployed: 6 kN/m<sup>2</sup>
Maximum safe speed at sea level: 99 m/s
Maximum safe speed at 10 km: 269 m/s
Minimum safe altitude at 2300 m/s: 31.5 km

####Reflectron KR-7

The Reflectron KR-7 is the second short-range antenna available from RemoteTech 2. It has a longer range than the Comms DTS-M1, making it well-suited for spacecraft beyond Minmus's orbit. However, its narrow cone reduces its effectiveness at the Mun's distance or closer. The Reflectron KR-7 is too sturdy to be ripped off by atmospheric flight, so if properly targeted it can replace the Reflectron DP-10 as a launch antenna.

![Picture of Reflectron KR-7](antenna_refl7.png)
VAB Category: Science Parts
Tech to Unlock: [Electrics](http://wiki.kerbalspaceprogram.com/wiki/Tech_tree#Electrics)
Manufacturer: Parabolic Industries
Cost: 100
Mass: 0.5 tons
Drag: 0.2
Comlink power: 0.82 charge/s
Science power: 50 charge/s
Science efficiency: 7.5 charge/Mit

**Transmission Properties**
Maximum Range: 90,000 km
Cone Angle: 25&deg;
Cone covers Kerbin at: 2800 km
Cone covers kerbostationary orbit at: 16,000 km
Reach: Kerbin sphere of influence

**Atmosphere Performance**
Does not break in atmospheric flight.

####Communotron 88-88

The Communotron 88-88 is by far the lightest interplanetary antenna. It can easily reach all the inner planets, and can even contact Dres when it is on the same side of the sun as Kerbin. However, its narrow cone means that players will have to point it at a specific satellite if they wish to make course corrections while en route to Eve or Duna.

![Picture of Communotron 88-88](antenna_com88-88.png)
VAB Category: Science Parts
Tech to Unlock: [Electronics](http://wiki.kerbalspaceprogram.com/wiki/Electronics)
Manufacturer: Ionic Protonic Electronics
Cost: 1100
Mass: 0.025 tons
Drag: 0.2
Comlink power: 0.93 charge/s
Science power: 50 charge/s
Science efficiency: 7.5 charge/Mit

**Transmission Properties**
Maximum Range: 40,000,000 km
Cone Angle: 0.06&deg;
Cone covers Kerbin at: 1,100,000 km
Cone covers kerbostationary orbit at: 6,600,000 km
Reach: Duna (all times), Dres (same side of sun only)

**Atmosphere Performance**
Maximum ram pressure when deployed: 6 kN/m<sup>2</sup>
Maximum safe speed at sea level: 99 m/s
Maximum safe speed at 10 km: 269 m/s
Minimum safe altitude at 2300 m/s: 31.5 km

####Reflectron KR-14

The Reflectron KR-14 is an intermediate-range interplanetary antenna. It can easily reach all the inner planets as well as Dres. Just like the Communotron-88, the KR-14 has a narrow cone and will have difficulty "seeing" communications satellites if pointed directly at Kerbin from too close a range.

![Picture of Reflectron KR-14](antenna_refl14.png)
VAB Category: Science Parts
Tech to Unlock: [Large Electrics](http://wiki.kerbalspaceprogram.com/wiki/Tech_tree#Large_Electrics)
Manufacturer: Parabolic Industries
Cost: 100
Mass: 1.0 tons
Drag: 0.2
Comlink power: 0.93 charge/s
Science power: 50 charge/s
Science efficiency: 7.5 charge/Mit

**Transmission Properties**
Maximum Range: 60,000,000 km
Cone Angle: 0.04&deg;
Cone covers Kerbin at: 1,700,000 km
Cone covers kerbostationary orbit at: 9,900,000 km
Reach: Dres (all times), Jool (same side of sun only), Eeloo (periapsis and same side of sun only)

**Atmosphere Performance**
Does not break in atmospheric flight.

####CommTech-1

The CommTech-1 is the first antenna capable of returning signals to Kerbin from the outer solar system. Despite the in-game description, it can reach any planet, even Eeloo at apoapsis. However, it has an extremely narrow cone; players should avoid using the dish in cone mode until they pass the orbit of Dres. Even a satellite in orbit around Jool may have occasional connection problems when using cone mode, as it can approach within 52 million km of Kerbin.

> A powerful high-gain fixed dish. It can reach almost anything in the solar system.

![Picture of CommTech-1](antenna_ct1.png)
VAB Category: Science Parts
Tech to Unlock: [Specialized Electrics](http://wiki.kerbalspaceprogram.com/wiki/Tech_tree#Specialized_Electrics)
Manufacturer: AIES Aerospace
Cost: 800
Mass: 1.0 tons
Drag: 0.2
Comlink power: 2.60 charge/s
Science power: 50 charge/s
Science efficiency: 7.5 charge/Mit

**Transmission Properties**
Maximum Range: 350,000,000 km
Cone Angle: 0.006&deg;
Cone covers Kerbin at: 11,000,000 km
Cone covers kerbostationary orbit at: 66,000,000 km
Reach: Eeloo (all times)

**Atmosphere Performance**
Does not break in atmospheric flight.

####Reflectron GX-128

The Reflecton-GX-128 is the longest-range antenna available in RemoteTech 2. While it has, for all practical purposes, the same range as the CommTech-1, its foldable construction makes it much lighter.

> A massive medium-interplanetary class dish. Wherever you are in the Kerbol system, you'll be able to stay in contact with this.

![Picture of CommTech-1](antenna_ct1.png)
VAB Category: Science Parts
Tech to Unlock: [Advanced Science Tech](http://wiki.kerbalspaceprogram.com/wiki/Tech_tree#Advanced_Science_Tech)
Manufacturer: Parabolic Industries
Cost: 800
Mass: 0.5 tons
Drag: 0.2
Comlink power: 2.80 charge/s
Science power: 50 charge/s
Science efficiency: 7.5 charge/Mit

**Transmission Properties**
Maximum Range: 400,000,000 km
Cone Angle: 0.005&deg;
Cone covers Kerbin at: 14,000,000 km
Cone covers kerbostationary orbit at: 79,000,000 km
Reach: Eeloo (all times)

**Atmosphere Performance**
Maximum ram pressure when deployed: 6 kN/m<sup>2</sup>
Maximum safe speed at sea level: 99 m/s
Maximum safe speed at 10 km: 269 m/s
Minimum safe altitude at 2300 m/s: 31.5 km

##Modding Parts to Work With RemoteTech

