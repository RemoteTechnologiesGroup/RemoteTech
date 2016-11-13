using System;

namespace RemoteTech.FlightComputer.Commands
{
    public class BurnCommand : AbstractCommand
    {
        [Persistent] public float Throttle;
        [Persistent] public double Duration;
        [Persistent] public double DeltaV;
        [Persistent] public string KaCItemId = string.Empty;

        public override int Priority { get { return 2; } }

        public override String Description
        {
            get
            {
                return String.Format("Burn {0}, {1}", Throttle.ToString("P2"), burnLength()) + Environment.NewLine + base.Description;
            }
        }
        public override string ShortName 
        { 
            get { 
                return String.Format("Execute burn for {0}", burnLength()); 
            } 
        }
        private string burnLength() {
            return Duration > 0 ? RTUtil.FormatDuration(Duration) : (DeltaV.ToString("F2") + "m/s");
        }

        private bool mAbort;

        public override bool Pop(FlightComputer f)
        {
            return true;
        }

        public override bool Execute(FlightComputer f, FlightCtrlState fcs)
        {
            if (mAbort)
            {
                fcs.mainThrottle = 0.0f;
                return true;
            }

            if (Duration > 0)
            {
                fcs.mainThrottle = Throttle;
                Duration -= TimeWarp.deltaTime;
            }
            else if (DeltaV > 0)
            {
                fcs.mainThrottle = Throttle;
                DeltaV -= (Throttle * FlightCore.GetTotalThrust(f.Vessel) / f.Vessel.GetTotalMass()) * TimeWarp.deltaTime;
            }
            else
            {
                fcs.mainThrottle = 0.0f;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the total time for this burn in seconds
        /// </summary>
        /// <param name="f">Flightcomputer for the current vessel</param>
        /// <returns>max burn time</returns>
        public double getMaxBurnTime(FlightComputer f)
        {
            if (Duration > 0) return Duration;

            return DeltaV / (Throttle * FlightCore.GetTotalThrust(f.Vessel) / f.Vessel.GetTotalMass());
        }

        public override void Abort() { mAbort = true; }

        public static BurnCommand Off()
        {
            return new BurnCommand()
            {
                Throttle = Single.NaN,
                Duration = 0,
                DeltaV = 0,
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static BurnCommand WithDuration(float throttle, double duration)
        {
            return new BurnCommand()
            {
                Throttle = throttle,
                Duration = duration,
                DeltaV = 0,
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static BurnCommand WithDeltaV(float throttle, double delta)
        {
            return new BurnCommand()
            {
                Throttle = throttle,
                Duration = 0,
                DeltaV = delta,
                TimeStamp = RTUtil.GameTime,
            };
        }

        /// <summary>
        /// This method will be triggerd right after the command was enqueued to
        /// the flight computer list.
        /// </summary>
        /// <param name="computer">Current flightcomputer</param>
        public override void CommandEnqueued(FlightComputer computer)
        {
            var timetoexec = (TimeStamp + ExtraDelay) - 180;

            // only insert if we've no negative time and the option is set
            if (!(timetoexec - RTUtil.GameTime > 0) || !RTSettings.Instance.AutoInsertKaCAlerts)
                return;

            // set kac alarm label
            var kaCAddonLabel = "Burn " + computer.Vessel.vesselName + " for ";
            if (Duration > 0)
                kaCAddonLabel += RTUtil.FormatDuration(this.Duration);
            else
                kaCAddonLabel += this.DeltaV;
                
            // create the alarm
            if (RTCore.Instance != null && RTCore.Instance.KacAddon != null)
            {
                KaCItemId = RTCore.Instance.KacAddon.CreateAlarm(RemoteTech_KACWrapper.KACWrapper.KACAPI.AlarmTypeEnum.Raw, kaCAddonLabel, timetoexec, computer.Vessel.id);
            }
        }

        /// <summary>
        /// This method will be triggerd after deleting a command from the list.
        /// </summary>
        /// <param name="computer">Current flight computer</param>
        public override void CommandCanceled(FlightComputer computer)
        {
            if (KaCItemId == string.Empty || RTCore.Instance == null || RTCore.Instance.KacAddon == null)
                return;

            // Cancel also the kac entry
            RTCore.Instance.KacAddon.DeleteAlarm(KaCItemId);
            KaCItemId = string.Empty;
        }
    }
}
