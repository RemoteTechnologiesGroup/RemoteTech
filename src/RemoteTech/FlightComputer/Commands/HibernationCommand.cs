using RemoteTech.Modules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RemoteTech.FlightComputer.Commands
{
    public class HibernationCommand : AbstractCommand
    {
        public enum PowerModes
        {
            Normal,
            Wake, //exit from any power-low state
            Sleep, //future but complex feature - low-power (not as low as hibernate) and reduced comm range
            Hibernate, //probe hibernate and antenna retraction
            AntennaSaver, //deactivate/re-activate antennas with power thresholds
        }

        [Persistent] public PowerModes PowerMode;
        [Persistent] private List<uint> AntennaIDs = new List<uint>();
        private Dictionary<uint,DateTime> antennaLastChangedDates = new Dictionary<uint,DateTime>();
        private bool mAbort = false;
        private bool mStartHibernation = false;
        private Vessel vesselReference;

        public override string ShortName
        {
            get
            {
                switch(PowerMode)
                {
                    case PowerModes.Hibernate:
                        return "Power: Hibernation (" + (AntennaIDs.Count==0? "deactivating" : "inactive "+ AntennaIDs.Count)+" antennas)";
                    case PowerModes.AntennaSaver:
                        return "Power: Automatically de/re-activating antennas on thresholds";
                    case PowerModes.Normal:
                    case PowerModes.Wake:
                        return "Power: Terminating active power state";
                    default:
                        return "Power: Unknown";
                }
            }
        }

        public override string Description
        {
            get
            {
                return ShortName + Environment.NewLine + base.Description;
            }
        }

        public static HibernationCommand Hibernate()
        {
            return new HibernationCommand()
            {
                PowerMode = PowerModes.Hibernate,
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static HibernationCommand AntennaSaver()
        {
            return new HibernationCommand()
            {
                PowerMode = PowerModes.AntennaSaver,
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static HibernationCommand WakeUp()
        {
            return new HibernationCommand()
            {
                PowerMode = PowerModes.Wake,
                TimeStamp = RTUtil.GameTime,
            };
        }

        public override bool Pop(FlightComputer fc)
        {
            this.vesselReference = fc.Vessel;

            var activeHibCommand = HibernationCommand.findActiveHibernationCmd(fc);
            if(activeHibCommand != null) //what to do with active hibernation cmd under this new hib cmd?
            {
                if(this.PowerMode == PowerModes.Wake)
                {
                    activeHibCommand.Abort();
                    return false;
                }
                else if(this.PowerMode == PowerModes.Hibernate && activeHibCommand.PowerMode == PowerModes.AntennaSaver)
                {
                    activeHibCommand.Abort();
                }
                else if (this.PowerMode == PowerModes.Hibernate && activeHibCommand.PowerMode == PowerModes.Hibernate)
                {
                    return false;
                }
                else if (this.PowerMode == PowerModes.AntennaSaver&& activeHibCommand.PowerMode == PowerModes.Hibernate)
                {
                    activeHibCommand.Abort();
                }
                else if (this.PowerMode == PowerModes.AntennaSaver && activeHibCommand.PowerMode == PowerModes.AntennaSaver)
                {
                    return false;
                }
            }

            if (PowerMode == PowerModes.Hibernate)
            {
                if(AntennaIDs.Count == 0)//no saved list found
                {
                    mStartHibernation = true;
                }

                return true;
            }
            else if (PowerMode == PowerModes.AntennaSaver)
            {
                return true;
            }

            return false;
        }

        public override bool Execute(FlightComputer fc, FlightCtrlState ctrlState)
        {
            if (mAbort)
            {               
                mAbort = false;
                return true;
            }
            else if (mStartHibernation)
            {
                AntennaIDs = getActiveAntennas(RTCore.Instance.Satellites[fc.Vessel.id].Antennas.ToList());
                EnterHibernation(fc.Vessel, safeGetAntennas(AntennaIDs, RTCore.Instance.Satellites[fc.Vessel.id].Antennas.ToList()));
                mStartHibernation = false;
                return false;
            }
            else if (this.PowerMode == PowerModes.AntennaSaver) //run until mAbort is issued
            {
                RunThresholdControl(fc.Vessel, RTCore.Instance.Satellites[fc.Vessel.id].Antennas);
                return false;
            }
            else
            {
                //drop any queued non-power command up to next hiberation command (wake)
                var cmdsToDrop = new List<ICommand>();
                for(int i=0; i<fc.QueuedCommands.Count(); i++)
                {
                    if (!(fc.QueuedCommands.ElementAt(i) is HibernationCommand || fc.QueuedCommands.ElementAt(i) is ManeuverCommand))//don't mess with player's maneuver nodes
                        cmdsToDrop.Add(fc.QueuedCommands.ElementAt(i));
                    else
                        break;//found next hiberation command
                }

                for (int i = 0; i < cmdsToDrop.Count; i++)
                {
                    fc.Remove(cmdsToDrop[i]);
                }
            }

            return false;
        }

        public override void Abort()
        {
            switch (this.PowerMode)
            {
                case PowerModes.Hibernate:
                    var activatedAntennas = safeGetAntennas(AntennaIDs, RTCore.Instance.Satellites[this.vesselReference.id].Antennas.ToList());
                    ExitHibernation(this.vesselReference, activatedAntennas);
                    break;
                case PowerModes.AntennaSaver:
                    var antennas = RTCore.Instance.Satellites[this.vesselReference.id].Antennas.ToList();
                    TerminateThresholdControl(antennas);
                    break;
                default:
                    break;
            }

            AntennaIDs.Clear();
            this.vesselReference = null;
            mAbort = true;
        }

        private void EnterHibernation(Vessel vessel, List<IAntenna> antennas)
        {
            //set all parts with ModuleCommand to hibernate
            var parts = vessel.Parts;
            for(int i=0; i< parts.Count; i++)
            {
                var moduleCommand = parts[i].Modules.GetModule<ModuleCommand>();
                if(moduleCommand != null)
                {
                    moduleCommand.hibernation = true;
                }
            }

            //retract activated antennas
            for(int i=0; i<antennas.Count; i++)
            {
                antennas[i].Activated = false;
            }
        }

        private void ExitHibernation(Vessel vessel, List<IAntenna> antennas)
        {
            //wake all parts with ModuleCommand up
            var parts = vessel.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                var moduleCommand = parts[i].Modules.GetModule<ModuleCommand>();
                if (moduleCommand != null)
                {
                    moduleCommand.hibernation = false;
                }
            }

            //extend antennas that were activated prior to hibernation
            for (int i = 0; i < antennas.Count; i++)
            {
                antennas[i].Activated = true;
            }
        }

        public static HibernationCommand findActiveHibernationCmd(FlightComputer fc)
        {
            var cmdItr = fc.ActiveCommands.GetEnumerator();
            while (cmdItr.MoveNext())
            {
                if (cmdItr.Current is HibernationCommand)
                {
                    var hibCmd = cmdItr.Current as HibernationCommand;
                    if ((hibCmd.PowerMode == PowerModes.Hibernate || hibCmd.PowerMode == PowerModes.Sleep || hibCmd.PowerMode == PowerModes.AntennaSaver))
                    {
                        return hibCmd;
                    }
                }
            }

            return null;
        }

        private List<uint> getActiveAntennas(List<IAntenna> antennas)
        {
            //get all activated antennas, except for probe cores' built-in comms
            List<uint> ids = new List<uint>();
            for (int i = 0; i < antennas.Count; i++)
            {
                if (antennas[i].Activated && !(antennas[i] is ModuleRTAntennaPassive))
                {
                    ids.Add((antennas[i] as PartModule).part.flightID);
                }
            }
            return ids;
        }

        /// <summary>
        /// Safely obtain the marked antennas between play-sessions/staging since one modded part can have 2 or more antenna modules.
        /// </summary>
        private List<IAntenna> safeGetAntennas(List<uint> antennaIDs, List<IAntenna> antennas)
        {
            List<IAntenna> desiredAntennas = new List<IAntenna>();

            for(int i=0; i<antennas.Count;i++)
            {
                if (antennaIDs.Contains((antennas[i] as PartModule).part.flightID))
                {
                    desiredAntennas.Add(antennas[i]);
                }
            }

            return desiredAntennas;
        }

        //Credit: rsparkyc
        //https://github.com/rsparkyc/AntennaPowerSaver/blob/master/AntennaPowerSaver/ModuleAntennaPowerSaver.cs
        //use IAntenna to avoid ToList's final-result performance penalty in repeated run loops
        private void RunThresholdControl(Vessel vessel, IEnumerable<IAntenna> antennas)
        {
            //FC does not run in time wrap (>= 5x)

            //do EC calculation
            double currentECAmount = 1;
            double maxECAmount = 1;
            double percentage;

            vessel.GetConnectedResourceTotals(PartResourceLibrary.ElectricityHashcode, out currentECAmount, out maxECAmount);
            percentage = (currentECAmount / maxECAmount) * 100.00;

            //run on each antenna
            for (int i = 0; i < antennas.Count(); i++)
            {
                if (!(antennas.ElementAt(i) is ModuleRTAntennaPassive))
                {
                    var thisAntenna = antennas.ElementAt(i) as ModuleRTAntenna;
                    thisAntenna.GUI_DeReactivation_Status = string.Format("EC {0:0.0}%", percentage);

                    if (thisAntenna.Activated && percentage <= thisAntenna.RTDeactivatePowerThreshold)
                    {
                        SetDelayedAntennaState(false, thisAntenna);
                    }
                    else if(!thisAntenna.Activated && percentage >= thisAntenna.RTActivatePowerThreshold)
                    {
                        SetDelayedAntennaState(true, thisAntenna);
                    }
                }
            }
        }

        private void TerminateThresholdControl(List<IAntenna> antennas)
        {
            //update antenna's gui status
            for (int i = 0; i < antennas.Count; i++)
            {
                if (!(antennas[i] is ModuleRTAntennaPassive))
                {
                    (antennas[i] as ModuleRTAntenna).GUI_DeReactivation_Status = "Off";
                }
            }

            antennaLastChangedDates.Clear();
        }

        private void SetDelayedAntennaState(bool active, ModuleRTAntenna antenna)
        {
            var nowDate = DateTime.Now;

            if (!antennaLastChangedDates.ContainsKey(antenna.part.flightID))
            {
                antennaLastChangedDates.Add(antenna.part.flightID, nowDate);
                antenna.Activated = active;
            }
            else if(nowDate.Subtract(antennaLastChangedDates[antenna.part.flightID]).TotalSeconds > 5)
            {
                antennaLastChangedDates[antenna.part.flightID] = nowDate;
                antenna.Activated = active;
            }
        }
    }
}
