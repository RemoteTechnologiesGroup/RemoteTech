RemoteTech
==========

Community developed continuation of Kerbal Space Program's RemoteTech mod.

## Introduction

RemoteTech is a modification for Squad’s ‘Kerbal Space Program’ (KSP) which overhauls the unmanned space program. It does this by requiring unmanned vessels have a connection to Kerbal Space Center (KSC) to be able to be controlled. This adds a new layer of difficulty that compensates for the lack of live crewmembers.

Your unmanned vessels require an uplink to a command station to be controlled. Mission Control at the Kerbal Space Center delivers a vast omnidirectional antenna that will reach your vessels up to and slightly beyond Minmus.

## Mechanics

###Antennas

Using antennas, it is now possible to set up satellite networks to route your control input. Unlike in stock KSP, antennas will no longer activate or deactivate automatically; you must order an antenna to activate by right-clicking on it. There are two classes of antennas: ‘Dishes’ and ‘Omnidirectionals’.

####Dishes

Dishes are antennas that must be instructed what direction to point at. They do not need to be physically turned; you need merely select a target from a list. Dishes tend to be used for long range communication and come with a cone of vision (which is narrower for longer-range antennas). If the dish is pointed at a planet or moon, anything inside this cone can achieve a connection with the dish.

##### Omnidirectionals

Omni antennas radiate in every direction equally, and as such do not require you to target them at anything. A consequence is that they are limited to shorter ranges.

### Signal Delay
To comply with Kerbal law, RemoteTech is required to delay your control input so that signalling does not exceed the ‘speed of light’ (pfft, what a silly law). If you are aware of the consequences of breaking the law (or like being a rebel), you are free to turn this off in the settings file.

### Connections

A ‘working connection’ is defined as a command center being able to send control input to its destination and back. Connections between neighbouring satellites are referred to as ‘links’. To have a link between two satellites, it is required that both satellites can transmit a signal to the other independently. You have a connection when there is a sequence of links between a command center and the destination.

### Signal Processors
Signal Processors are any part that can recieve commands over a working connection, including all stock probe cores. You will only be able to control a signal processor as long as you have a working connection, and by default you will be subject to signal delay. Signal processors also include a Flight Computer that can be used to schedule actions ahead of time, for example to carry out basic tasks during a communications gap.

### Command Stations
For those extra long distance missions, it is possible to set up a team of Kerbals to act as a local command center. This Command Station can not process science, a connection to KSC will still be required for that. However, the Command Station allows you to work without the signal delay to Kerbin, which might otherwise climb up to several minutes. Command Stations require a special probe part and a minimum number of kerbals to operate. Consult your VAB technicians for more information.

### Science Transmissions
Transmitting science back to KSC now requires you have a working connection to KSC. Any other source of control, such as a crew pod or a working connection to a command station, does not count.

## Career Mode

All included parts have been integrated in the stock technology tree. Have fun! As an extra, once you unlock Unmanned Technology, all probes will feature an integrated 3km omnidirectional antenna at no cost.

## License

> RemoteTech is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

> This program is distributed in the hope that it will be useful,but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

> You should have received a copy of the GNU General Public License along with this program.  If not, see <http://www.gnu.org/licenses/>.
