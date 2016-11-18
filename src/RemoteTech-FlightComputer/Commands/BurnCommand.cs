using System;
using RemoteTech.Common.Settings;
using RemoteTech.Common.Utils;

namespace RemoteTech.FlightComputer.Commands
{
    public class BurnCommand : AbstractCommand
    {
        [Persistent] public float Throttle;
        [Persistent] public double Duration;
        [Persistent] public double DeltaV;
        [Persistent] public string KaCItemId = string.Empty;

        public override int Priority => 2;

        public override string Description =>
            $"Burn {Throttle:P2}, {BurnLength()}{Environment.NewLine}{base.Description}";

        public override string ShortName => $"Execute burn for {BurnLength()}";

        private string BurnLength() {
            return Duration > 0 ? TimeUtil.FormatDuration(Duration) : (DeltaV.ToString("F2") + "m/s");
        }

        private bool _abort;

        private static RTCore CoreInstance => RTCore.Instance;

        public override bool Pop(FlightComputer f)
        {
            return true;
        }

        public override bool Execute(FlightComputer f, FlightCtrlState fcs)
        {
            if (_abort)
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
        /// <param name="f">FlightComputer for the current vessel</param>
        /// <returns>max burn time</returns>
        public double GetMaxBurnTime(FlightComputer f)
        {
            if (Duration > 0) return Duration;

            return DeltaV / (Throttle * FlightCore.GetTotalThrust(f.Vessel) / f.Vessel.GetTotalMass());
        }

        public override void Abort() { _abort = true; }

        public static BurnCommand Off()
        {
            return new BurnCommand()
            {
                Throttle = float.NaN,
                Duration = 0,
                DeltaV = 0,
                TimeStamp = TimeUtil.GameTime,
            };
        }

        public static BurnCommand WithDuration(float throttle, double duration)
        {
            return new BurnCommand()
            {
                Throttle = throttle,
                Duration = duration,
                DeltaV = 0,
                TimeStamp = TimeUtil.GameTime,
            };
        }

        public static BurnCommand WithDeltaV(float throttle, double delta)
        {
            return new BurnCommand()
            {
                Throttle = throttle,
                Duration = 0,
                DeltaV = delta,
                TimeStamp = TimeUtil.GameTime,
            };
        }

        /// <summary>
        /// This method will be triggered right after the command was enqueued to
        /// the flight computer list.
        /// </summary>
        /// <param name="computer">Current FlightComputer</param>
        public override void CommandEnqueued(FlightComputer computer)
        {
            var timetoexec = (TimeStamp + ExtraDelay) - 180;

            // only insert if we've no negative time and the option is set
            if (!(timetoexec - TimeUtil.GameTime > 0) || !RTSettings.Instance.AutoInsertKaCAlerts)
                return;

            // set KAC alarm label
            var kaCAddonLabel = "Burn " + computer.Vessel.vesselName + " for ";
            if (Duration > 0)
                kaCAddonLabel += TimeUtil.FormatDuration(Duration);
            else
                kaCAddonLabel += DeltaV;
                
            // create the alarm
            if (CoreInstance != null && CoreInstance.KacAddon != null)
            {
                KaCItemId = CoreInstance.KacAddon.CreateAlarm(RemoteTech_KACWrapper.KACWrapper.KACAPI.AlarmTypeEnum.Raw, kaCAddonLabel, timetoexec, computer.Vessel.id);
            }
        }

        /// <summary>
        /// This method will be triggered after deleting a command from the list.
        /// </summary>
        /// <param name="computer">Current flight computer</param>
        public override void CommandCanceled(FlightComputer computer)
        {
            if (KaCItemId == string.Empty || CoreInstance == null || CoreInstance.KacAddon == null)
                return;

            // Cancel also the KAC entry
            CoreInstance.KacAddon.DeleteAlarm(KaCItemId);
            KaCItemId = string.Empty;
        }
    }
}
