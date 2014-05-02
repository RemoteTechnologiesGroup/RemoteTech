---
title: Tutorial - Kerbosynchronous Equatorial Orbit
layout: content
navbar: false
---

{% include banner.html %}

**Oh shit son!** This page is still under development!
{: .alert .alert-danger}

#Creating a Kerbosynchronous Satellite Network

{% include toc.html %}

This tutorial covers how to launch three or four equally spaced satellites into kerbosynchronous equatorial orbit (KEO). It assumes you have no pre-existing satellite network. This network is recommended for players interested in role-playing or in a challenge. Since in RemoteTech dishes can be pointed and signals rerouted instantaneously, there is no in-game advantage to keeping communications satellites stationary, and a lower orbit is both easier to achieve and better for interplanetary communications.

The tutorial gives two methods for creating a KEO network. Separate launches have an easier rocket design and less specific orbit requirements, but are more tedious. A single launch is faster, but requires skill in both rocket design and orbital maneuvers.

##Requirements

For part requirements, see the [General Satellite Design tutorial](#).

This tutorial assumes you have at least one mod that gives you orbital periods in-game. In order of decreasing accuracy, the best choices are Vessel Orbital Information Display, Kerbal Engineer Redux, and MechJeb.

###Satellite Design

Ensure your satellite has an option for making very low-thrust burns (either RCS or a light engine).

In addition to any antennas pointed at moons, planets, or vessel groups, you will need antennas for the following two roles:

* Communicating with Kerbin's surface and low Kerbin orbit: KEO is located 2869&nbsp;km above the surface, or 3469&nbsp;km from Kerbin's center. The farthest point on Kerbin's surface from any satellite will be 3420&nbsp;km away, near the poles. Therefore, the best antennas for this role are the Communotron&nbsp;32 or the Comms DTS-M1.
* Communicating with the satellites' siblings: KEO satellites in an equilateral triangle formation will be located 6008&nbsp;km apart, requiring the use of the Comms DTS-M1 to maintain contact. KEO satellites in a square formation will be only 4906&nbsp;km apart, allowing the Communotron&nbsp;32 to fill this role if you have the necessary technology.

##Method 1: Separate Launches

Use a rocket with at least 5615 m/s of delta-V, preferably a little more.

###First Satellite

Begin by launching your satellite into low Kerbin orbit. If you are using the Comms DTS-M1 as your Kerbin communication antenna, target it at Mission Control (not Kerbin) to maintain contact whenever your satellite is above the horizon.

You now need to schedule a maneuver node for the KEO transfer orbit. You want to set your apoapsis at about 2870&nbsp;km (it doesn't have to be exact), and schedule the maneuver node so that you reach apoapsis while the satellite has a line of sight to the Kerbal Space Center (KSC). The transfer orbit will take 2 hours 46 minutes, so you want to account for Kerbin (and KSC) rotating by 165&deg; in the time it takes you to reach apoapsis. The best position for your first satellite is 60&deg; away from KSC's longitude (for a 3-satellite network) or 45&deg; away from KSC (for a 4-satellite network). So the best place to put the maneuver node is:

Network Size | 1st Satellite East of KSC | 1st Satellite West of KSC
-------------|:-------------------------:|:-------------------------:
3 satellites | 105&deg; before KSC       | 135&deg; after KSC
4 satellites | 120&deg; before KSC       | 150&deg; after KSC
{: .data}

Again, placement doesn't have to be exact. As long as you're within about 20&deg; or so, the worst that will happen is that your network will look a little lopsided.

If you don't have a pre-existing satellite network, all four possible burns will be out of contact with KSC. Once you're satisfied with your maneuver node, open the flight computer by clicking on the green calculator icon below the mission clock (if it's not green, you will have to wait until you next fly over KSC). Click "NODE" at the top of the window, then "EXEC" at the bottom. This will tell the computer to prepare for and execute the burn when the time comes. Once the satellite comes back over the horizon after the burn, you'll be able to control it manually again.

Once you are approaching apoapsis (and are in contact with KSC, thanks to your careful maneuver timing), circularize your orbit as usual. Your goal is to set your period as close to 6 Earth hours (1 Kerbin day) as you can . It does not matter if your orbit is perfectly circular; it does not matter if your orbit is perfectly uninclined. **The orbital period is what makes a kerbosynchronous orbit work.** Write down your final period for later use.

Once you've achieved your final orbit, you're done. If you're using a dish antenna to contact the KSC, switch its target to Kerbin. This will let the remaining satellites in the network benefit from your first satellite's superior coverage.

###Remaining Satellites

Fortunately, the first satellite was the hardest, because now you can use Kerbal Space Program's built-in tools to place your remaining satellites. Launch each satellite into low Kerbin orbit as before. Set up all your satellite's sibling connections while in low Kerbin orbit, making sure to have the previous satellites point a dish at your new satellite as well. This will let you keep in touch with your new satellite for a much larger portion of its orbit than if it relied on a direct line to KSC.

Use the map view to select the previous satellite in the network as the target. Set a maneuver node that reaches an apoapsis of 2870&nbsp;km; a rendezvous marker should appear, telling you how far your satellite will be from the target at apoapsis. Adjust the maneuver node's position until this distance is close to 6008&nbsp;km (for a 3-satellite network) or 4906&nbsp;km (for a 4-satellite network). If the maneuver node is in one of your network's blind spots, use the flight computer to handle the burn.

Once your satellite reaches apoapsis, circularize your orbit. This time, your goal is to get **exactly** the same period as your first satellite (to within [high tolerances](#appendix-orbit-tolerances)). It is more important that your satellites stay in formation with each other than that they stay synchronized with Kerbin's rotation. RCS can help with this, as can right-clicking on your engine and setting the throttle limiter to a very low value.

Once you are happy with your orbit, double-check that all dishes have the targets they need to, and that all *previously* launched satellites that need to target your new satellite have done so. You're done!

##Method 2: Single Launcher

For this method, you want to stack all your satellites on top of your rocket, with a stack separator (NOT a decoupler) between each. Configure your staging so that the satellites are released from top to bottom.

Use a rocket with at least 5400 m/s of delta-V, not including any fuel in the satellites. Make sure each satellite has at least 435 m/s, preferably a little more.

###Transfer orbit 1

Work in progress: find a more efficient way to get into the desired orbit
{: .alert .alert-danger}

Begin by launching into an elliptical orbit with an apoapsis of 2870&nbsp;km (it doesn't have to be exact). If you are using the Comms DTS-M1 as your Kerbin communication antenna, target the Kerbin antenna on each of the satellites at Mission Control (not Kerbin) to maintain contact whenever your satellite is above the horizon.

Wait until the rocket approaches apoapsis while in line of sight from KSC. A position directly over KSC will make for a less robust network; a good position would be 40-80&deg; away (for a 3-satellite network) or 10-80&deg; away (for a 4-satellite network), because these positions give two satellites in the network direct lines of sight to KSC.

Once you are approaching apoapsis, deploy whatever antennas you need for your first satellite and your rocket to be able to maintain contact with KSC *independently*, then release the satellite and switch to it. 

Once your satellite reaches apoapsis, circularize its orbit using its built-in engine. Your goal is to set your period as close to 6 Earth hours (1 Kerbin day) as you can. It does not matter if your orbit is perfectly circular; it does not matter if your orbit is perfectly uninclined. **The orbital period is what makes a kerbosynchronous orbit work.** Write down your final period for later use.

Once you've achieved your final orbit, all that's left is setting up the antennas. If you're using a dish antenna to contact the KSC, switch its target to Kerbin. This will let the rocket and the remaining satellites benefit from your first satellite's.

###Transfer orbit 2

Switch back to the rocket, which should still be close to apoapsis, and burn prograde until the orbital period rises to 4 hours (for a 3-satellite network; 1225&nbsp;km periapsis) or 4.5 hours (for a 4-satellite network; 1658&nbsp;km periapsis). Up to a minute of error won't matter, since you only need to worry about the drift over 3-4 orbits.

###Deployment

The rocket's orbital period is set so that it will next return to apoapsis 120&deg; (for a 3-satellite network) or 90&deg; (for a 4-satellite network) behind the satellite you just launched. Every time the rocket approaches apoapsis, release another satellite. You may need to make some small (1 m/s or less) corrections to your rocket's orbit to keep the period the same after the satellite pushes off. Do so, then switch to the satellite.

Circularize the satellite's orbit at apoapsis. This time, your goal is to get **exactly** the same period as your first satellite (to within [high tolerances](#appendix-orbit-tolerances)). It is more important that your satellites stay in formation with each other than that they stay synchronized with Kerbin's rotation. RCS can help with this, as can right-clicking on your main engine and setting the throttle limiter to a very low value.

Don't forget to set up the antennas for each satellite, including two-way connections between siblings!

##Appendix: Orbit Tolerances

The precision with which you need to match the periods of your KEO satellites depends on how much drift between satellites you're willing to tolerate:

Period Error | Drift Rate             | Time to drift 20&deg;
-------------|:----------------------:|:-------------------------:
0.01 seconds | 0.00017&deg; per orbit | 82.2 Earth years (282 Kerbin years)
0.1  seconds | 0.0017&deg; per orbit  | 8.2 Earth years (28.2 Kerbin years)
1    seconds | 0.017&deg; per orbit   | 300 Earth days (2 Kerbin years 347 days)
5    seconds | 0.083&deg; per orbit   | 60 Earth days (240 Kerbin days)
{: .data}

#Optional Steps

##Save File Tweaking

It is nearly impossible to give two satellites exactly the same orbital period, because the period will change whenever the satellite rotates (for example, to keep facing the sun over the course of the year). For the last word in satellite synchronization, you may wish to edit your save file. Some RemoteTech players see this as essential to get around game engine limitations, others see it as cheating. You will have to decide for yourself.

Once you've synched up your satellites in game as best you can, exit the game and open saves/&lt;Your Game Name&gt;/persistent.sfs in any text editor. Search for the name of your first satellite, then find a block a few lines down that looks like this:

    ORBIT
    {
        SMA = 3468749.92670803
        ECC = 0.00031529434862492
        INC = 0.314137942694019
        LPE = 341.433303970299
        LAN = 42.6228520343802
        MNA = 3.88805040897349
        EPH = 8196339.4103384
        REF = 1
    }

Copy the SMA value, and only the SMA value, to the similar blocks you will find in the entries for the other satellites. Your satellites will now have exactly the same orbital period.