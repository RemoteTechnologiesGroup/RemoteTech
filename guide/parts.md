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

<hr>

## Overview

{::comment}
Yes, the non-breaking spaces are necessary. Without them, when printing the table on a narrow screen, browsers won't be smart enough to realize that notes is the only column that word-wraps well, and will try to create eye-wrenching entries like 2500
km
{:/comment}

### Stock antennas

Kerbal Space Program has a number of antennas available, which ranges and power consumptions vary widely.

Antenna | Type | Cone angle (degree) | Cost | Atmosphere safe | Range (Mm) | Power Drain (charge/s)
--- | ---: | ---: | ---: | ---: | ---: | ---:
Communotron 16-S | Omni | - | 300 | Yes | 1.50 | 0.02
Communotron 16 | Omni | - | 300 | No | 2.50 | 0.13
HG-5 High Gain Antenna | Dish | 90.00 | 600 | No | 20.00 | 0.55
KSC Mission Control | Omni | - | - | - | 75.00 | -
RA-2 Relay Antenna | Dish | 12.50 | 1,800 | Yes | 200.00 | 1.15
RA-15 Relay Antenna | Dish | 0.25 | 2,400 | Yes | 10,000.00 | 1.10
Communotron HG-55 | Dish | 0.12 | 1,200 | No | 25,000.00 | 1.04
Communotron 88-88 | Dish | 0.06 | 1,500 | No | 40,000.00 | 0.93
Communotron DTS-M1 | Dish | 45.00 | 900 | No | 50,000.00 | 0.82
RA-100 Relay Antenna | Dish | 0.65 | 3,000 | Yes | 100,000.00 | 1.10
{:.data}

### RemoteTech antennas

RemoteTech integrates the additional antennas into Kerbal Space Program. Among these antennas, the antenna, Reflectron DP-10, is the cheapest one while the Reflectron GX-128 wields the power so immense that a crash-party video can be streamed directly to Eeloo from Moho.

Antenna | Type | Cone angle (degree) | Cost | Atmosphere safe | Range (Mm) | Power Drain (charge/s)
--- | ---: | ---: | ---: | ---: | ---: | ---:
Reflectron DP-10 | Omni | - | 60 | Yes | 0.50 | 0.01
CommTech EXP-VR-2T | Omni | - | 400 | No | 3.00 | 0.18
Communotron 32 | Omni | - | 600 | No | 5.00 | 0.60
Reflectron KR-7 | Dish | 25.00 | 800 | Yes | 90.00 | 0.82
Reflectron KR-14 | Dish | 0.04 | 2,000 | Yes | 60,000.00 | 0.93 
CommTech-1 | Dish | 0.01 | 9,500 | Yes | 350,000.00 | 2.60
Reflectron GX-128 | Dish | 0.01 | 11,000 | No | 400,000.00 | 2.80
{:.data}

<hr>

## Omnidirectional Antennas

The following list is of all stock and RemoteTech antennas of the omnidirectional type.

