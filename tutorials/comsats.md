---
title: Tutorial - Comsat Guidelines
layout: content
navbar: false
---

{% include banner.html %}

# Creating Communications Satellites

{% include toc.html %}

This tutorial covers general principles that apply to any communications satellite. Content specific to a particular purpose or orbital configuration will be covered in [other tutorials](../#setting-up-satellite-constellations).

To follow this tutorial, you should be familiar with the basic rules of RemoteTech, including the differences between dishes and omnidirectional antennas, how dish cones work, and the range requirements of the antennas you've unlocked so far. You must also have a plan for where you are planning to send satellites or probes in at least the near future. By the end of the tutorial, you should understand:

* how to build satellites designed for a particular network
* how to minimize the number of antennas needed to support specific types of missions
* the design trade-offs between all-purpose or more specialized satellites

## Overview

In the early game, when all you have is a few satellites in low Kerbin orbit or traveling to the Mun or Minmus, almost any satellite network will do. Once they start launching more satellites and sending interplanetary probes, however, some players get bogged down in the tasks of launching replacement comsats or juggling dishes.

Fortunately, much of the trouble can be avoided with a little planning. Pick a role for each satellite, be it supporting low Kerbin orbit missions, allowing contact with the far sides of the Mun or Minmus, supporting landers on Eve or Duna, or networking with your Laythe colony. Once you know what kinds of missions a satellite needs to support -- and more importantly, what missions it doesn't -- you can pick a small set of antennas that will satisfy your needs. For example, if you launch a satellite (or, more likely, two) that always has a Communotron 88-88 dish pointed at Duna, you will always be able to send transmissions from Duna to Kerbin, no matter how many missions you launch or whether they are orbiters, landers, or both.

It certainly beats having a single-dish satellite that must be micromanaged for every mission.

## Planning

### Research

To create a viable communications satellite, you need at least the following technologies:

* [Flight Control](http://wiki.kerbalspaceprogram.com/wiki/Flight_Control), which unlocks probe cores and the [Reflectron DP-10](../../guide/parts/#reflectron-dp-10) launch antenna
* [Electrics](http://wiki.kerbalspaceprogram.com/wiki/Tech_tree#Electrics), which unlocks solar panels and one of the two [short-range dishes](../../guide/parts/#reflectron-kr-7).

If you want to communicate with interplanetary probes, you will have to wait until you develop [Large Electrics](http://wiki.kerbalspaceprogram.com/wiki/Large_Electrics) or [Electronics](http://wiki.kerbalspaceprogram.com/wiki/Electronics), which give you access to long-range dishes.

### Payload

The number and type of antennas on your satellite will depend on where you want it to relay transmissions: a satellite that needs to single-handedly contact any point in the system is going to be much larger and more complex than one that serves as an intermediary between two other relays. Location matters, too: a satellite in orbit around Kerbin will need more antennas than one around the Mun, or around Eeloo.

Generally, antennas will fit into one or more of the following roles:

Local communication
:    If your satellite has an omnidirectional antenna on board, it will be able to communicate freely with other ships within a few thousand kilometers, even if they are outside the original purpose of the network. Depending on the range of your omni antenna and the placement of the satellite(s), an omni may serve as your primary antenna, or it may simply be there for added flexibility.

Communication with the surface (including KSC, for Kerbin satellites)
:    Low-orbit constellations can use an omnidirectional antenna for this purpose; more remote satellites should cover the planet with a short-range, wide-angle dish.

Communication with siblings
:    If your satellite is part of a constellation, it needs to be able to relay information as needed with other satellites from the same set. Requirements are similar to those for surface communication; work out the spacing between your satellites before picking an antenna.

Communication with relays orbiting other planets (including Kerbin) or moons
:    Placing secondary relays around the Mun, Minmus, or planets lets you create a more robust network while using simpler and lighter satellite designs. For these connections you will need dishes. The [list of parts](../../guide/parts/#dish-antennas) has basic dish specs, including effective range. A good rule of thumb is to use the shortest-range dish that will always reach its destination; this will give you the widest possible field of view, maximizing the number of satellites you can relay through.

Communication with relays in high orbit
:    If a satellite orbiting another planet has a highly eccentric orbit, it may pass out of a cone's field of view. If you are planning to connect to such a relay satellite, you will need a dish that targets that specific satellite (possibly *instead of* a cone pointed at the planet, if the high satellite is supposed to be the main point of contact with the planet).

Communication with deep-space missions
:    For miscellaneous spacecraft that don't fit the other categories, RemoteTech offers [active vessel](../../guide/overview/#target-active) targeting mode. Any antennas targeted at "Active Vessel" will attempt to directly connect to the ship the player is currently using. While active vessel targeting has its limitations (in particular, it does not work through relays), it is the most efficient way to control spacecraft that are far from any planets or comsats. Always use your longest-range dish for this role, since the antenna's field of view does not matter.

**Remember: pick the right antenna for the right job, and don't bother with roles that are already taken by another (current or planned) satellite.**

## Design

Once you know what dishes you need, you're ready to start working in the VAB.

![plot of orbital dark time vs. height above Kerbin](kerbin_darkness.png){:.right}

### Power Use

RemoteTech antennas consume a large amount of power. Ensure you have enough solar panels or RTG's to power all your antennas. Unlike in the stock game, you can't just take for granted that you have enough electricity.

For solar-powered satellites, make sure you have enough battery capacity to make it through the night side of the orbit. If you know your orbital period (P), orbital radius (a), and planetary radius (r), the time spent in darkness in a circular equatorial orbit is ![P &times; (1/180&deg;) arcsin (r/a)](darktime.png). For eccentric or inclined orbits, the calculation is much more complex.

The time for orbits around Kerbin is plotted on the right, as a function of orbital height from the top of Kerbin's atmosphere of the edge of Kerbin's SoI. For a 100&nbsp;km orbit, darkness lasts only 640 seconds, while for a keosynchronous equatorial orbit (KEO) it lasts 1200 seconds. 

**Example:** a brute-force approach to a late-game interplanetary satellite might feature 1 Communotron 32, 2 Comms DTS-M1's, 3 Communotron 88-88's, 2 Reflectron KR-14's, 3 CommTech-1's. This monstrosity will consume 15 ElectricCharge per second. If placed in KEO, it will need storage for 18,000 units of charge!

### Station-Keeping
{:.spacer}

If your satellite constellation requires extremely precise positioning, you may with to include RCS thrusters or a light engine on your satellite. Ion engines are excellent choices for station keeping because their low thrust gives you very fine control over your orbit.
