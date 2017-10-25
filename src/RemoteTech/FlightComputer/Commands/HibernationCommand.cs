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
            Wake, //exit from power-low state
            Sleep, //future but complex feature - low-power (not as low as hibernate) and reduced comm range
            Hibernate, //probe hibernate and antenna retraction
        }

        [Persistent] public PowerModes PowerMode;
        [Persistent] private List<uint> AntennaIDs = new List<uint>();
        [Persistent] private List<int> AntennaIndices = new List<int>(); //scenario: Part ID is not enough as one modded part can have 2 or more antenna modules
        private bool mAbort = false;
        private bool mStartHibernation = false;

        public override string ShortName
        {
            get
            {
                switch(PowerMode)
                {
                    case PowerModes.Hibernate:
                        return "Power: Hibernation (" + (AntennaIDs.Count==0? "retracting" : ""+AntennaIDs.Count)+" antennas)";
                    case PowerModes.Normal:
                    case PowerModes.Wake:
                        return "Power: Wake up";
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
            var activeHibCommand = HibernationCommand.findActiveHibernationCmd(fc);

            if (PowerMode == PowerModes.Hibernate && activeHibCommand == null) // no active hibernation
            {
                mStartHibernation = true;

                //get all activated antennas, except for probe cores' built-in comms
                AntennaIDs.Clear();
                AntennaIndices.Clear();
                var antennas = RTCore.Instance.Satellites[fc.Vessel.id].Antennas.ToList();
                for(int i = 0; i< antennas.Count; i++)
                {
                    if (antennas[i].Activated && !(antennas[i] is ModuleRTAntennaPassive))
                    {
                        var antenna = antennas[i] as PartModule;
                        AntennaIDs.Add(antenna.part.flightID);
                        AntennaIndices.Add(i);
                    }
                }

                return true;
            }
            else if(PowerMode == PowerModes.Wake && activeHibCommand != null)
            {
                activeHibCommand.Abort();
                return false;
            }

            return false;
        }

        public override bool Execute(FlightComputer fc, FlightCtrlState ctrlState)
        {
            if (mAbort)
            {
                var activatedAntennas = safeGetAntennas(AntennaIDs, AntennaIndices, RTCore.Instance.Satellites[fc.Vessel.id].Antennas.ToList());
                ExitHibernation(fc.Vessel, activatedAntennas);
                mAbort = false;
                return true;
            }
            else if (mStartHibernation)
            {
                var activatedAntennas = safeGetAntennas(AntennaIDs, AntennaIndices, RTCore.Instance.Satellites[fc.Vessel.id].Antennas.ToList());
                EnterHibernation(fc.Vessel, activatedAntennas);
                mStartHibernation = false;
                return false;
            }

            return false;
        }

        public override void Abort()
        {
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
                    if ((hibCmd.PowerMode == PowerModes.Hibernate || hibCmd.PowerMode == PowerModes.Sleep))
                    {
                        return hibCmd;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Safely obtain the marked antennas between play-sessions/staging since one modded part can have 2 or more antenna modules.
        /// </summary>
        private List<IAntenna> safeGetAntennas(List<uint> antennaIDs, List<int> antennaIndices, List<IAntenna> antennas)
        {
            List<IAntenna> desiredAntennas = new List<IAntenna>();

            for(int i=0; i<antennas.Count;i++)
            {
                if (antennaIDs.Contains((antennas[i] as PartModule).part.flightID) && AntennaIndices.Contains(i))
                {
                    desiredAntennas.Add(antennas[i]);
                }
            }

            return desiredAntennas;
        }
    }
}
