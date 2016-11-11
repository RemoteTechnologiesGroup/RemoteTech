---
title: RemoteTech
layout: content
---

{% include banner.html %}

{% include toc.html %}

## Introduction to RemoteTech
RemoteTech is a modification for Squad's 'Kerbal Space Program' (KSP) which overhauls the unmanned space program. It does this by requiring unmanned vessels have a connection to Kerbal Space Center (KSC) to be able to be controlled. This adds a new layer of difficulty that compensates for the lack of live crew members.

## First steps

* Like in stock KSP, you need to research Basic Science in the technology tree before you can build an unmanned probe.
* You need to use an antenna that won't break in the atmosphere to be able to control an unmanned rocket during atmospheric flight. The Reflectron DP-10, available in Start, is the earliest such antenna available. Others can be found in the [parts listing](guide/parts/).
* For farther flights, your probe should utilise more powerful antennas, such as the Communotron 16 or the Communotron DTS-M1. You need to manually activate these antennas once you are flying high enough that the airflow is too thin to break them off. If you are using the DTS-M1, you also need to target it at Mission Control. The activation and targeting can be performed by right-clicking on an antenna.
* Once you place a few satellites in Kerbin orbit, consider adding some more communication satellites (comsats) to maintain a connection when out of sight of KSC. See the [tutorials](tutorials/#setting-up-satellite-constellations) for more details.
* As you expand farther out into the system, you may need to expand and/or upgrade your comsat network to allow for connections to probes orbiting other moons or planets. Plan ahead!

## Overview of mechanics

### Antennas
Using antennas, it is now possible to set up satellite networks to route your control input. Unlike in stock KSP, antennas will no longer activate or deactivate automatically; you must order an antenna to activate by right-clicking on it. There are two classes of antennas: 'Dishes' and 'Omnidirectionals'.

#### Dishes
Useful for long range communications, dishes are directional or beam antennas that must be instructed what direction to point at. They do not need to be physically rotated; you need merely select a target from a list of comsats. These dishes come with a cone of vision (which becomes narrower for a longer range). If the dish is pointed at a planet or moon, anything inside this cone can achieve a connection with the dish.

#### Omnidirectionals
Omni antennas radiate in every direction equally, and as such do not require you to target them at anything. However, they have shorter ranges than the dishes.

The Kerbal Space Center has multiple dish antennas that can be pointed to different targets, making it behave like an omnidirectional antenna with a range of 75 Mm. If you want to send probes beyond Kerbin's sphere of influence, you *must* invest in some communications satellites with long-range antennas.

### Signal Delay
To comply with Kerbal law, RemoteTech is required to delay your control input so that signalling does not exceed the 'speed of light'. If you are aware of the consequences of breaking the law (or like being a rebel), you are free to disable this in the RemoteTech settings, available as a launcher button in the KSC scene.

### Connections
A 'working connection' is defined as a command center being able to send control input to its destination. Connections between neighbouring satellites are referred to as 'links'. To have a link between two satellites, it is required that *both* satellites [are set up to contact each other](guide/overview/#connection-rules). You have a connection when there is a sequence of links between a command center and the destination.

### Signal Processors
Signal Processors are any part that can receive commands over a working connection, including all stock probe cores. You will only be able to control a signal processor as long as you have a working connection, and by default you will be subject to [signal delay](#signal-delay). Signal processors also include a [flight computer](guide/comp/) that can be used to schedule actions ahead of time, for example to carry out basic tasks during a communications gap.

**Beware:** if you do not have a working connection, you cannot send **any** commands to an unmanned probe, including commands to activate its antennas!

### Command Stations
For those long-distance missions, it is possible to set up a team of Kerbals to act as a local command center. Setting up a command station is a major undertaking for situations where you *really* need real-time control of nearby probes. It is not something to be attempted lightly (literally).

Command Stations allow you to work without the signal delay to Kerbin, which might otherwise climb up to several minutes. However, a Command Station cannot process science; a connection to KSC will still be required for that. Command Stations require a special probe part and a minimum number of kerbals on the same ship. Consult your VAB technicians for more information.

### Science Transmissions
Transmitting science back to KSC requires a working connection to KSC. Any other source of control, such as a crew pod or a working connection to a command station, does not count.

## Credits
* RT1 Contributors: JDP, The_Duck, gresrun, Tosh, rkman, NovaSilisko, r4m0n, Alchemist, Kreuzung, Spaceghost, Tomato, BoJaN, Mortaq.
* RT2 Contributors: JDP, Cilph, TaranisElsu, acc, Vrana, MedievalNerd, NathanKell, jdmj, kommit, koritastic, Dail8859, Grays, Jsartisohn, MOARdV, Reignerok, Pezmc, Starstrider42, woogoose, Peppie23, d4rksh4de, tomek.piotrowski, Erendrake, Warrenseymour.
* RT v1.8.0 and beyond is currently maintained by the Remote Technologies Group, a community effort to ensure the proliferation of unmanned vessels throughout the known Kerbalverse: neitsa, TaxiService.
