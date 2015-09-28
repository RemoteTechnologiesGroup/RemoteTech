using RemoteTech.SimpleTypes;
using System;
using System.Reflection;

namespace RemoteTech.AddOns
{
    /// <summary>
    /// This class connects to the KSP-Addon KerbalAlarmClock, created by triggerAU
    /// Topic: http://forum.kerbalspaceprogram.com/threads/24786
    /// </summary>
    public class KerbalAlarmClockAddon : AddOn
    {
        private bool KaCApiReady = false;

        public enum AlarmTypeEnum
        {
            Raw,
            Maneuver,
            ManeuverAuto,
            Apoapsis,
            Periapsis,
            AscendingNode,
            DescendingNode,
            LaunchRendevous,
            Closest,
            SOIChange,
            SOIChangeAuto,
            Transfer,
            TransferModelled,
            Distance,
            Crew,
            EarthTime,
            Contract,
            ContractAuto
        }

        public KerbalAlarmClockAddon()
            : base("KerbalAlarmClock", "KerbalAlarmClock.KerbalAlarmClock")
        {
            // change the bindings
            this.bFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod;
            this.loadInstance();
        }

        private void loadInstance()
        {
            /// load KaC instance
            try
            {
                this.instance = this.assemblyType.GetField("APIInstance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                this.KaCApiReady = (bool)this.assemblyType.GetField("APIReady", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            }
            catch (Exception ex)
            {
                RTLog.Verbose("AddOn.loadInstance exception: {0}", RTLogLevel.Assembly, ex);
            }
        }

        /// <summary>
        /// Returns the KaC API Status
        /// </summary>
        public bool APIReady()
        {
            return this.KaCApiReady;
        }
        
        /// <summary>
        /// Create a new Alarm
        /// </summary>
        /// <param name="AlarmType">What type of alarm are we creating</param>
        /// <param name="Name">Name of the Alarm for the display</param>
        /// <param name="UT">Universal Time for the alarm</param>
        /// <returns>ID of the newly created alarm</returns>
        public String CreateAlarm(AlarmTypeEnum AlarmType, String Name, Double UT)
        {
            // Is KaC Ready?
            if (!this.APIReady()) return String.Empty;

            var result = this.invoke(new System.Object[] { (Int32)AlarmType, Name, UT });

            if (result != null) return (String)result;
            return String.Empty;
        }

        /// <summary>
        /// Delete Alarm Method for calling via API
        /// </summary>
        /// <param name="AlarmID">Unique ID of the alarm</param>
        /// <returns>Success</returns>
        public Boolean DeleteAlarm(String AlarmID)
        {
            // Is KaC Ready?
            if (!this.APIReady()) return false;

            var result = this.invoke(new System.Object[] { AlarmID });

            if (result != null) return (Boolean)result;
            return false;
        }
    }
}
