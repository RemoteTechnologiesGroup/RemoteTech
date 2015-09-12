---
title: Tutorial - Interplanetary Networks
layout: content
---

{% include banner.html %}

# Tutorial: Interplanetary Networks

Original tutorial by grays42. Some editing done by the Remote Technologies Group.

{% include toc.html %}

This tutorial is geared toward players who have connected to Low Kerbin Orbit and have made one or more connections to the Mun and Minmus, and now want to create stable, reliable networks at interplanetary distances. The same relay system that works for Kerbin’s moons can work outside Kerbin’s sphere of influence, but the distance and equipment needed change significantly.

## Dish Range and Cone of Influence

This guide will refer to the Comms DTS-1 (45 Mm) and Reflectron KR-7 (90 Mm) as “short-range dishes”, because these operate only within Kerbin’s sphere of influence and are designed for connection to Mun and Minmus. The Communotron 88-88 and all later dishes are “long-range dishes” designed to reach out between planets.

Each dish has advantages and disadvantages, but the two most important features are the dish’s **range** and **cone of influence**.

**Range**
: The dish must have a long enough range to cross the distance to the target planet or moon. See the table below for minimum antennas needed. Some antennas will provide partial coverage (the Reflectron KR-14 will connect to Jool during most of the year, for example), but this is not advised unless your interplanetary missions are carefully scheduled to avoid range blackouts.

**Cone of Influence**
: All dishes can connect to multiple targets within a cone extending out from the dish, provided the dish is pointed at a planet or moon rather than at a specific satellite. Any satellites within this cone are available for connection even though the dish isn’t pointed directly at them. Longer range dishes have narrower cones, and using an antenna with too narrow of a cone may cause communications blackouts when pointing at a planet.

| Target | Maximum Separation from Kerbin | Minimum Antenna Needed |
|:--------:|:-------:|:--------:|
| Mun | 12 Mm | Comms DTS-M1 (50 Mm) |
| Minmus | 47 Mm | Comms DTS-M1 (50 Mm) |
| Moho | 19.9 Gm | Communotron 88-88 (40 Gm) |
| Eve | 24 Gm | Communotron 88-88 (40 Gm) |
| Duna | 35 Gm | Communotron 88-88 (40 Gm) |
| Dres | 60 Gm | Reflectron KR-14 (60 Gm) |
| Jool | 86 Gm | CommTech-1 (350 Gm) |
| Eeloo | 127 Gm | CommTech-1 (350 Gm) |
{: .data}

Unless design decisions dictate a different solution, it is generally advisable to use the shortest range antenna that will suit your interplanetary network in order to guarantee as broad a cone of coverage as possible. (Shorter dishes generally require less power, as well.) 

**Example:** while using the most powerful dish (Reflectron GX-128) to connect to a satellite around Moho, the width of the cone at Moho’s closest pass to Kerbin is only 637 km. This means that it will be unable to reliably connect to any satellites that orbit Moho at an altitude higher than 68 km. However, the lower-range Communotron 88-88 with its 0.06° cone is able to provide reliable connection to satellites up to 3500 km altitude while still guaranteeing connections at all distances between Kerbin and Moho.

This problem can be mitigated by pointing the dish at a specific satellite rather than a planet or moon, but this limits the usefulness of the antenna as a method to connect to many vessels at a particular destination, and introduces micromanagement in assigning specific dishes to specific missions.

## Interplanetary Network Example

This example of an interplanetary network is a commonly used due to its small number of relays and reliably high uptime. Other networks are possible on a situational basis, and this method can be extended for use in more complex networks around Jool’s moons.

