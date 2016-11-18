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

Antenna | Type | Cone&nbsp;angle<br/>(degree) | Cost | Atmosphere<br/>safe | Range (Mm) | Power&nbsp;Drain<br/>(charge/s)
--- | ---: | ---: | ---: | ---: | ---: | ---:
Communotron 16-S | Omni | - | 300 | Yes | 1.50 | 0.02
Communotron 16 | Omni | - | 300 | No | 2.50 | 0.13
HG-5 High Gain Antenna | Dish | 90.000 | 600 | No | 20.00 | 0.55
Communotron DTS-M1 | Dish | 45.000 | 900 | No | 50.00 | 0.82
KSC Mission Control | Omni | - | - | - | 75.00 | -
RA-2 Relay Antenna | Dish | 12.500 | 1,800 | Yes | 200.00 | 1.15
RA-15 Relay Antenna | Dish | 0.250 | 2,400 | Yes | 10,000.00 | 1.10
Communotron HG-55 | Dish | 0.120 | 1,200 | No | 25,000.00 | 1.04
Communotron 88-88 | Dish | 0.060 | 1,500 | No | 40,000.00 | 0.93
RA-100 Relay Antenna | Dish | 0.025 | 3,000 | Yes | 100,000.00 | 1.10
{:.data}

### RemoteTech antennas

RemoteTech integrates the additional antennas into Kerbal Space Program. Among these antennas, the antenna, Reflectron DP-10, is the cheapest one while the Reflectron GX-128 wields the power so immense that a crash-party video can be streamed directly to Eeloo from Moho.

Antenna | Type | Cone&nbsp;angle<br/>(degree) | Cost | Atmosphere<br/>safe | Range (Mm) | Power&nbsp;Drain<br/>(charge/s)
--- | ---: | ---: | ---: | ---: | ---: | ---:
Reflectron DP-10 | Omni | - | 60 | Yes | 0.50 | 0.01
CommTech EXP-VR-2T | Omni | - | 400 | No | 3.00 | 0.18
Communotron 32 | Omni | - | 600 | No | 5.00 | 0.60
Reflectron KR-7 | Dish | 25.000 | 800 | Yes | 90.00 | 0.82
Reflectron KR-14 | Dish | 0.040 | 2,000 | Yes | 60,000.00 | 0.93 
CommTech-1 | Dish | 0.006 | 9,500 | Yes | 350,000.00 | 2.60
Reflectron GX-128 | Dish | 0.005 | 11,000 | No | 400,000.00 | 2.80
{:.data}

<hr>

## Omnidirectional Antennas

{::comment}
Yes, the non-breaking spaces are necessary. Without them, when printing the table on a narrow screen, browsers won't be smart enough to realize that notes is the only column that word-wraps well, and will try to create eye-wrenching entries like 2500
km
{:/comment}

The following list is of all stock and RemoteTech antennas of the omnidirectional type.

