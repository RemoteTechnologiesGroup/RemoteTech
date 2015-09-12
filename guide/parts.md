---
title: RemoteTech List of Parts
layout: content
custom_css: true
extra_css: antennas
---

{% include banner.html %}

# List of Parts

{% include toc.html %}

## Probe Cores

All stock, B9, and FASA probe cores serve as [signal processors](../../#signal-processors). In addition, the [RC-L01 Remote Guidance Unit](http://wiki.kerbalspaceprogram.com/wiki/RC-L01_Remote_Guidance_Unit) can serve as a [command station](../../#command-stations), provided a crew of 6 or more kerbals is available to split the jobs of running the ship and monitoring nearby probes. The crew can be anywhere on the ship; it does not have to be in a particular part. The RC-L01 still acts as a probe core and a signal processor, whether or not a crew is on board.

After the player develops [Unmanned Tech](http://wiki.kerbalspaceprogram.com/wiki/Unmanned_Tech), all supported probe cores will receive a free, always-on, 3&nbsp;km omnidirectional antenna.

The probe cores are otherwise unchanged from their [stock versions](http://wiki.kerbalspaceprogram.com/wiki/Parts#Pods).

## Omnidirectional Antennas

{::comment}
Yes, the non-breaking spaces are necessary. Without them, when printing the table on a narrow screen, browsers won't be smart enough to realize that notes is the only column that word-wraps well, and will try to create eye-wrenching entries like 2500
km
{:/comment}

Part                | Cost | Mass            | Drag | Range          | Power Drain   | Notes
:-------------------|-----:|:----------------|------|---------------:|:--------------|:------
[Reflectron DP-10](#reflectron-dp-10) | 60   | 0.005&nbsp;tons | 0.2  |    500&nbsp;km | 0.01&nbsp;e/s | Activated on mission start. Not damaged by atmospheric flight
[Communotron 16](#communotron-16) | 300  | 0.005&nbsp;tons | 0.2  |   2500&nbsp;km | 0.13&nbsp;e/s | 
[CommTech EXP-VR-2T](#commtech-exp-vr-2t) | 400  | 0.02&nbsp;tons  | 0.0  |   3000&nbsp;km | 0.18&nbsp;e/s | 
[Communotron 32](#communotron-32) | 600  | 0.01&nbsp;tons  | 0.2  |   5000&nbsp;km | 0.6&nbsp;e/s  | 
KSC Mission Control |      |                 |      | 75,000&nbsp;km |               | Command Station
{:.data}

<!--All science transmissions with stock or RemoteTech antennas cost 7.5 charge per Mit, and they all drain 50 charge per second while transmitting science. This is in addition to the power drain listed in the table, which is for keeping the antenna active and searching for links.-->

### Reflectron DP-10

<div class="antenna" markdown="1">

![Picture of Reflectron DP-10](antenna_dp10_B.png)

The Reflectron DP-10 is a lightweight omnidirectional antenna. Its omnidirectional nature and its ability to function in atmosphere even at high speeds make it an excellent choice for launches and landings, but its short range means it rapidly becomes useless outside low Kerbin orbit. Unlike other antennas, the DP-10 is active by default, although this state can be toggled in the antenna's right-click menu.
{:.blurb}

|Basic Properties
--------------------|-------------------
Tech to Unlock      | [Flight Control](http://wiki.kerbalspaceprogram.com/wiki/Flight_Control)
VAB Category        | Science Parts
Manufacturer        | Parabolic Industries
Cost                | 60
Mass                | 0.005 tons
Length              | 1.375 m
Drag                | 0.2
Comlink power       | 0.01 charge/s
Science power       | 50 charge/s
Science efficiency  | 7.5 charge/Mit
{:.basic}

|Transmission Properties
--------------------------------------|-------------------
Maximum Range                         | 500 km
Reach                                 | Any line of sight to KSC Mission Control, if below 150 km altitude
{:.xmit}

|Atmosphere Performance
|------------------------------------
|Does not break in atmospheric flight.
{:.atm}

---------------

</div>

### Communotron 16

<div class="antenna" markdown="1">

![Picture of Communotron 16](antenna_com16.png)

As in the stock game, the Communotron 16 is the starting omnidirectional antenna, essential for transmitting science from those early flights. It also forms the backbone of most players' low-orbit communications networks until the CommTech EXP-VR-2T and Communotron 32 are researched.
{:.blurb}

|Basic Properties
--------------------|-------------------
Tech to Unlock      | [None](http://wiki.kerbalspaceprogram.com/wiki/Start)
VAB Category        | Science Parts
Manufacturer        | Ionic Protonic Electronics
Cost                | 300
Mass                | 0.005 tons
Length              | 1.5 m
Drag                | 0.2
Comlink power       | 0.13 charge/s
Science power       | 50 charge/s
Science efficiency  | 7.5 charge/Mit
{:.basic}

|Transmission Properties
--------------------------------------|-------------------
Maximum Range                         | 2500 km
Reach                                 | Low Kerbin Orbit
{:.xmit}

|Atmosphere Performance
------------------------------------|-------------------
Maximum ram pressure when deployed  | 6 kN/m<sup>2</sup>
Maximum safe speed at sea level     | 99 m/s
Maximum safe speed at 10 km         | 269 m/s
Minimum safe altitude at 2300 m/s   | 32 km
{:.atm}

---------------

</div>

### CommTech EXP-VR-2T

<div class="antenna" markdown="1">

![Picture of EXP-VR-2T](antenna_expvr2t.png)

The CommTech EXP-VR-2T is an advanced antenna unlocked late in the tech tree. It is mounted on an extendable boom, making it much more compact than the Communotron 16 when retracted, but somewhat larger when deployed. It is slightly more powerful than the Communotron 16.
{:.blurb}

|Basic Properties
--------------------|-------------------
Tech to Unlock      | [Specialized Electrics](http://wiki.kerbalspaceprogram.com/wiki/Specialized_Electrics)
VAB Category        | Science Parts
Manufacturer        | AIES Aerospace
Cost                | 400
Mass                | 0.005 tons
Length              | 2 m
Drag                | 0.2
Comlink power       | 0.13 charge/s
Science power       | 50 charge/s
Science efficiency  | 7.5 charge/Mit
{:.basic}

|Transmission Properties
--------------------------------------|-------------------
Maximum Range                         | 3000 km
Reach                                 | Low Kerbin Orbit
{:.xmit}

|Atmosphere Performance
------------------------------------|-------------------
Maximum ram pressure when deployed  | 6 kN/m<sup>2</sup>
Maximum safe speed at sea level     | 99 m/s
Maximum safe speed at 10 km         | 269 m/s
Minimum safe altitude at 2300 m/s   | 32 km
{:.atm}

---------------

</div>

### Communotron 32

<div class="antenna" markdown="1">

![Picture of Communotron 32](antenna_com32.png)

The Communotron 32 is the most powerful omnidirectional antenna available in RemoteTech, capable of reaching past keosynchronous orbit and filling many moons' spheres of influence. However, it consumes a lot of energy when active, nearly as much as the low-end dishes.
{:.blurb}

|Basic Properties
--------------------|-------------------
Tech to Unlock      | [Large Electrics](http://wiki.kerbalspaceprogram.com/wiki/Large_Electrics)
VAB Category        | Science Parts
Manufacturer        | Ionic Protonic Electronics
Cost                | 600
Mass                | 0.01 tons
Length              | 3 m
Drag                | 0.2
Comlink power       | 0.6 charge/s
Science power       | 50 charge/s
Science efficiency  | 7.5 charge/Mit
{:.basic}

|Transmission Properties
--------------------------------------|-------------------
Maximum Range                         | 5000 km
Reach                                 | Near-Kerbin space, synchronous orbit
{:.xmit}

|Atmosphere Performance
------------------------------------|-------------------
Maximum ram pressure when deployed  | 3 kN/m<sup>2</sup>
Maximum safe speed at sea level     | 70 m/s
Maximum safe speed at 10 km         | 190 m/s
Minimum safe altitude at 2300 m/s   | 35 km
{:.atm}

---------------

</div>

## Dish Antennas

{::comment}
Yes, the non-breaking spaces are necessary. Without them, when printing the table on a narrow screen, browsers won't be smart enough to realize that notes is the only column that word-wraps well, and will try to create eye-wrenching entries like 2500
km
{:/comment}

Antenna           | Cost | Mass            | Drag | Cone | Range          | Power Drain   | Notes
:-----------------|-----:|:----------------|------|:-----------|---------------:|:--------------|:------
[Comms DTS-M1](#comms-dts-m1) | 600  | 0.03&nbsp;tons  | 0.2  | 45&deg;    | 50,000&nbsp;km | 0.82&nbsp;e/s | 
[Reflectron KR-7](#reflectron-kr-7) | 800  | 0.5&nbsp;tons   | 0.2  | 25&deg;    | 90,000&nbsp;km | 0.82&nbsp;e/s | Not damaged by atmospheric flight
[Communotron 88-88](#communotron-88-88) | 1100  | 0.025&nbsp;tons | 0.2  | 0.06&deg;  | 40M&nbsp;km    | 0.93&nbsp;e/s | 
[Reflectron KR-14](#reflectron-kr-14) | 2000  | 1.0&nbsp;tons   | 0.2  | 0.04&deg;  | 60M&nbsp;km    | 0.93&nbsp;e/s | Not damaged by atmospheric flight
[CommTech-1](#commtech-1) | 9500  | 1.0&nbsp;tons   | 0.2  | 0.006&deg; | 350M&nbsp;km   | 2.6&nbsp;e/s  | Not damaged by atmospheric flight
[Reflectron GX-128](#reflectron-gx-128) | 11000  | 0.5&nbsp;tons   | 0.2  | 0.005&deg; | 400M&nbsp;km   | 2.8&nbsp;e/s  | 
{:.data}

**Warning:** the Reflectron SS-5 and Reflectron LL-5 are legacy parts included for backward compatibility. Do not use these parts in new spacecraft, as they will be removed in an upcoming release of RemoteTech.
{: .alert}

### Comms DTS-M1

<div class="antenna" markdown="1">

![Picture of Comms DTS-M1](antenna_dtsm1_B.png)

The Comms DTS-M1 is the shortest-ranged of the directional dishes. Its wide cone makes it perfect for maintaining contact with multiple satellites within Kerbin's sphere of influence.
{:.blurb}

|Basic Properties
--------------------|-------------------
Tech to Unlock      | [Science Tech](http://wiki.kerbalspaceprogram.com/wiki/Science_Tech)
VAB Category        | Science Parts
Manufacturer        | Ionic Symphonic Protonic Electronics
Cost                | 600
Mass                | 0.03 tons
Dimensions          | 1 &times; 0.75  m
Drag                | 0.2
Comlink power       | 0.82 charge/s
Science power       | 50 charge/s
Science efficiency  | 7.5 charge/Mit
{:.basic}

|Transmission Properties
--------------------------------------|-------------------
Maximum Range                         | 50,000 km
Reach                                 | Minmus
Cone Diameter                         | 45&deg;
Cone covers Kerbin at                 | 1600 km
Cone covers keosynchronous orbit at | 9100 km
{:.xmit}

|Atmosphere Performance
------------------------------------|-------------------
Maximum ram pressure when deployed  | 6 kN/m<sup>2</sup>
Maximum safe speed at sea level     | 99 m/s
Maximum safe speed at 10 km         | 269 m/s
Minimum safe altitude at 2300 m/s   | 32 km
{:.atm}

---------------

</div>

### Reflectron KR-7

<div class="antenna" markdown="1">

![Picture of Reflectron KR-7](antenna_refl7.png)

The Reflectron KR-7 is the second short-range antenna available from RemoteTech. It has a longer range than the Comms DTS-M1, making it well-suited for spacecraft beyond Minmus's orbit. However, its narrow cone reduces its effectiveness at the Mun's distance or closer. The Reflectron KR-7 is too sturdy to be ripped off by atmospheric flight, so if properly targeted it can replace the Reflectron DP-10 as a launch antenna.
{:.blurb}

|Basic Properties
--------------------|-------------------
Tech to Unlock      | [Electrics](http://wiki.kerbalspaceprogram.com/wiki/Tech_tree#Electrics)
VAB Category        | Science Parts
Manufacturer        | Parabolic Industries
Cost                | 800
Mass                | 0.5 tons
Diameter            | 1.375 m
Drag                | 0.2
Comlink power       | 0.82 charge/s
Science power       | 50 charge/s
Science efficiency  | 7.5 charge/Mit
{:.basic}

|Transmission Properties
--------------------------------------|-------------------
Maximum Range                         | 90,000 km
Reach                                 | Kerbin sphere of influence
Cone Diameter                         | 25&deg;
Cone covers Kerbin at                 | 2800 km
Cone covers keosynchronous orbit at | 16,000 km
{:.xmit}

|Atmosphere Performance
|------------------------------------
|Does not break in atmospheric flight.
{:.atm}

---------------

</div>

### Communotron 88-88

<div class="antenna" markdown="1">

![Picture of Communotron 88-88](antenna_com88-88.png)

The Communotron 88-88 is by far the lightest interplanetary antenna. It can easily reach all the inner planets, and can even contact Dres when it is on the same side of the sun as Kerbin. However, its narrow cone means that players will have to point it at a specific satellite if they wish to make course corrections while en route to Eve or Duna.
{:.blurb}

|Basic Properties
--------------------|-------------------
Tech to Unlock      | [Electronics](http://wiki.kerbalspaceprogram.com/wiki/Electronics)
VAB Category        | Science Parts
Manufacturer        | Ionic Protonic Electronics
Cost                | 1100
Mass                | 0.025 tons
Diameter            | 2.375 m
Drag                | 0.2
Comlink power       | 0.93 charge/s
Science power       | 50 charge/s
Science efficiency  | 7.5 charge/Mit
{:.basic}

|Transmission Properties
--------------------------------------|-------------------
Maximum Range                         | 40,000,000 km
Reach                                 | Duna (all times), Dres (same side of sun only)
Cone Diameter                         | 0.06&deg;
Cone covers Kerbin at                 | 1,100,000 km
Cone covers keosynchronous orbit at | 6,600,000 km
{:.xmit}

|Atmosphere Performance
------------------------------------|-------------------
Maximum ram pressure when deployed  | 6 kN/m<sup>2</sup>
Maximum safe speed at sea level     | 99 m/s
Maximum safe speed at 10 km         | 269 m/s
Minimum safe altitude at 2300 m/s   | 32 km
{:.atm}

---------------

</div>

### Reflectron KR-14

<div class="antenna" markdown="1">

![Picture of Reflectron KR-14](antenna_refl14.png)

The Reflectron KR-14 is an intermediate-range interplanetary antenna. It can easily reach all the inner planets as well as Dres. Like the Communotron-88, the KR-14 has a narrow cone and will have difficulty "seeing" communications satellites if pointed directly at Kerbin from too close a range.
{:.blurb}

|Basic Properties
--------------------|-------------------
Tech to Unlock      | [Large Electrics](http://wiki.kerbalspaceprogram.com/wiki/Large_Electrics)
VAB Category        | Science Parts
Manufacturer        | Parabolic Industries
Cost                | 2200
Mass                | 1.0 tons
Diameter            | 2.75 m
Drag                | 0.2
Comlink power       | 0.93 charge/s
Science power       | 50 charge/s
Science efficiency  | 7.5 charge/Mit
{:.basic}

|Transmission Properties
--------------------------------------|-------------------
Maximum Range                         | 60,000,000 km
Reach                                 | Dres (all times), Jool (same side of sun only), Eeloo (periapsis and same side of sun only)
Cone Diameter                         | 0.04&deg;
Cone covers Kerbin at                 | 1,700,000 km
Cone covers keosynchronous orbit at | 9,900,000 km
{:.xmit}

|Atmosphere Performance
|------------------------------------
|Does not break in atmospheric flight.
{:.atm}

---------------

</div>

### CommTech-1

<div class="antenna" markdown="1">

![Picture of CommTech-1](antenna_ct1.png)

The CommTech-1 is the first antenna capable of returning signals to Kerbin from the outer solar system. Despite the in-game description, it can reach any planet available in version 0.23.5 of the game, even Eeloo at apoapsis. However, it has an extremely narrow cone; players should avoid using the dish in cone mode until they pass the orbit of Dres. Even a satellite in orbit around Jool may have occasional connection problems when using cone mode, as it can approach within 52 million km of Kerbin.

For players using Planet Factory, the CommTech-1 can reach Inaccessable and Sentar, but not Serious, Stella, or Barry.
{:.blurb}

|Basic Properties
--------------------|-------------------
Tech to Unlock      | [Specialized Electrics](http://wiki.kerbalspaceprogram.com/wiki/Specialized_Electrics)
VAB Category        | Science Parts
Manufacturer        | AIES Aerospace
Cost                | 9500
Mass                | 1.0 tons
Diameter            | 3.5 m
Drag                | 0.2
Comlink power       | 2.60 charge/s
Science power       | 50 charge/s
Science efficiency  | 7.5 charge/Mit
{:.basic}

|Transmission Properties
--------------------------------------|-------------------
Maximum Range                         | 350,000,000 km
Reach                                 | Eeloo (all times)
Cone Diameter                         | 0.006&deg;
Cone covers Kerbin at                 | 11,000,000 km
Cone covers keosynchronous orbit at | 66,000,000 km
{:.xmit}

|Atmosphere Performance
|------------------------------------
|Does not break in atmospheric flight.
{:.atm}

---------------

</div>

### Reflectron GX-128

<div class="antenna" markdown="1">

![Picture of GX-128](antenna_gx128.png)

The Reflectron-GX-128 is the longest-range antenna available in RemoteTech. While it has, for all practical purposes, the same abilities as the CommTech-1, its foldable construction makes it much lighter.
{:.blurb}

|Basic Properties
--------------------|-------------------
Tech to Unlock      | [Advanced Science Tech](http://wiki.kerbalspaceprogram.com/wiki/Advanced_Science_Tech)
VAB Category        | Science Parts
Manufacturer        | Parabolic Industries
Cost                | 11000
Mass                | 0.5 tons
Diameter            | 6.5 m
Drag                | 0.2
Comlink power       | 2.80 charge/s
Science power       | 50 charge/s
Science efficiency  | 7.5 charge/Mit
{:.basic}

|Transmission Properties
--------------------------------------|-------------------
Maximum Range                         | 400,000,000 km
Reach                                 | Eeloo (all times)
Cone Diameter                         | 0.005&deg;
Cone covers Kerbin at                 | 14,000,000 km
Cone covers keosynchronous orbit at | 79,000,000 km
{:.xmit}

|Atmosphere Performance
------------------------------------|-------------------
Maximum ram pressure when deployed  | 6 kN/m<sup>2</sup>
Maximum safe speed at sea level     | 99 m/s
Maximum safe speed at 10 km         | 269 m/s
Minimum safe altitude at 2300 m/s   | 32 km
{:.atm}

---------------

</div>