![Long range network illustration](http://i.imgur.com/L33gBHb.png){:.right style='max-width: 70%;'}

1. Several omnidirectional satellites in low orbit around Kerbin’s equator (which you should already have up anyway). At least one must have a short-range dish (such as the 90 Mm Reflectron KR-7) for connecting to the long range relay in step 2. Using the 90 Mm dish allows the relay to connect to the edge of Kerbin’s sphere of influence.

2. One long-range relay satellite in a very eccentric near-polar orbit (~80 km periapsis, ~80,000 km apoapsis, inclination near poles). Should have a short-range dish pointed at Kerbin, to connect to the low-orbit network, and one or more long-range dishes pointed at the destination planet(s). The high apoapsis and high inclination guarantees the satellite will always have a connection to your low orbit satellite and will only blackout behind the planet for about 10 minutes every two weeks (99.8% uptime).

3. One or more relays at the destination equipped with a long-range dish pointed at the relay satellite in step 2 and either an omnidirectional antenna or short-range antenna for communication with a lander. (Because you have direct control over your lander and relay at your destination, blanket coverage is generally optional.)

## Other Considerations and Pitfalls

### Minimize line-of-sight blackout periods.

Every step in the chain is vulnerable to blackouts, and any break in the chain will black out the entire link. Ensure that blackout periods are minimized by providing redundant coverage or placing long-range relays into highly eccentric orbits.

### Minimize power blackout periods.

Be sure to account for power concerns for all satellites. Use the power calculator [available here](https://docs.google.com/spreadsheet/ccc?key=0AkXf-77s6gmFdEdVeGFqX0xobTczYkhEaEVrVTdWV3c) (or in [OpenOffice/LibreOffice format](http://www.mediafire.com/view/63tyil19qwo417r/KSP_Battery_Calculator_LibreOffice_fix.ods) if the Google link doesn't work) if you’re not sure how much solar and battery power your satellite will need.

### Orbits’ relative positions will change throughout the year

Setting a satellite into a polar circular orbit so that it always has a clear line of sight to Kerbin is fine, except that throughout the course of a year, that orbit’s orientation relative to Kerbin will change as both planets orbit the sun. During some periods of the year, the satellite will black out on every orbit as it passes behind the planet. Avoid this by setting up highly eccentric orbits, which will black out infrequently and briefly.

### Use Active Vessel with caution.

The use of Active Vessel as a target of any dish antenna can potentially cause confusion. In all cases of dish antennas, using explicit connections rather than Active Vessel connections dramatically reduces network troubleshooting issues. More detail regarding the Active Vessel troubleshooting issue is available [here](http://remotetechnologiesgroup.github.io/RemoteTech/guide/overview/#target-active).

A tutorial on when to use and how to troubleshoot active vessel will be posted later.

### Beware of time delay.

Interplanetary probes experience a very significant (several minutes or more) time delay unless local control is provided by a [RC-L01](http://wiki.kerbalspaceprogram.com/wiki/RC-L01_Remote_Guidance_Unit) as a [Command Station](http://remotetechnologiesgroup.github.io/RemoteTech/#command-stations). Commands can be queued up to be executed during blackout periods, but the execution of extremely long burns to insert into orbit is very unpredictable, and interplanetary atmospheric aerobraking is very difficult, bordering on impossible.

Burns to insert into orbit after an interplanetary intercept need to be done with a short burn, if at all possible; this minimizes the risk of an orbit that fails to insert or de-orbits into the planet, with no time to correct due to time delay. While very long burns can be done using the flight computer, be prepared for inaccuracies or problems due to time delay that may be difficult to fix. You can avoid this problem either by using regular engines rather than nuclear or ion engines, or by making your initial approach at a higher altitude (e.g. 500-1000 km), where you'll cross a shorter distance during the burn. Both methods will make the burn easier at the cost of requiring slightly more fuel.

Without a local source of control such as a command station, attempting to land a probe using engines at interplanetary distances is not recommended, as the time delay makes such a landing virtually impossible. Parachute landings can be done, however, as shown in the [re-entry tutorial](../reentry/).

## Final Challenge: Jool

Jool is an interesting case for interplanetary networks, and it is advisable to attempt other connections prior to tackling the gas giant’s multiple moon system. With Jool’s radius of 6 Mm, setting up a low-orbit omnidirectional relay is impractical, and with Pol’s apoapsis of 210 Mm, the short-range dishes can’t guarantee coverage to all of Jool’s moons. In addition, the CommTech-1’s cone of influence of 0.006° provides a coverage diameter of 10.8 Gm at Jool’s closest point to Kerbin, making blanket coverage of all moons with a single dish impossible.

Any moon can get a fairly reliable connection using the method outlined above by utilizing a separate connection for each moon, but the pinnacle of RemoteTech 2 mastery is setting up a single relay to Jool, and relaying that connection to all of Jool’s moons using a Joolian satellite system. That exercise is left to the user.
