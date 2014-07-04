---
title: Flight Computer
layout: content
---

{% include banner.html %}

#The Flight Computer

{% include toc.html %}

![IMAGE: flight computer window, with a simple queue](flightcomputer.png){: .right}

The flight computer lets you make deep-space maneuvers or schedule actions while dealing with signal delay or temporary breaks in communication. It is **not** a standalone autopilot, and cannot carry out launches, landings, dockings, or other complex maneuvers. KSP already has several excellent autopilot mods, including MechJeb and kOS, and we are working to make sure that these mods are compatible with RemoteTech.

The basic flight computer window shows only the controls for operating it; clicking ">>" will display the computer's current state as well as a queue of any "instant" commands sent to it. Instant commands are anything you normally do with a single click or key press, including right-click actions, staging commands, action groups, and anything with a toggle key (RCS, SAS, landing gear...). They do not include slewing, translation, changing the throttle (or cutting it with the "X" key), or anything else that might involve holding down a key.

An instant command may be canceled by clicking the "X" button next to the command. Non-instant commands cannot be canceled.

##Signal delay

All commands, instant and otherwise, are subject to signal delay, whether or not the flight computer's window is open. Instant commands will be shown in the queue along with the amount of time remaining until the probe receives the command. Once the time drops to zero, the command will be executed. Cancellations also count as commands and appear in the queue, along with a signal delay. Unless you are using manual delay (see below), there is no way to get a cancel command to the ship before it starts executing the original command.

If a command pulls up a window, you may click buttons in the window without signal delay. For example, if you have a two-minute delay and activate a science experiment, you have to wait only two minutes, not four, before the probe discards, saves, or transmits the data.

##Manual delay

The text box in the lower right corner of the computer window lets you choose to delay an action by a specific amount. This is useful if you expect to go out of contact, but want the probe to carry out a command while out of reach. To set the manual delay, type a delay into the box **and hit enter**. Merely typing the delay does nothing. Numbers with no units will be interpreted as seconds; otherwise, you need to give exact units -- "1m20s" will be parsed as one minute and twenty seconds, while "1m20" will be treated as bad input. Once a manual delay is set, any command, instant or not, will be delayed, whether the flight computer window is open or not. **Remember to set your delay to zero when you're done!**

If the manual delay is less than the signal delay, the delay will be ignored -- the probe will execute the command as soon as it gets it, just as if the delay were zero. If the manual delay is more than the signal delay, the command will be delayed by the given amount from when the command was *sent*, not received. The computer queue will list two delays: the first is the signal delay, while the second is the amount the computer will wait after it gets the signal.

**Example:** a probe is ten light-minutes away and about to pass behind a planet for a burn, which is scheduled 20 minutes from now. Type "20m" into the delay box and hit enter to set the delay to 20 minutes. Then issue a command to (for example) point retrograde. After a few seconds the flight queue will read "9m56s+10m00s", indicating that the signal will take just under ten minutes to reach the ship, followed by another ten minutes before the ship acts on it.

Cancellations are not affected by manual delay, so a command will be removed from the queue as soon as the cancellation reaches the ship. This makes manual delay helpful if you want to double-check a complex sequence of commands before they are executed.

##Autopilot commands

The buttons on the left side of the screen control a simple autopilot. All buttons are instant actions, so they are shown in the queue and may be canceled. Like all commands, they are subject to signal delay and manual delay. The buttons are as follows:

###Attitude Control

![IMAGE: layout of the attitude controls](flightcomputer_att.png){: .left}

Pointing a ship manually with several minutes of lag is nearly impossible, so the computer can be programmed to hold a particular position. Choosing any attitude will override the previous attitude command. The path the ship takes in pointing toward a new position can be very roundabout, so be sure to allow plenty of time to turn the ship.

There are six basic directions (+/- GRD, RAD, and NRM), corresponding roughly to the six maneuver node axes. The exact meaning of each direction depends on the reference frame, chosen with one of four buttons in the flight computer window: 

<div></div>{:.spacer}

ORB
: Directions are relative to the ship's orbital motion. This is the default if no reference frame is selected, and the only reference frame in which the six directions correspond *exactly* to those from the game's maneuver node editor.

