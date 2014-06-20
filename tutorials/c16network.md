---
title: Tutorial - Medium-Altitude Omni Network
layout: content
navbar: false
---

{% include banner.html %}

#Creating an Omni-Only Network

{% include toc.html %}

This tutorial covers how to create a four-satellite network* using no antenna more powerful than the [Communotron 16](../../guide/parts/#communotron-16). Players who have access to more powerful antennas can feel free to include them in their satellite design, while leaving the Communotron 16 as the backbone of the constellation.

*While it's possible to make a 3-satellite network with a few changes to these instructions, the orbit and synchronization requirements are too strict for it to be worth the trouble.

The requirement that the satellites be connected by Communotron 16's forces the altitude of the satellites to be between 250&nbsp;km (below which the satellites are below each others' horizons) and 1160&nbsp;km (above which the satellites are out of range of each other). The closer you are to the middle of this range, the broader your tolerances on aligning your satellites. We'll go with an altitude of 776.6&nbsp;km, which in addition to being far from either end of the altitude range has a convenient orbital period of 1.5&nbsp;hours.

<!-- The tutorial gives two methods for creating an omni network. [Separate launches](#method-1-separate-launches) have an easier rocket design and less specific orbit requirements, but are more tedious. A [single launch](#method-2-single-launcher) is faster, but requires skill in both rocket design and orbital maneuvers.-->

##Requirements

For part requirements, see the [General Satellite Design tutorial](../comsats/).

This tutorial assumes you have at least one mod that gives you orbital periods in-game. In order of decreasing precision, the best choices are Vessel Orbital Information Display, Kerbal Engineer Redux, and MechJeb.

###Satellite Design

In each 1.5-hour orbit, a satellite will experience up to 775&nbsp;seconds of darkness. Make sure you have enough battery power!

Ensure your satellite has an option for making very low-thrust burns (either RCS or a light engine).

Use a rocket with at least 5170 m/s of delta-V.

![Image showing how to look up orbital period in Kerbal Engineer](single_finalorbit.png "Getting an (almost) 1.5 hour orbit"){: .left}

##Mission Plan

###First Satellite

Begin by getting your satellite into an orbit with an apoapsis of 777&nbsp;km, then wait until it approaches apoapsis while over the Kerbal Space Center. This should not take more than a few orbits.

Circularize your orbit as usual. Your goal is to set your period as close to 1 hour 30 minutes as you can. As long as they're reasonably low, your eccentricity and inclination don't matter. The period, however, *does* matter: write down the exact value for later use.

###Remaining Satellites
{: .spacer}

![Image showing how the map view should look before the second satellite's transfer burn](single_2_align.png "A good transfer burn for the second of three satellites: apoapsis at KEO altitude, and 1947 km from the first satellite"){: .right}

The second and third satellites are the hardest, because you need to synchronize them both with your previous satellite(s) and with Kerbin's rotation. Launch each satellite into low Kerbin orbit. Use the map view to select the previous satellite in the network as the target. Set a maneuver node that reaches an apoapsis of 777&nbsp;km; a rendezvous marker should appear, telling you how far your satellite will be from the target at apoapsis. Adjust the maneuver node's position until this distance is close to 1947&nbsp;km. It doesn't have to be exact.

You also need to make sure that either the marker representing your targeted satellite or the marker representing your current satellite will be over KSC, after allowing for Kerbin to rotate by roughly one sixth of a rotation -- otherwise, you may be out of contact during the circularization burn. If necessary, delay the maneuver node by one orbit by right-clicking on it, clicking on the blue button to the lower right, then right-clicking again. After advancing by one orbit, you will have to fine-tune the separation between the satellites again.

If the maneuver node happens when you are out of contact with KSC, you will have to use the flight computer to handle the burn. Open the flight computer by clicking on the green calculator icon below the mission clock (if it's not green, you will have to wait until the next time you have a connection; another advantage of delaying the maneuver by a full orbit). Click "NODE" at the top of the window, then "EXEC" at the bottom. This will tell the computer to prepare for and execute the burn when the time comes. Once the satellite comes back over the horizon after the burn, you'll be able to control it directly again.

![A finished 4-satellite constellation](single_final.png){: .left}

Once your satellite reaches apoapsis, circularize your orbit. This time, your goal is to get **exactly** the same period as your first satellite (to within [high tolerances](#appendix-orbit-tolerances)). RCS can help with this, as can right-clicking on your engine and setting the throttle limiter to a very low value.

The fourth satellite is similar to the second and third, except you don't have to worry about synchronizing with KSC's rotation. If you set up the maneuver to reach apoapsis 1947&nbsp;km from the previous satellite, then you are guaranteed to have a working connection when you make the circularization burn.

<div></div>{: .spacer}

##Appendix: Orbit Tolerances

The precision with which you need to match the periods of your KEO satellites depends on how much drift between satellites you're willing to tolerate:

Period Error | Drift Rate             | Time to drift out of contact (38&deg;)
-------------|:----------------------:|:-------------------------:
0.01 seconds | 0.00067&deg; per orbit | 10 Earth years (33 Kerbin years)
0.1  seconds | 0.0067&deg; per orbit  | 360 Earth days (3 Kerbin years)
1    seconds | 0.067&deg; per orbit   | 36 Earth days (140 Kerbin days)
{: .data}

#Optional Steps

##Save File Tweaking

It is nearly impossible to give two satellites exactly the same orbital period, because the period will change whenever the satellite rotates (for example, to keep facing the sun over the course of the year). For the last word in satellite synchronization, you may wish to edit your save file. Some RemoteTech players see this as essential to get around game engine limitations, others see it as cheating. You will have to decide for yourself.

WARNING: editing the save file may corrupt the save if you are not careful. Make a copy of your save game before proceeding.
{:.alert .alert-danger}

Once you've synced up your satellites in-game as best you can, exit the game and open `saves/<Your Game Name>/persistent.sfs` in any text editor. Search for the name of your first satellite, ignoring any debris entries, then find a block a few lines down that looks like this:

    ORBIT
    {
        SMA = 1376599.67497195
        ECC = 0.00389366036050499
        INC = 0.233585703096135
        LPE = 43.7188835604928
        LAN = 344.168722710595
        MNA = 5.59207455474008
        EPH = 15705.4855810846
        REF = 1
    }

Copy the SMA value, and only the SMA value, to the similar blocks you will find in the entries for the other satellites. Your satellites will now have exactly the same orbital period.