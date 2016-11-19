using System;

namespace RemoteTech.Common.AddOn
{
    /// <summary>
    /// This class connects to the KSP-Addon KerbalAlarmClock, created by triggerAU
    /// Topic: http://forum.kerbalspaceprogram.com/threads/24786
    /// </summary>
    public class KerbalAlarmClockAddon : AddOn
    {
        public KerbalAlarmClockAddon()
            : base("KerbalAlarmClock", "KerbalAlarmClock.KerbalAlarmClock")
        {
            if (!AssemblyLoaded)
                return;

            KACWrapper.InitKACWrapper();
            var message = KACWrapper.APIReady ? "KerbalAlarmClockAddon.loadInstance: Successfully loaded KAC!" : "KerbalAlarmClockAddon.loadInstance: Couldn't load Instance.";
            RTLog.Verbose(message, RTLogLevel.Assembly);
        }
       
        /// <summary>
        /// Create a new Alarm
        /// </summary>
        /// <param name="alarmType">What type of alarm are we creating</param>
        /// <param name="name">Name of the Alarm for the display</param>
        /// <param name="UT">Universal Time for the alarm</param>
        /// <param name="vesselId">The id of the vessel for which to set an alarm.</param>
        /// <returns>ID of the newly created alarm or string.empty if alarm couldn't be created.</returns>
        public string CreateAlarm(KACWrapper.KACAPI.AlarmTypeEnum alarmType, string name, double UT, Guid vesselId)
        {
            // Is KaC Ready?
            if (!AssemblyLoaded && !KACWrapper.APIReady)
                return string.Empty;

            // create alarm and get its ID.
            var alarmId = KACWrapper.KAC.CreateAlarm(alarmType, name, UT);
            if (string.IsNullOrEmpty(alarmId))
                return string.Empty;

            // find alarm from ID.
            var kacAlarm = KACWrapper.KAC.Alarms.Find(alarm => alarm.ID == alarmId);
            if (kacAlarm == null)
                return string.Empty;

            // set vessel ID in alarm (might be necessary for the "jump to ship" feature from the alarm).
            kacAlarm.VesselID = vesselId.ToString();

            return alarmId;
        }

        /// <summary>
        /// Delete Alarm Method for calling via API
        /// </summary>
        /// <param name="alarmId">Unique ID of the alarm</param>
        /// <returns>true if alarm was deleted, false otherwise.</returns>
        public bool DeleteAlarm(string alarmId)
        {
            // Is KaC Ready?
            if (!AssemblyLoaded && !KACWrapper.APIReady)
                return false;

            return KACWrapper.KAC.DeleteAlarm(alarmId);
        }
    }
}
