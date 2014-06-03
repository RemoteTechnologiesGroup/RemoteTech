---
title: Tutorial - Comsat Guidelines
layout: content
navbar: false
---

{% include banner.html %}

#Creating Communications Satellites

{% include toc.html %}

This tutorial covers general principles that apply to any communications satellite. Content specific to a particular purpose or orbital configuration will be covered in [other tutorials](../#setting-up-satellite-constellations).

##Planning

###Research

To create a viable communications satellite, you need at least the following technologies:

* [Flight Control](http://wiki.kerbalspaceprogram.com/wiki/Flight_Control), which unlocks probe cores and the [Reflectron DP-10](../../guide/parts/#reflectron-dp-10) launch antenna
* [Electrics](http://wiki.kerbalspaceprogram.com/wiki/Tech_tree#Electrics), which unlocks solar panels and the [Reflectron KR-7 dish](../../guide/parts/#reflectron-kr-7).

If you want to communicate with interplanetary probes, you will have to wait until you develop [Large Electrics](http://wiki.kerbalspaceprogram.com/wiki/Large_Electrics) or [Electronics](http://wiki.kerbalspaceprogram.com/wiki/Electronics).

###Payload

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

Communication with deep-space missions
:    For miscellaneous spacecraft that don't fit the other categories, RemoteTech offers [active vessel](../../guide/overview/#target-active) targeting mode. Any antennas targeted at "Active Vessel" will attempt to directly connect to the ship the player is currently using. While active vessel targeting has its limitations (in particular, it does not work through relays), it is the most efficient way to control spacecraft that are far from any planets or comsats. Always use your longest-range dish for this role, since the antenna's field of view does not matter.

**Remember: pick the right antenna for the right job, and don't bother with roles that are already taken by another (current or planned) satellite.**

##Design

Once you know what dishes you're using, you're ready to start working in the VAB.

![plot of orbital dark time vs. height above Kerbin](kerbin_darkness.png){:.right}

###Power Use

RemoteTech antennas consume a large amount of power. Ensure you have enough solar panels or RTG's to power all your antennas. Unlike in the stock game, you can't just take for granted that you have enough electricity.

For solar-powered satellites, make sure you have enough battery capacity to make it through the night side of the orbit. If you know your orbital period (P), orbital radius (a), and planetary radius (r), the time spent in darkness in a circular equatorial orbit is ![P × (1/180&deg;) arcsin (r/a)](darktime.png). For eccentric or inclined orbits, the calculation is much more complex.

The time for orbits around Kerbin is plotted on the right, as a function of orbital height from the top of Kerbin's atmosphere of the edge of Kerbin's SoI. For a 100&nbsp;km orbit, darkness lasts only 640 seconds, while for a keosynchronous equatorial orbit (KEO) it lasts 1200 seconds. 

**Example:** a brute-force approach to a late-game interplanetary satellite might feature 1 Communotron 32, 2 Comms DTS-M1's, 3 Communotron 88-88's, 2 Reflectron KR-14's, 3 CommTech-1's. This monstrosity will consume 15 ElectricCharge per second. If placed in KEO, it will need storage for 18,000 units of charge!

###Station-Keeping
{:.spacer}

If your satellite constellation requires extremely precise positioning, you may with to include RCS thrusters or a light engine on your satellite. Ion engines are excellent choices for station keeping because their low thrust gives you very fine control over your orbit.
