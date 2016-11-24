---
title: Modifying the RemoteTech settings
layout: content
navbar: false
---
 
{% include banner.html %}

# Modifying the RemoteTech settings

{% include toc.html %}

RemoteTech has a configuration of default settings, such as the `EnableSignalDelay` flag and a `GroundStations` node of ground stations. These settings are loaded from the `GameData/RemoteTech/Default_Settings.cfg` file during Kerbal Space Program's startup screen.

## When a player starts a new game

## When resuming an existing game

## Full settings (Nov 2016)

All of the RemoteTech settings are outlined along with their brief descriptions below:

```
RemoteTechSettings
{
	RemoteTechEnabled = True
	CommNetEnabled = False
	ConsumptionMultiplier = 1
	RangeMultiplier = 1
	MissionControlRangeMultiplier = 1    
	ActiveVesselGuid = 35b89a0d664c43c6bec8d0840afc97b2
	NoTargetGuid = 00000000-0000-0000-0000-000000000000
	SpeedOfLight = 3E+08
	MapFilter = Omni, Cone, Path
	EnableSignalDelay = True
	RangeModelType = Standard
	MultipleAntennaMultiplier = 0
	ThrottleTimeWarp = True
	ThrottleZeroOnNoConnection = True
	HideGroundStationsBehindBody = True
	ControlAntennaWithoutConnection = False
	UpgradeableMissionControlAntennas = True
	HideGroundStationsOnDistance = True
	ShowMouseOverInfoGroundStations = True
	AutoInsertKaCAlerts = True
	FCLeadTime = 180
	FCOffAfterExecute = False
	DistanceToHideGroundStations = 3E+07
	DishConnectionColor = 0.996078372,0.701960802,0.0313725509,1
	OmniConnectionColor = 0.552941203,0.517647088,0.407843113,1
	ActiveConnectionColor = 0.65882349,1,0.0156862792,1
	RemoteStationColorDot = 0.996078014,0,0,1
	GroundStations
	{
		STATION
		{
			Guid = 5105f5a9-d628-41c6-ad4b-21154e8fc488
			Name = Mission Control
			Latitude = -0.13133150339126601
			Longitude = -74.594841003417997
			Height = 75
			Body = 1
			MarkColor = 0.996078014,0,0,1
			Antennas
			{
				ANTENNA
				{
					Omni = 75000000
					Dish = 0
					CosAngle = 1
					UpgradeableOmni = 4E+06;3.0E+07;7.5E+07
					UpgradeableDish = 
					UpgradeableCosAngle = 
				}
			}
		}
	}
	PreSets
	{
		PRESETS = RemoteTech/Default_Settings/RemoteTechSettings
	}
}
```

### Ground stations

The `GroundStations` section of `RemoteTech/RemoteTech_Settings.cfg` (which will be created when you start KSP) controls the number, range, and placement of satellite uplink stations. Each `STATION{}` block represents a station. By default, there is only one ground station, coinciding with the KSC Tracking Station.

Each `STATION{}` block needs the following fields:

`Guid`
: A unique idenfier for the station. **Must** be unique, or your network will exhibit undefined behavior. If you need a way to generate new guids, try [random.org](http://www.random.org/cgi-bin/randbyte?nbytes=16&format=h).

`Name`
: The name that shows up in the target selection menu.

`Latitude`, `Longitude`
: The position of the station on the planet's surface in degrees north and degrees east, respectively.

`Height`
: The station's altitude above sea level, in meters.

`Body`
: The internal ID number of the planet on which the station is located. You can find a list of ID numbers for stock Kerbal Space Program [here](https://github.com/Anatid/XML-Documentation-for-the-KSP-API/blob/master/src/FlightGlobals.cs#L72). If you are playing with Real Solar System or other planet packs, edit this value with caution.

`Antennas`
: This block should contain a single `ANTENNA` block, which itself should contain a single `Omni` field. The value of the field is the antenna range in meters.

## RemoteTech settings in a player's existing save

## Examples of MM patches

Assumed that you are familiar with the Module Manager's [Handbook](https://github.com/sarbian/ModuleManager/wiki/Module-Manager-Handbook), a number of examples are provided to show how you could modify one or more settings through a patch.

### Edit a setting

### Add one or more ground station

### Precedence order of third-party RemoteTech patches