Antenna | Cost | Atmosphere<br/>safe | Range (Mm) | Power&nbsp;Drain<br/>(charge/s) | Notes
--- | ---: | ---: | ---: | ---: | ---:
[Reflectron DP-10](#reflectron-dp-10) | 60 | Yes | 0.50 | 0.01 | Activated by default
[Communotron 16-S](#communotron-16-s) | 300 | Yes | 1.50 | 0.02 | Activated by default
[Communotron 16](#communotron-16) | 300 | No | 2.50 | 0.13 | 
[CommTech EXP-VR-2T](#commtech-exp-vr-2t) | 400 | No | 3.00 | 0.18 | 
[Communotron 32](#communotron-32) | 600 | No | 5.00 | 0.60 | Upgraded version of Communotron 16
[KSC Mission Control](#ksc-mission-control) | - | - | 75.00 | - | Kerbin's Command Station
{:.data}

<div class="antenna" markdown="1">

### Reflectron DP-10

![Picture of Reflectron DP-10](antenna_dp10.png)

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
|Does not break off in flight
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
|Does not break off in flight
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
|Tear off in flight when deployed
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
Mass | 0.020 tons
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
|Tear off in flight when deployed
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
Mass | 0.010 tons
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
|Tear off in flight when deployed
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

Antenna | Cost | Atmosphere<br/>safe | Cone&nbsp;angle<br/>(degree) | Range (Mm) | Power&nbsp;Drain<br/>(charge/s) | Notes
--- | ---: | ---: | ---: | ---: | ---: | ---:
[HG-5 High Gain Antenna](#hg-5-high-gain-antenna) | 600 | No | 90.000 | 20.00 | 0.55 | 
[Communotron DTS-M1](#communotron-dts-m1) | 900 | No | 45.000 | 50.00 | 0.82 | 
[Reflectron KR-7](#reflectron-kr-7) | 800 | Yes | 25.000 | 90.00 | 0.82 | 
[RA-2 Relay Antenna](#ra-2-relay-antenna) | 1,800 | Yes | 12.500 | 200.00 | 1.15 | Cover Kerbin's sphere of influence
[RA-15 Relay Antenna](#ra-15-relay-antenna) | 2,400 | Yes | 0.250 | 10,000.00 | 1.10 | 
[Communotron HG-55](#communotron-hg-55) | 1,200 | No | 0.120 | 25,000.00 | 1.04 | 
[Communotron 88-88](#communotron-88-88) | 1,500 | No | 0.060 | 40,000.00 | 0.93 | 
[Reflectron KR-14](#reflectron-kr-14) | 2,000 | Yes | 0.040 | 60,000.00 | 0.93  | 
[RA-100 Relay Antenna](#ra-100-relay-antenna) | 3,000 | Yes | 0.025 | 100,000.00 | 1.10 | 
[CommTech-1](#commtech-1) | 9,500 | Yes | 0.006 | 350,000.00 | 2.60 | 
[Reflectron GX-128](#reflectron-gx-128) | 11,000 | No | 0.005 | 400,000.00 | 2.80 | Range is more than twice as wide as Kerbol System
{:.data}

<div class="antenna" markdown="1">

### HG-5 High Gain Antenna

![Picture of HG-5 High Gain Antenna](antenna_hg5.png)

**Bill Kerman's notes**

The HG-5 High Gain Antenna is the shortest-ranged of the directional dishes. With its wide cone, this antenna can easily maintain contact with multiple satellites orbiting Kerbin at 100 km from a relatively low altitude.
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | [Basic Science](http://wiki.kerbalspaceprogram.com/wiki/Technology_tree#Basic_Science)
Cost | 600
Mass | 0.070 tons
{:.basic}

| Transmission Properties
| ---
Comlink power | 0.55 charge/s
Science efficiency | 7.5 charge/Mit
Maximum Range | 20,000 km
Reach | Mun
Cone angle | 90.000&deg;
Cone covering Kerbin (0.7&nbsp;Mm altitude) at | 700 km
Cone covering keosynchronous orbit (2.9&nbsp;Mm altitude) at | 3,500 km
{:.xmit}

| Atmosphere Safety
| ---
| Tear off in flight when deployed
{:.atm}

---

</div>

<div class="antenna" markdown="1">

### Communotron DTS-M1

![Picture of Communotron DTS-M1](antenna_dtsm1.png)

**Bill Kerman's notes**

The Communotron DTS-M1 is the next step up from the HG-5 High Gain Antenna in terms of range, fund cost and power drain. Its cone is wide enough to comfortably command satellites orbiting Kerbin at 100 km from a relatively low altitude.
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | [Precision Engineering](http://wiki.kerbalspaceprogram.com/wiki/Technology_tree#Precision_Engineering)
Cost | 900
Mass | 0.050 tons
{:.basic}

| Transmission Properties
| ---
Comlink power | 0.82 charge/s
Science efficiency | 7.5 charge/Mit
Maximum Range | 50,000 km
Reach | Minmus
Cone angle | 45.000&deg;
Cone covering Kerbin (0.7&nbsp;Mm altitude) at | 1,700 km
Cone covering keosynchronous orbit (2.9&nbsp;Mm altitude) at | 8,500 km
{:.xmit}

| Atmosphere Safety
| ---
| Tear off in flight when deployed
{:.atm}

---

</div>

<div class="antenna" markdown="1">

### Reflectron KR-7

![Picture of Reflectron KR-7](antenna_refl7.png)

**Bill Kerman's notes**

The Reflectron KR-7 has a longer range than the Communotron DTS-M1, making it well-suited for a spacecraft beyond Minmus's orbit. However, its narrower cone reduces its coverage effectiveness at the Mun's altitude or lower. 
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | [Electrics](http://wiki.kerbalspaceprogram.com/wiki/Technology_tree#Electrics)
Cost | 800
Mass | 0.050 tons
{:.basic}

| Transmission Properties
| ---
Comlink power | 0.82 charge/s
Science efficiency | 7.5 charge/Mit
Maximum Range | 90,000 km
Reach | Kerbin's sphere of influence
Cone angle | 25.000&deg;
Cone covering Kerbin (0.7&nbsp;Mm altitude) at | 3,200 km
Cone covering keosynchronous orbit (2.9&nbsp;Mm altitude) at | 16,000 km
{:.xmit}

| Atmosphere Safety
| ---
| Does not break off in flight
{:.atm}

---

</div>

<div class="antenna" markdown="1">

### RA-2 Relay Antenna

![Picture of RA-2 Relay Antenna](antenna_ra2.png)

**Bill Kerman's notes**

This adorable antenna, RA-2 Relay Antenna, represents a compromise on the cone angle, range and fund cost. It packs a broad and serious punch for a reasonable price.
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | [Precision Engineering](http://wiki.kerbalspaceprogram.com/wiki/Technology_tree#Precision_Engineering)
Cost | 1,800
Mass | 0.150 tons
{:.basic}

| Transmission Properties
| ---
Comlink power | 1.15 charge/s
Science efficiency | 7.5 charge/Mit
Maximum Range | 200 Mm
Reach | Kerbin's sphere of influence
Cone angle | 12.500&deg;
Cone covering Kerbin (0.7&nbsp;Mm altitude) at | 6,400 km
Cone covering keosynchronous orbit (2.9&nbsp;Mm altitude) at | 32,000 km
{:.xmit}

| Atmosphere Safety
| ---
| Does not break off in flight
{:.atm}

---

</div>

<div class="antenna" markdown="1">

### RA-15 Relay Antenna

![Picture of RA-15 Relay Antenna](antenna_ra15.png)

**Bill Kerman's notes**

The RA-15 Relay Antenna is the first antenna to break the 1-Gm range barrier. However, the antenna is suitable for short-range and temporary interplanetary communications only. Moreover, its narrow cone of less than 1&deg; requires players to aim the antenna at a specific comsat instead of a constellation of satellites when maintaining a working connection.
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | [Electronics](http://wiki.kerbalspaceprogram.com/wiki/Technology_tree#Electronics)
Cost | 2,400
Mass | 0.300 tons
{:.basic}

| Transmission Properties
| ---
Comlink power | 1.10 charge/s
Science efficiency | 7.5 charge/Mit
Maximum Range | 10 Gm
Reach | Moho (same side of sun only), <br/>Eve (same side of sun only), <br/>Duna (same side of sun only)
Cone angle | 0.250&deg;
Cone covering Kerbin (0.7&nbsp;Mm altitude) at | 320 Mm
Cone covering keosynchronous orbit (2.9&nbsp;Mm altitude) at | 1,700 Mm
{:.xmit}

| Atmosphere Safety
| ---
| Does not break off in flight
{:.atm}

---

</div>

<div class="antenna" markdown="1">

### Communotron HG-55

![Picture of Communotron HG-55](antenna_hg55.png)

**Bill Kerman's notes**

The Communotron HG-55 is mounted on a foldable arm, allowing it to be attached to a smaller satellite than the RA-15 Relay Antenna, which requires a larger cross-section area. In addition, the longer range enables it to reach Moho and Eve easily at all the times. Like the other antenna, this antenna requires a specific direction to a single comsat instead of a wide communication net.
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | [Electronics](http://wiki.kerbalspaceprogram.com/wiki/Technology_tree#Electronics)
Cost | 1,200
Mass | 0.075 tons
{:.basic}

| Transmission Properties
| ---
Comlink power | 1.04 charge/s
Science efficiency | 7.5 charge/Mit
Maximum Range | 25 Gm
Reach | Moho (all times), Eve (all times), <br/>Duna (same side of sun only)
Cone angle | 0.120&deg;
Cone covering Kerbin (0.7&nbsp;Mm altitude) at | 670 Mm
Cone covering keosynchronous orbit (2.9&nbsp;Mm altitude) at | 3,400 Mm
{:.xmit}

| Atmosphere Safety
| ---
| Tear off in flight when deployed
{:.atm}

---

</div>

<div class="antenna" markdown="1">

### Communotron 88-88

![Picture of Communotron 88-88](antenna_com88-88.png)

**Bill Kerman's notes**

The Communotron 88-88 is by far the most compact interplanetary antenna in the retracted state. It can easily reach all the inner planets, and can even contact Dres when it is on the same side of the sun as Kerbin. However, its narrow cone means that players will have to point it at a specific comsat for a working connection.
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | [Automation](http://wiki.kerbalspaceprogram.com/wiki/Technology_tree#Automation)
Cost | 1,500
Mass | 0.100 tons
{:.basic}

| Transmission Properties
| ---
Comlink power | 0.93 charge/s
Science efficiency | 7.5 charge/Mit
Maximum Range | 40 Gm
Reach | Duna (all times), <br/>Dres (same side of sun only)
Cone angle | 0.060&deg;
Cone covering Kerbin (0.7&nbsp;Mm altitude) at | 1,400 Mm
Cone covering keosynchronous orbit (2.9&nbsp;Mm altitude) at | 6,700 Mm
{:.xmit}

| Atmosphere Safety
| ---
| Tear off in flight when deployed
{:.atm}

---

</div>

<div class="antenna" markdown="1">

### Reflectron KR-14

![Picture of Reflectron KR-14](antenna_refl14.png)

**Bill Kerman's notes**

The Reflectron KR-14 is a large intermediate-range interplanetary antenna. It can easily reach all the inner planets as well as Dres. Like the Communotron-88, the KR-14 has a narrow cone and will have difficulty "seeing" communications satellites if pointed directly at Kerbin from too close a range.
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | [High-Power Electrics](http://wiki.kerbalspaceprogram.com/wiki/Technology_tree#High-Power_Electrics)
Cost | 2,000
Mass | 0.100 tons
{:.basic}

| Transmission Properties
| ---
Comlink power | 0.93 charge/s
Science efficiency | 7.5 charge/Mit
Maximum Range | 60 Gm
Reach | Dres (all times), <br/>Jool (same side of sun only), <br/>Eeloo (periapsis and same side of sun only)
Cone angle | 0.040&deg;
Cone covering Kerbin (0.7&nbsp;Mm altitude) at | 2,100 Mm
Cone covering keosynchronous orbit (2.9&nbsp;Mm altitude) at | 10 Gm
{:.xmit}

| Atmosphere Safety
| ---
| Does not break off in flight
{:.atm}

---

</div>

<div class="antenna" markdown="1">

### RA-100 Relay Antenna

![Picture of RA-100 Relay Antenna](antenna_ra100.png)

**Bill Kerman's notes**

The RA-100 Relay Antenna is a long-range interplanetary antenna and can reliably contact a satellite at Jool at all the time. However, its large cross-section renders all but the largest rockets useless to send up. Like the other dishes, the direction to a specific satellite is required in order to establish a working connection as the antenna's cone is too narrow to cast a wide sweep on an entire satellite net.
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | [Automation](http://wiki.kerbalspaceprogram.com/wiki/Technology_tree#Automation)
Cost | 3,000
Mass | 0.650 tons
{:.basic}

| Transmission Properties
| ---
Comlink power | 1.10 charge/s
Science efficiency | 7.5 charge/Mit
Maximum Range | 100 Gm
Reach | Jool (all times), Eeloo (periapsis only)
Cone angle | 0.025&deg;
Cone covering Kerbin (0.7&nbsp;Mm altitude) at | 3,300 Mm
Cone covering keosynchronous orbit (2.9&nbsp;Mm altitude) at | 17 Gm
{:.xmit}

| Atmosphere Safety
| ---
| Does not break off in flight
{:.atm}

---

</div>

<div class="antenna" markdown="1">

### CommTech-1

![Picture of CommTech-1](antenna_ct1.png)

**Bill Kerman's notes**

The CommTech-1 is the first interplanetary antenna capable of reaching any planet, even Eeloo at apoapsis. However, the antenna has an extremely narrow cone of 0.01&deg; so players should avoid using it until they pass the orbit of Dres. It also may be emphasized that its energy consumption is high in the operation mode.
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | [Specialized Electrics](http://wiki.kerbalspaceprogram.com/wiki/Technology_tree#Specialized_Electrics)
Cost | 9,500
Mass | 0.300 tons
{:.basic}

| Transmission Properties
| ---
Comlink power | 2.60 charge/s
Science efficiency | 7.5 charge/Mit
Maximum Range | 350 Gm
Reach | Eeloo (all times)
Cone angle | 0.006&deg;
Cone covering Kerbin (0.7&nbsp;Mm altitude) at | 14 Gm
Cone covering keosynchronous orbit (2.9&nbsp;Mm altitude) at | 67 Gm
{:.xmit}

| Atmosphere Safety
| ---
| Does not break off in flight
{:.atm}

---

</div>

<div class="antenna" markdown="1">

### Reflectron GX-128

![Picture of Reflectron GX-128](antenna_gx128.png)

**Bill Kerman's notes**

The Reflectron GX-128 is a marvel of engineering, transmitting at tremendous power through the dozen lights of void. It is one of the most advanced radio transmitters the KSC's engineering division had ever developed, and capable to reach Eeloo at ease. While this antenna has, for all practical purposes, the same abilities as the CommTech-1, its foldable construction makes it much lighter and small in cross-section.
{:.blurb}

| Basic Properties
--- | ---
Tech to Unlock | [Advanced Science Tech](http://wiki.kerbalspaceprogram.com/wiki/Technology_tree#Advanced_Science_Tech)
Cost | 11,000
Mass | 0.240 tons
{:.basic}

| Transmission Properties
| ---
Comlink power | 2.80 charge/s
Science efficiency | 7.5 charge/Mit
Maximum Range | 400 Gm
Reach | Eeloo (all times)
Cone angle | 0.005&deg;
Cone covering Kerbin (0.7&nbsp;Mm altitude) at | 17 Gm
Cone covering keosynchronous orbit (2.9&nbsp;Mm altitude) at | 81 Gm
{:.xmit}

| Atmosphere Safety
| ---
| Tear off in flight when deployed
{:.atm}

---

</div>