Antenna | Cost | Atmosphere safe | Range (Mm) | Power Drain (charge/s) | Notes
--- | ---: | ---: | ---: | ---: | ---: | ---
[Reflectron DP-10](#reflectron-dp-10) | 60 | Yes | 0.50 | 0.01 | Activated by default
[Communotron 16-S](#communotron-16-s) | 300 | Yes | 1.50 | 0.02 | Activated by default
[Communotron 16](#communotron-16) | 300 | No | 2.50 | 0.13 | 
[CommTech EXP-VR-2T](#commtech-exp-vr-2t) | 400 | No | 3.00 | 0.18 | 
[Communotron 32](#communotron-32) | 600 | No | 5.00 | 0.60 | Upgraded version of Communotron 16
[KSC Mission Control](#ksc-mission-control) | - | - | 75.00 | - | Kerbin's Command Station
{:.data}

<div class="antenna" markdown="1">

### Reflectron DP-10

![Picture of Reflectron DP-10](antenna_dp10_B.png)

**Bill Kerman's notes**

The Reflectron DP-10 is a lightweight and cheap antenna. Its omnidirectional nature and high-speed survival in atmosphere make it an excellent choice for launches and landings. However, its short range rapidly renders it useless outside low Kerbin orbits.
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | [Start](http://wiki.kerbalspaceprogram.com/wiki/Technology_tree#Start)
Cost | 60
Mass | 0.015 tons
{:.basic}

| Transmission Properties
| ---
Comlink power | 0.01 charge/s
Science efficiency | 7.50 charge/Mit
Maximum Range | 500 km
Reach | Any line of sight to Mission Control under 150 km altitude
{:.xmit}

| Atmosphere Safety
| ---
|Does not break off in flight.
{:.atm}

---

</div>

<div class="antenna" markdown="1">

### Communotron 16-S

![Picture of Communotron 16-S](antenna_com16s.png)

**Bill Kerman's notes**

The Communotron 16-S is a surface mount version of the [Communotron 16](#communotron-16), in which this antenna will not break off during a high-speed flight. However, the trade-off for this survival is a reduction in its range. Along with the Communotron 16, it forms the backbone of most players' low-orbit communications networks until the CommTech EXP-VR-2T and Communotron 32 are researched.
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | [Engineering 101](http://wiki.kerbalspaceprogram.com/wiki/Technology_tree#Engineering_101)
Cost | 300
Mass | 0.015 tons
{:.basic}

| Transmission Properties
| ---
Comlink power | 0.02 charge/s
Science efficiency | 7.50 charge/Mit
Maximum Range | 1,500 km
Reach | Low Kerbin Orbit
{:.xmit}

| Atmosphere Safety
| ---
|Does not break off in flight.
{:.atm}

---

</div>

<div class="antenna" markdown="1">

### Communotron 16

![Picture of Communotron 16](antenna_com16.png)

**Bill Kerman's notes**

The Communotron 16 is the versatile and lightweight antenna, essential for transmitting science from your early flights. Along with the Communotron 16-S, it forms the backbone of most players' low-orbit communications networks until the CommTech EXP-VR-2T and Communotron 32 are researched.
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | [Engineering 101](http://wiki.kerbalspaceprogram.com/wiki/Technology_tree#Engineering_101)
Cost | 300
Mass | 0.005 tons
{:.basic}

| Transmission Properties
| ---
Comlink power | 0.13 charge/s
Science efficiency | 7.50 charge/Mit
Maximum Range | 2,500 km
Reach | Low Kerbin Orbit
{:.xmit}

| Atmosphere Safety
| ---
|Tear off in flight.
{:.atm}

---

</div>

<div class="antenna" markdown="1">

### CommTech EXP-VR-2T

![Picture of EXP-VR-2T](antenna_expvr2t.png)

**Bill Kerman's notes**

The CommTech EXP-VR-2T is an advanced antenna unlocked late in the technology tree. Mounted on an extendable boom, the antenna is more compact than the Communotron 16 in a retracted state, but larger when deployed. Although it is inferior to the Communotron 32 in terms of maximum range, the CommTech EXP-VR-2T's comlink is incredibly efficient.
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | [Specialized Electrics](http://wiki.kerbalspaceprogram.com/wiki/Technology_tree#Specialized_Electrics)
Cost | 500
Mass | 0.02 tons
{:.basic}

| Transmission Properties
| ---
Comlink power | 0.18 charge/s
Science efficiency | 7.50 charge/Mit
Maximum Range | 3,000 km
Reach | Low Kerbin Orbit
{:.xmit}

| Atmosphere Safety
| ---
|Tear off in flight.
{:.atm}

---

</div>

<div class="antenna" markdown="1">

### Communotron 32

![Picture of Communotron 32](antenna_com32.png)

**Bill Kerman's notes**

The Communotron 32 is the most powerful omnidirectional antenna available and capable of reaching kerbisynchronous equatorial orbits of almost 3 Mm. However, a large area of solar panels is required to satisfy the antenna's high energy demand, which is nearly as much as the low-tier dishes. Also, it is significantly larger than its cousin, the Communotron 16.
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | [High-Power Electrics](http://wiki.kerbalspaceprogram.com/wiki/Technology_tree#High-Power_Electrics)
Cost | 600
Mass | 0.01 tons
{:.basic}

| Transmission Properties
| ---
Comlink power | 0.60 charge/s
Science efficiency | 7.50 charge/Mit
Maximum Range | 5,000 km
Reach | Near-Kerbin space, synchronous orbit
{:.xmit}

| Atmosphere Safety
| ---
|Tear off in flight.
{:.atm}

---

</div>

<div class="antenna" markdown="1">

### KSC Mission Control

![Picture of KSC Mission Control](ksc_mission_control.png)

**Bill Kerman's notes**

At the Kerbal Space Center, the Mission Control establishes and maintains connections through the Tracking Station with every antenna-equipped vessel in the Kerbol system. Moreover, it acts as a permanent command station to issue commands to an unmanned probe over its working connection.
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | -
Cost | -
Mass | Immeasurable
{:.basic}

| Transmission Properties
| ---
Comlink power | -
Science efficiency | -
Maximum Range | 75,000 km
Reach | Kerbin's two natural satellites, Mun and Minmus
{:.xmit}

| Atmosphere Safety
| ---
| Not certified to fly
{:.atm}

---

</div>

<hr>

## Dish Antennas

{::comment}
Yes, the non-breaking spaces are necessary. Without them, when printing the table on a narrow screen, browsers won't be smart enough to realize that notes is the only column that word-wraps well, and will try to create eye-wrenching entries like 2500
km
{:/comment}

The following list is of all stock and RemoteTech antennas of the dish type.

Antenna | Cone angle (degree) | Cost | Atmosphere safe | Range (Mm) | Power Drain (charge/s) | Notes
--- | ---: | ---: | ---: | ---: | ---: | ---
[HG-5 High Gain Antenna](#hg-5) | 90.00 | 600 | No | 20.00 | 0.55 | 
[Reflectron KR-7](#reflectron-kr-7) | 25.00 | 800 | Yes | 90.00 | 0.82 | 
[RA-2 Relay Antenna](#ra-2) | 12.50 | 1,800 | Yes | 200.00 | 1.15 | 
[RA-15 Relay Antenna](#ra-15) | 0.25 | 2,400 | Yes | 10,000.00 | 1.10 | 
[Communotron HG-55](#communotron-hg-55) | 0.12 | 1,200 | No | 25,000.00 | 1.04 | 
[Communotron 88-88](#communotron-88-88) | 0.06 | 1,500 | No | 40,000.00 | 0.93 | 
[Communotron DTS-M1](#communotron-dts-m1) | 45.00 | 900 | No | 50,000.00 | 0.82 | 
[Reflectron KR-14](#reflectron-kr-14) | 0.04 | 2,000 | Yes | 60,000.00 | 0.93  | 
[RA-100 Relay Antenna](#ra-100) | 0.65 | 3,000 | Yes | 100,000.00 | 1.10 | 
[CommTech-1](#commtech-1) | 0.01 | 9,500 | Yes | 350,000.00 | 2.60 | 
[Reflectron GX-128](#reflectron-gx-128) | 0.01 | 11,000 | No | 400,000.00 | 2.80 | Range is more than twice as wide as Kerbal System
{:.data}

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
