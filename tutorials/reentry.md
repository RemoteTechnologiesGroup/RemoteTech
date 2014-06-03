---
title: Tutorial - Re-Entry Using the Flight Computer
layout: content
navbar: false
---

{% include banner.html %}

#Re-Entry Using the Flight Computer

{% include toc.html %}

This tutorial shows how to get the flight computer to execute a survivable re-entry. All numbers are for a Kerbin re-entry.

##Requirements

You must have researched [Flight Control](http://wiki.kerbalspaceprogram.com/wiki/Flight_Control), which unlocks basic probe technologies.

Following this tutorial does not *require* any mods other than RemoteTech, but the instructions assume you are using Deadly Reentry. Most of the precautions described here are unnecessary in the stock game.

Some mods will make the instructions in this tutorial easier to follow. Vessel Orbital Information Display, Kerbal Engineer Redux, and MechJeb will let you see the time to periapsis from the flight screen, not just the map view, making it easier to schedule actions in the flight computer. RealChutes and Smart Parts will let you replace timing-based triggers with altitude-based ones, taking a lot of the guesswork out of the re-entry sequence.

##Satellite Design

![IMAGE: assumed staging sequence](staging.png){: .left}

If you want a probe to be controllable during re-entry, you may want to keep a [Reflectron DP-10](../../guide/parts/#reflectron-dp-10) on the main probe body (requires low-orbit comsats, or a re-entry over the Kerbal Space Center). If you are using only antennas that break in atmosphere, you should bind either "Toggle" or "Activate" to an action group so that you can [schedule antenna deployment](#antenna-deployment) using the flight computer.

You may also want to bind other re-entry commands (such as antenna and solar panel retraction, or parachute deployment) to action groups for convenience.

This tutorial assumes you are using the default staging sequence: one stage to drop the engine, and a final stage to both drop the heat shield and deploy parachutes. Adapt the instructions to custom staging as appropriate.

##Pre-Reentry
{: .spacer}

Set up your re-entry trajectory as usual. When you are about 5-10 minutes away from periapsis, open the flight computer by clicking on the calculator icon in the upper left corner of the screen, then clicking ">>" in the window that pops up.

###Attitude Control

Point your heat shield into the airflow by clicking "GRD-", followed by "SRF". The flight computer state should read "surface retrograde". Unlike SAS, which loves to fight with parachutes, this is a safe attitude all the way from atmospheric entry to touchdown.

###Timing

The most difficult part of an automated re-entry is knowing when re-entry heating begins and ends. This depends on both the angle of your approach and on whether you are using stock aerodynamics or Ferram Aerospace Research (FAR). If you have FAR installed, then your tonnage also matters: heavier spacecraft slow to safe speeds later than light spacecraft. You may need to experiment with the timing for new spacecraft or for other planets (quicksave is your friend).

The most convenient time to compare re-entry sequencing to is the time of your orbit's periapsis before you enter the atmosphere. The table below shows when heating ends, compared to the time of periapsis. For example, if you are entering from LKO, playing with stock aerodynamics, and the map view says you will reach periapsis in 10 minutes, then your probe will be out of danger 8:45 from now.

{::comment}
For stock LKO, serious heating starts at -4:10. Look up for other mod combinations?
{:/comment}

Trajectory                       | Stock aerodynamics | FAR installed (2&nbsp;ton probe)
---------------------------------|:------------------:|:-------------------------:
From LKO to 30&nbsp;km periapsis |  -1:15             | +2:00
From Mun to 30&nbsp;km periapsis |                    | +1:15
{: .data}

![IMAGE: pre-reentry flight queue with parachutes](preflight_1.png){:.right}

###Parachutes

Your highest priority should be scheduling parachute deployment. Use the table above to pick a time just after major heating ends. Once you've decided when you want to deploy parachutes, type in the scheduled time (minus, say, 10-20 seconds) in the box in the lower right corner of the screen and hit enter. Feel free to pause the game while you do the math.

Wait until the time to periapsis advances by your 10-20 second margin, then hit the staging key. Type "0" followed by enter once you've scheduled parachute deployment, or your other commands will happen too late!

**Example:** if your orbit should hit periapsis in 8:53, and you want to deploy parachutes 1:00 before periapsis, type "7m40s" into the text box and hit enter. Wait until the time to periapsis equals 8:40, then hit the staging key to schedule the parachute stage.

###Engine Jettison

When you load a game or switch vessels, Kerbal Space Program will sometimes reset the ship's staging state to "prelaunch", and sometimes not. Because the staging indicator does not work in RemoteTech, you can't tell when this has happened and when it hasn't. Therefore, it's safest to jettison the engine manually, in real time. If the first staging key press doesn't jettison the engine (instead taking you from prelaunch to the engine stage), the second will. Once you've jettisoned the engine, you *know* that the next staging command will deploy parachutes, as intended.

![IMAGE: image of probe parachuting down, with a deployed antenna](chute.png "Benefits of a good re-entry queue."){:.left}

###Final Steps

Retract any solar panels just before you hit the atmosphere, and any non-atmospheric antennas before you reach 40 km. If you are out of radio contact at atmosphere entry you will need to schedule the retraction(s) with the flight computer, just like with the [parachutes](#parachutes). The time at which you hit the atmosphere depends on your trajectory -- 8-10 minutes before periapsis if entering from LKO, 4-5 minutes before periapsis if entering from high orbit, and 1-2 minutes before periapsis if returning from the Mun.

Good luck, and see you on the other side!

#Optional Steps
{: .spacer}

![IMAGE: full pre-reentry flight queue](preflight_2.png){:.left}

##Antenna Deployment

Most antennas can't be deployed in the lower atmosphere unless the ship is going less than 100 m/s (70 m/s for the [Communotron 32](../../guide/parts/#communotron-32)). If you rely on such an antenna for communication, you won't be able to use it until well after parachute (pre)deployment. 

If you want to use your antenna on the surface, you will need to schedule its activation during the pre-reentry stage. Schedule antenna deployment for 1 minute (with stock aerodynamics) or 2 minutes (with FAR) after parachute deployment, using the manual delay method you used to set the [parachutes](#parachutes).

**Important:** because you can't right click an antenna to deploy it while it's already deployed, you will need to use an action group to send the deploy command.