SRF
: Directions are relative to the ship's surface motion.

RVEL
: Directions are relative to the ship's motion past the current target.

TGT
: Directions are relative to the direction towards the current target.

Direction   | ORB Frame (Default)        | SRF Frame                                         | RVEL Frame                                               | TGT Frame
------------|----------------------------|---------------------------------------------------|----------------------------------------------------------|-------------------
GRD+        | Towards orbital velocity   | Towards surface velocity                          | Towards target relative velocity                         | Towards target
GRD-        | Away from orbital velocity | Away from surface velocity                        | Away from target relative velocity                       | Away from target
RAD+        | Outward from orbit         | Outward from surface trajectory                   | Perpendicular to relative velocity, in orbital plane     | Perpendicular to target
RAD-        | Inward from orbit          | Inward from surface trajectory                    | Perpendicular to relative velocity, in orbital plane     | Perpendicular to target
NRM+        | Up, out of orbital plane   | North (south) from eastward (westward) trajectory | Perpendicular to relative velocity, out of orbital plane | Perpendicular to target
NRM-        | Down, out of orbital plane | South (north) from eastward (westward) trajectory | Perpendicular to relative velocity, out of orbital plane | Perpendicular to target
{:.data .shadecol .sidehead}

Clicking on a direction once it's already selected will revert to GRD+. Clicking on a reference frame once it's already selected will turn off the flight computer's attitude control.

The other attitude options, which don't work with the six direction buttons, are:

KILL
:   This attempts to hold the ship in a fixed direction. It is useful for maintaining attitude in the middle of a sequence.

NODE
:   This attempts to face the direction required for the next maneuver node.

CUSTOM
:   This attempts to keep the ship in a specific pitch, heading, and roll, as chosen by the options below the six direction buttons. If possible, set the desired pitch, heading, and roll before clicking CUSTOM, as otherwise the ship's desired pointing will update as you type. The exact fields are:

    * PIT: the pitch angle, in degrees. 0 means to point the nose level (which, in a high orbit, may still be well above the horizon), 90 means to point it straight up.
    * HDG: the angle relative to north, in degrees. 0 means to point north, 90 means to point east.
    * RLL: the angle to rotate around the ship's control axis, in degrees. 0 means that the horizon will be at the bottom of the navball. 90 means that the horizon will be on the right side of the navball.

###Executing Maneuver Nodes

Pressing the "EXEC" button causes the ship to wait until it approaches a maneuver node, then slew to the maneuver position and start the engine for a precalculated amount of time. Once the length of the burn has passed, the flight computer will shut off the engine. Automatic node execution overrides any attitude control commands, and once the execution is done the flight computer switches off. The player may need to schedule post-burn commands such as KILL or toggle SAS to keep the ship pointed.

Because node execution does not wait for the ship to face the node before turning on the engine, players are *strongly* encouraged to use a NODE attitude command well before executing a maneuver node. The management is not responsible for any burns that had the opposite of their intended effect.

Unlike most commands, EXEC ignores manual delays -- the time of the burn is set by the location of the maneuver node. If the time to the node is less than the signal delay, the execution command won't be sent.

**Note:** the flight computer does not understand staging, and will continue to count down to the end of the burn even if the current fuel tanks are empty. Schedule any staging commands separately by using manual delay.

![IMAGE: a simple example of a manual burn](manualburn.png){: .right}

###Manual Burns

Automatic node execution is convenient, but has a few limitations: it needs a well-defined maneuver node, making it difficult to do small velocity corrections, and it ignores any nodes after the first. For more control over burns, players may set the burn parameters by hand.

First, set a manual delay for the start of the burn. Adjust the throttle slider to the desired level. In the box, type either the desired duration of the burn, or the desired delta-V (e.g., "100 m/s"). Numbers without units are interpreted as burn time in seconds. Clicking "BURN" will add the burn to the command queue, with signal delay and whatever manual delay was set.

Just like automatic node execution, the manual burn doesn't include staging or attitude control. Those commands must be scheduled separately.
