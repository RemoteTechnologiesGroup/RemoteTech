---
title: Tutorial - Re-Entry Using the Flight Computer
layout: content
navbar: false
---

{% include banner.html %}

**Oh shit son!** This page is still under development!
{: .alert .alert-danger}

#Re-Entry Using the Flight Computer

{% include toc.html %}

This tutorial shows how to get the flight computer to execute a re-entry by itself. All numbers are for a Kerbin re-entry.

##Requirements

You must have researched [Flight Control](http://wiki.kerbalspaceprogram.com/wiki/Flight_Control), which unlocks basic probe technologies.

Following this tutorial does not *require* any mods other than RemoteTech, but the instructions assume you are using Deadly Reentry. Most of the precautions described here are unneccessary in the stock game.

Some mods will make the instructions in this tutorial easier to follow. Kerbal Engineer and MechJeb will let you see the time to periapsis from the flight screen, not just the map view, making it easier to schedule actions in the flight computer. RealChutes and Smart Parts will let you replace timing-based triggers with altitude-based ones, taking a lot of the guesswork out of the re-entry sequence.

##Satellite Design

If you want a probe to be controllable during re-entry, you may want to keep a [Reflectron DP-10](../../guide/parts/#reflectron-dp-10) on the main probe body (requires low-orbit comsats, or a re-entry over KSC). If you are using only antennas that break in atmosphere, you should bind either "Toggle" or "Activate" to an action group so that you can schedule antenna deployment using the flight computer.

You may also want to bind other re-entry commands (such as antenna and solar panel retraction, or parachute deployment) to action groups for convenience.

This tutorial assumes you are using the default staging sequence: one stage to drop the engine, and a final stage to both jettison the heat shield and deploy parachutes. Adapt the instructions to custom staging as appropriate.

![IMAGE: assumed staging sequence](staging_reentry.png)

##Pre-Reentry

Set up your re-entry trajectory as usual. When you are about 5-10 minutes away from periapsis, open the flight computer by clicking on the calculator icon in the upper left corner of the screen, then clicking ">>" in the window that pops up.

###Attitude Control

Point your heat shield into the airflow by pressing "SRF", followed by "GRD-". The flight computer state should read "surface retrograde". Unlike SAS, which loves to fight with parachutes, this is a safe attitude all the way from atmospheric entry to touchdown.

###Parachutes

Your highest priority should be scheduling parachute deployment. The best timing depends on your trajectory and spacecraft mass. For a 2-ton Mun probe aiming for periapsis at 30-40 km, deploying parachutes between a minute and 1:30 after the expected time to periapsis (visible in map view if you mouse over the "Pe" marker) works well. For shallower trajectories (like re-entry from low orbit), pick a later time.

Once you've decided when you want to deploy parachutes, type in the scheduled time (plus some leeway) in the box in the lower right corner of the screen and hit enter. Then press the staging key when the time you typed equals the time to periapsis plus the time after periapsis you decided on. Type "0" followed by enter once you've scheduled parachute deployment, or your other commands will happen too late!

**Example:** if your orbit should hit periapsis in 4:53, and you want to deploy parachutes 1:15 after periapsis, type "5m45s" into the text box and hit enter. Wait until the time to periapsis equals 4:30, then hit the staging key to schedule the parachute stage.

###Engine Jettison

When you load a game or switch active vessels, Kerbal Space Program will sometimes reset the ship's staging state to "prelaunch", and sometimes not. Because the staging indicator does not work in RemoteTech, you can't tell when this has happened and when it hasn't. Therefore, it's safest to jettison the engine manually. If the first staging keypress doesn't jettison the engine (instead taking you from prelaunch to the engine stage), the second will. Once you've jettisoned the engine, you *know* that the next staging command will deploy parachutes, as intended.

![IMAGE: full pre-entry flight queue](queue_reentry.png)

###Final Steps

Retract any solar panels just before you hit the atmosphere, and any non-atmospheric antennas before you reach 40 km. If you are out of radio contact at atmosphere entry you will need to schedule the retraction(s) with the flight computer, just like with the [parachutes](#parachutes). 1-2 minutes before periapsis works well, though shallow re-entries from low orbit will hit the atmosphere earlier.

Good luck, and see you on the other side!

#Optional Steps

##Antenna Deployment

Most antennas can't be deployed in the atmosphere unless the ship is going less than 100 m/s (70 m/s for the [Communotron 32](../../guide/parts/#communotron-32)). If you rely on such an antenna for communication, you won't be able to use it until well after parachute (pre)deployment. 

If you want to use your antenna on the surface, you will need to schedule its activation during the pre-reentry stage. Schedule antenna deployment for 1-2 minutes after parachute deployment, using the manual delay method you used to set the [parachutes](#parachutes).

**Important:** because you can't right click an antenna to deploy it while it's already deployed, you will need to use an action group to send the deploy command.
