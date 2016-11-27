---
title: Modifying the RemoteTech settings
layout: content
navbar: false
---
 
{% include banner.html %}

# Modifying the RemoteTech settings

{% include toc.html %}

RemoteTech has a configuration of default settings, such as the `EnableSignalDelay` flag or a `GroundStations` block. These settings are loaded from RemoteTech's setting file during Kerbal Space Program's startup screen.

**For add-on developers**

If you are interested in reconfiguring a player's RemoteTech installation to work on your mod, you can [deliver your own tweaks](#deliver-your-remotetech-tweaks) into the RemoteTech settings. For example, the author of a planet package could relocate RemoteTech's ground station(s) to a different planet.

**For players**

If you want to edit additional ground stations into the RemoteTech settings of your existing save, the settings are found in either `RemoteTech_Settings.cfg` or `persistent.sfs`. Refer to the [section](#ground-stations) on how to add one or more ground stations.

<hr>

## How does RemoteTech initialise its settings?

When a player starts a new game or resumes an existing game, a sequence of actions is executed:

1. Probe KSP's `GameDatabase` to obtain a list of configurations contained the `RemoteTechSettings` block
2. Iterate through this list to find the configuration of RemoteTech's `Default_Settings.cfg`
3. Load the configuration into the internal settings
4. Check if there are RemoteTech settings in the player's save (newly-created or existing)
   1. If the existing settings are found in the save, the default values in the internal settings are overwrote by the save values
5. RemoteTech proceeds to begin its operations

In summary, the default settings of RemoteTech are loaded into KSP's memory when launching the game. There, the [Module Manager](https://github.com/sarbian/ModuleManager) add-on will modify these settings on behalf of third-party mods targeting RemoteTech. Therefore, if a player starts a new game on his/her current setup of installed mods, these modified settings will be used and stored persistently.

However, the RemoteTech settings in a player's save folder cannot be modified externally due to the KSP's restriction. So even if a player installs a new mod that has a patch for RemoteTech, the RemoteTech settings in his/her save, would not be changed unless the player starts a new game.

<hr>

## Deliver your RemoteTech tweaks

RemoteTech dropped the approach of accepting a third-party mod's `RemoteTech_Settings.cfg` in favour for a better and safer approach of using a [Module Manager](https://github.com/sarbian/ModuleManager) patch for the same tweaks. To upgrade, take those settings you want to affect and translate them into a MM patch according to the [handbook](https://github.com/sarbian/ModuleManager/wiki/Module-Manager-Handbook).
{: .alert .alert-danger}

You can use a Module Manager patch to do the following actions:

1. Edit one or more settings of RemoteTech
2. Edit or add ground station(s)
3. Set a precedence order for yourr own mod in relation to other mods, which have their own RemoteTech patches
4. Skip if another mod, which would render your tweaks unnecessary/useless, is detected

However, this is only applicable to a player's new game that takes in the modified RemoteTech settings. The existing saves cannot be modified due to KSP's restriction.

Lastly, if such patch of a third-party mod is not provided (eg not updated for while), RemoteTech's "fallback" mechanism will list the mod's `RemoteTech_Settings.cfg` in the setting window at the KSC scene for a player to manually apply.

<hr>

## Full settings

In the `GameData/RemoteTech/Default_Settings.cfg` file, all of the RemoteTech settings are stored inside a root block, `RemoteTechSettings`. See this [page](http://remotetechnologiesgroup.github.io/RemoteTech/guide/settings/) for the most of the settings.

`Default_Settings.cfg` **(Last updated: Nov 2016)**

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

The `GroundStations` block controls the number, range, and placement of satellite uplink stations. Each `STATION{}` block represents a station. By default, there is a single ground station, coinciding with the Tracking Station of the Kerbal Space Center.

Each `STATION{}` block has the following fields:

`Guid`
: A **unique** idenfier for the station. Your network would exhibit undefined behavior on duplicate guids. Use the [random.org](http://www.random.org/cgi-bin/randbyte?nbytes=16&format=h) to get some new guids.

`Name`
: The name that a player see in-game.

`Latitude`, `Longitude`
: The position of the station on the planet's surface in degrees north and degrees east respectively.

`Height`
: The station's altitude above sea level, in meters.

`Body`
: The internal ID number of the planet on which the station is located. You can find a list of body IDs for the stock game [here](https://github.com/Anatid/XML-Documentation-for-the-KSP-API/blob/master/src/FlightGlobals.cs#L72-L91). If you are playing with Real Solar System or other planet packs, edit this value with caution.

`MarkColor` (RGBα quadruplet) (default = Red, fully opaque)
: The color in which the ground station will be drawn as a solid circle on the map view and tracking station.

`Antennas`
: This block should contain a single `ANTENNA` block, which in turn should contain a single `Omni` field of the antenna range in meters. Except for the three-level `UpgradeableOmni` field, the other fields are currently unused.

<hr>

## Examples of MM patches

Assumed that you are familiar with the Module Manager's [Handbook](https://github.com/sarbian/ModuleManager/wiki/Module-Manager-Handbook), a number of examples are provided to show how you could modify one or more settings through a patch.

For demonstration purpose, let your mod be named `ExampleMod`.

### Edit a setting

```
@RemoteTechSettings:FOR[ExampleMod]
{
	%EnableSignalDelay = False
}
```

```
//Relocate Mission Control to Duna
@RemoteTechSettings:FOR[ExampleMod]
{
	@GroundStations
	{
		@STATION:HAS[#Name[Mission?Control]]
		{
			@Body = 6
		}
	}
}
```

### Add extra ground stations

```
// The GroundStation block needs to be deleted first before adding more stations
@RemoteTechSettings:FOR[ExampleMod]
{
	!GroundStations,* {}
	GroundStations
	{
		STATION
		{
			Guid = 5105f5a9-d628-41c6-ad4b-21154e8fc488
			Name = Mission Control
			Latitude = -0.131331503391266
			Longitude = -74.594841003418
			Height = 100
			Body = 1
			MarkColor = 1,0,0,1
			Antennas
			{
				ANTENNA
				{
					Omni = 9E+11
				}
			}
		}
		STATION
		{
			Guid = 74dc7a4e-e22e-35d6-eee6-39be668a23c4 
			Name = KSC Northern Control
			Latitude = 19.65
			Longitude = -77.4
			Height = 3200
			Body = 1
			MarkColor = 1,0.8,0,0.7
			Antennas
			{
				ANTENNA
				{
					Omni = 1E+06
				}
			}
		}
	}
}
```

### Mutually exclusiveness

The `NEEDS` keyword is useful if you do not want your tweaks to be applied for a player who has a particular mod. For example, your Kerbin-scope patch should not be applied when it is "detected" that another mod, SupersizeKerbin, is applying its Earth-scope patch to RemoteTech.

```
@RemoteTechSettings:NEEDS[!SupersizeKerbin]:FOR[ExampleMod] {...}
```

### Precedence order of third-party RemoteTech patches

The `BEFORE` and `AFTER` keywords can control the order in which your patch is applied. This standard ordering is, however, useful for a small number of **known** mods targeting the same values. A mod developer doesn't and can't know all other mods in advance to write against (eg they do not exist yet).

Therefore, a lexicographic [scheme](http://forum.kerbalspaceprogram.com/index.php?/topic/139167-12-remotetech-v181-2016-11-19/&do=findComment&comment=2859196) of prefixes `{z, zz, zzz, ...}` is introduced to keep track of other mods patching on each prefix level. For example, let two mods be `AsteroidFactory` and `SolarSystem`, whose `FOR` names are `AsteroidFactory` and `zzSolarSystem` respectively. If you want your `SuperRangeAntennas` patch to be applied between these two patches in order, then append the prefix `z` to your `FOR` name.

```
@RemoteTechSettings:FOR[AsteroidFactory] {...}

@RemoteTechSettings:FOR[zSuperRangeAntennas] {...}

@RemoteTechSettings:FOR[zzSolarSystem] {...}
```

From the above, the Module Manager would apply these patches alphabetically i.e. the `AsteroidFactory`, `zSuperRangeAntennas` and `zzSolarSystem` patches in this particular order. 

