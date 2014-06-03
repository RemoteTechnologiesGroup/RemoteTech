---
title: Tutorial - Medium-Altitude Omni Network
layout: content
navbar: false
---

{% include banner.html %}

#Creating an Omni-Only Network

{% include toc.html %}

This tutorial covers how to create a three-satellite network using no antenna more powerful than the [Communotron 16](../../guide/parts/#communotron-16). Players who have access to more powerful antennas can feel free to include them in their satellite design, while leaving the Communotron 16 as the backbone of the constellation.

The requirement that the satellites be connected by Communotron 16's forces the altitude of the satellites to be between 600&nbsp;km (below which the satellites are below each others' horizons) and 840&nbsp;km (above which the satellites are out of range of each other). We'll go with an altitude of 776.6&nbsp;km, which has a convenient orbital period of 1.5&nbsp;hours.

The tutorial gives two methods for creating an omni network. [Separate launches](#method-1-separate-launches) have an easier rocket design and less specific orbit requirements, but are more tedious. A [single launch](#method-2-single-launcher) is faster, but requires skill in both rocket design and orbital maneuvers.

##Requirements

For part requirements, see the [General Satellite Design tutorial](../comsats/).

This tutorial assumes you have at least one mod that gives you orbital periods in-game. In order of decreasing precision, the best choices are Vessel Orbital Information Display, Kerbal Engineer Redux, and MechJeb.

###Satellite Design

In each 1.5-hour orbit, a satellite will experience up to 775&nbsp;seconds of darkness. Make sure you have enough battery power!

Ensure your satellite has an option for making very low-thrust burns (either RCS or a light engine).

##Method 1: Separate Launches

Use a rocket with at least 5170 m/s of delta-V.

![Image showing how to look up orbital period in Kerbal Engineer](single_finalorbit.png "Getting an (almost) 1.5 hour orbit"){: .left}

###First Satellite

Begin by getting your satellite into an orbit with an apoapsis of 777&nbsp;km, then wait until it approaches apoapsis while over the Kerbal Space Center. This should not take more than a few orbits.

Circularize your orbit as usual. Your goal is to set your period as close to 1 hour 30 minutes as you can. As long as they're reasonably low, your eccentricity and inclination don't matter. The period, however, *does* matter: write down the exact value for later use.

###Remaining Satellites
{: .spacer}

![Image showing how the map view should look before the second satellite's transfer burn](single_2_align.png "A good transfer burn for the second of three satellites: apoapsis at KEO altitude, and 2384 km from the first satellite"){: .right}

The second satellite is the hardest of the three, because you need to synchronize it both with your first satellite and with Kerbin's rotation. Launch the satellite into low Kerbin orbit. Use the map view to select the first satellite as the target. Set a maneuver node that reaches an apoapsis of 777&nbsp;km; a rendezvous marker should appear, telling you how far your satellite will be from the target at apoapsis. Adjust the maneuver node's position until this distance is close to 2384&nbsp;km. It doesn't have to be exact, but it needs to be closer than 50&nbsp;km to the exact value.

You also need to make sure that either the marker representing your first satellite or the marker representing your second satellite will be over KSC, after allowing for Kerbin to rotate by roughly one sixth of a rotation -- otherwise, you will be out of contact during the circularization burn. If necessary, delay the maneuver node by one orbit by right-clicking on it, clicking on the blue button to the lower right, then right-clicking again. After advancing by one orbit, you will have to fine-tune the separation between the satellites again.

If the maneuver node happens when you are out of sight of KSC, you will have to use the flight computer to handle the burn. Open the flight computer by clicking on the green calculator icon below the mission clock (if it's not green, you will have to wait until the next time you have a connection; another advantage to delaying the maneuver by a full orbit). Click "NODE" at the top of the window, then "EXEC" at the bottom. This will tell the computer to prepare for and execute the burn when the time comes. Once the satellite comes back over the horizon after the burn, you'll be able to control it directly again.

![A finished 3-satellite constellation](single_final.png){: .left}

Once your satellite reaches apoapsis, circularize your orbit. This time, your goal is to get **exactly** the same period as your first satellite (to within [high tolerances](#appendix-orbit-tolerances)). RCS can help with this, as can right-clicking on your engine and setting the throttle limiter to a very low value.

The third satellite is similar to the second, except you don't have to worry about synchronizing with KSC's rotation. If you set up the maneuver to reach apoapsis 2380&nbsp;km from either of the two satellites (on the side that does not yet have a satellite, of course), then you are guaranteed to have a working connection when you make the circularization burn.

<div></div>{: .spacer}

![Screenshot of a stacked satellite launcher](multi_rocket.png "Stack of three satellites"){: .right}

##Method 2: Single Launcher

For this method, you want to stack all your satellites on top of your rocket, with a stack separator (NOT a decoupler) between each. Configure your staging so that the satellites are released from top to bottom.

Use a rocket with at least 4870 m/s of delta-V, not including any fuel in the satellites. Make sure each satellite has at least 310 m/s.

###Transfer orbit 1

Begin by launching into an elliptical orbit with an apoapsis of 2870&nbsp;km (it doesn't have to be exact). If you are using the [Comms DTS-M1](../../guide/parts/#comms-dts-m1) as your Kerbin communication antenna, target the Kerbin antenna on each of the satellites at Mission Control (not Kerbin) to maintain contact whenever your satellite is above the horizon.

![A screenshot of a good position to drop the first satellite](multi_1_align.png){: .left}

Wait until the rocket approaches apoapsis while in line of sight from KSC. A position directly over KSC will make for a less robust network; a good position would be 40-80&deg; away (for a 3-satellite network) or 10-80&deg; away (for a 4-satellite network), because these positions give two satellites in the network direct lines of sight to KSC.

Once you are approaching apoapsis, deploy whatever antennas you need for your first satellite and your rocket to be able to maintain contact with KSC *independently*, then release the satellite and switch to it. 

Once your satellite reaches apoapsis, circularize its orbit using its built-in engine. Your goal is to set your period as close to 6 Earth hours (1 Kerbin day) as you can. It does not matter if your orbit is perfectly circular; it does not matter if your orbit is perfectly uninclined. **The orbital period is what makes a keosynchronous orbit work.** Write down your exact final period for later use.

Once you've achieved your final orbit, all that's left is setting up the antennas. If you're using a dish antenna to contact the KSC, switch its target to Kerbin. This will let the rocket and the remaining satellites benefit from your first satellite's superior coverage.

![An example of a transfer orbit](multi_xfer.png "A 4.5-hour transfer orbit immediately after releasing the first satellite"){: .left}

###Transfer orbit 2

Switch back to the rocket, which should still be close to apoapsis, and burn prograde until the orbital period rises to 4 hours (for a 3-satellite network; 1225&nbsp;km periapsis) or 4.5 hours (for a 4-satellite network; 1658&nbsp;km periapsis). Up to a minute of error won't matter, since you only need to worry about the drift over 3-4 orbits.

###Deployment

The rocket's orbital period is set so that it will next return to apoapsis 120&deg; (for a 3-satellite network) or 90&deg; (for a 4-satellite network) behind the satellite you just launched. Every time the rocket approaches apoapsis, release another satellite. If you are using dishes rather than the [Communotron 32](../../guide/parts/#communotron-32) to set up connections between keostationary satellites, you need to take care with the dish targets, especially if you have not yet researched [Unmanned Tech](http://wiki.kerbalspaceprogram.com/wiki/Unmanned_Tech) (which unlocks a 3&nbsp;km omni antenna for all probe cores).

Before releasing the satellite:

* Both the rocket and the satellite should have a dish pointed at the *previous* satellite to be released.
* The previous satellite should have a dish pointed at the rocket.

Immediately after releasing the satellite:

* The rocket should point a dish at the satellite that was just released. The dish that was pointing at the previous satellite may be used, although this may temporarily break the rocket's connection to Mission Control.
* The previous satellite should retarget the dish pointing at the rocket to the satellite that was just released.
* The satellite just released should point an unused dish at the rocket.
* The satellite should retarget its Mission Control dish to point to Kerbin.

This sequence of steps will ensure that each satellite has a two-way link to the satellite just before it, and that the rocket always has a two-way link to the last satellite in the chain.

![To build up a network while out of sight of KSC, make sure each satellite has a two-way connection to its neighbors, and that the last satellite has a two-way connection to the launch rocket](multi_3_setup.png "A network with 2 out of 4 satellites released."){:.pairedimages}
![Screenshot of a 4-satellite network](multi_final.png){:.pairedimages}

Circularize the satellite's orbit at apoapsis. This time, your goal is to get **exactly** the same period as your first satellite (to within [high tolerances](#appendix-orbit-tolerances)). It is more important that your satellites stay in formation with each other than that they stay synchronized with Kerbin's rotation. RCS can help with this, as can right-clicking on your main engine and setting the throttle limiter to a very low value.

Once you release the second-to-last satellite, the rocket and the final satellite will be essentially the same thing. Have the first satellite in the sequence and the rocket/final satellite point dishes at each other, completing a ring of connections.

##Appendix: Orbit Tolerances

The precision with which you need to match the periods of your KEO satellites depends on how much drift between satellites you're willing to tolerate:

Period Error | Drift Rate             | Time to drift out of contact (6&deg;)
-------------|:----------------------:|:-------------------------:
0.01 seconds | 0.00067&deg; per orbit | 1 Earth year 200 days (5 Kerbin years)
0.1  seconds | 0.0067&deg; per orbit  | 60 Earth days (200 Kerbin days)
1    seconds | 0.067&deg; per orbit   | 6 Earth days (20 Kerbin days)
{: .data}

#Optional Steps

##Save File Tweaking

It is nearly impossible to give two satellites exactly the same orbital period, because the period will change whenever the satellite rotates (for example, to keep facing the sun over the course of the year). For the last word in satellite synchronization, you may wish to edit your save file. Some RemoteTech players see this as essential to get around game engine limitations, others see it as cheating. You will have to decide for yourself.

Once you've synched up your satellites in-game as best you can, exit the game and open saves/&lt;Your Game Name&gt;/persistent.sfs in any text editor. Search for the name of your first satellite, ignoring any debris entries, then find a block a few lines down that looks like this:

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