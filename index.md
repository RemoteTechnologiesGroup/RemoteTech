---
title: RemoteTech
layout: content
---

{% include banner.html %}

{% include toc.html %}

##Introduction to RemoteTech
RemoteTech is a modification for Squad's 'Kerbal Space Program' (KSP) which overhauls the unmanned space program. It does this by requiring unmanned vessels have a connection to Kerbal Space Center (KSC) to be able to be controlled. This adds a new layer of difficulty that compensates for the lack of live crew members.

##First steps

* Like in stock KSP, you need to research Flight Control before you can build unmanned probes.
* You need to use an antenna that won't break in the atmosphere to be able to control an unmanned rocket during atmospheric flight. The Reflectron DP-10, unlocked along with the Stayputnik probe core, is the earliest such antenna available. Others can be found in the [parts listing](guide/parts/).
* For farther flights, your probe should also have more powerful antennas, such as the Communotron 16 or the Comms DTS-M1. You will have to manually turn on these antennas once you get high enough that the airflow won't break them off, and if you are using the DTS-M1 you will also need to target it at Mission Control. Both commands can be done by right-clicking on the antenna.
* Once you can place satellites in orbit, consider putting up some comsats to maintain a connection when out of sight of KSC. See the [tutorials](tutorials/#setting-up-satellite-constellations) for more details.
* As you expand farther out into the system, you may need to expand and/or upgrade your comsat network to allow for connections to probes orbiting other moons or planets. Plan ahead!

##Overview of mechanics

###Antennas
Using antennas, it is now possible to set up satellite networks to route your control input. Unlike in stock KSP,  antennas will no longer activate or deactivate automatically; you must order an antenna to activate by right-clicking on it. There are two classes of antennas: 'Dishes' and 'Omnidirectionals'.

####Dishes
Dishes are antennas that must be instructed what direction to point at. They do not need to be physically turned; you need merely select a target from a list. Dishes tend to be used for long range communication and come with a cone of vision (which is narrower for longer-range antennas). If the dish is pointed at a planet or moon, anything inside this cone can achieve a connection with the dish.

####Omnidirectionals
Omni antennas radiate in every direction equally, and as such do not require you to target them at anything. A consequence is that they are limited to shorter ranges.

###Signal Delay
To comply with Kerbal law, RemoteTech is required to delay your control input so that signaling does not exceed the 'speed of light' (pfft, what a silly law). If you are aware of the consequences of breaking the law (or like being a rebel), you are free to turn this off in the settings file (which will be created once you start KSP).

###Connections
A 'working connection' is defined as a command center being able to send control input to its destination and back. Connections between neighbouring satellites are referred to as 'links'. To have a link between two satellites, it is required that *both* satellites [can transmit a signal](guide/overview/#connection-rules) to the other independently. You have a connection when there is a sequence of links between a command center and the destination.

###Signal Processors
Signal Processors are any part that can receive commands over a working connection, including all stock probe cores. You will only be able to control a signal processor as long as you have a working connection, and by default you will be subject to [signal delay](#signal-delay). Signal processors also include a *Flight Computer* that can be used to schedule actions ahead of time, for example to carry out basic tasks during a communications gap.

<!--**Beware**: if you do not have a working connection, you cannot send **any** commands to an unmanned probe, including commands to activate its antennas!-->

###Command Stations
For those extra long distance missions, it is possible to set up a team of Kerbals to act as a local command center. This Command Station can not process science, a connection to KSC will still be required for that. However, the Command Station allows you to work without the signal delay to Kerbin, which might otherwise climb up to several minutes. Command Stations require a special probe part and a minimum number of kerbals on the same ship. Consult your VAB technicians for more information.

###Science Transmissions
Transmitting science back to KSC now requires you have a working connection to KSC. Any other source of control, such as a crew pod or a working connection to a command station, does not count.

##List of parts

###Modified stock parts

* All stock probe cores now have [signal processor capability](#signal-processors) so that they are affected by the communications network they are connected to.

* The three stock antennas have been modified to make them fit the rules of RemoteTech: the Communotron 16 is now the basic [omnidirectional antenna](#omnidirectionals), the Comms DTS-M1 a short-range [dish](#dishes), and the Communotron 88-88 a medium-range dish.

* The Launch Stability Enhancer now acts as a land line for the rocket, allowing the player to send pre-launch commands regardless of whether any antennas are active.

###Modified third-party mod parts

* The MechJeb AR202 can act as a signal processor, just like the stock probe cores.

Neither other third-party probe cores, nor any third-party antennas, are supported at this time.

###New parts

RemoteTech includes seven new antennas.

* The Reflectron DP-10 is a short-range omnidirectional antenna intended for launch and landing.
* The CommTech EXP-VR-2T and Communotron 32 are enhanced omnidirectional antennas.
* The Reflectron KR-7 and KR-14 are short- and medium-range dishes, respectively.
* The CommTech-1 and Reflectron GX-128 are long-range dishes designed for missions to the outer planets.

##Credits
* RT1 Contributors: JDP, The_Duck, gresrun, Tosh, rkman, NovaSilisko, r4m0n, Alchemist, Kreuzung, Spaceghost, Tomato, BoJaN, Mortaq.
* RT2 Contributors: JDP, Cilph, TaranisElsu, acc, Vrana, MedievalNerd, NathanKell, jdmj, kommit.
* RT2 v1.4.0 and beyond is maintained by the Remote Technologies Group, a community effort to ensure the proliferation of unmanned vessels throughout the known Kerbalverse: Erendrake, Pezmc, Starstrider42, Peppie23, Warrenseymour
