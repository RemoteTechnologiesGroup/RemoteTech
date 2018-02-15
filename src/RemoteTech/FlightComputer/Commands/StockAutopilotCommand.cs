using System;
using static VesselAutopilot;

namespace RemoteTech.FlightComputer.Commands
{
    public class StockAutopilotCommand : AbstractCommand
    {
        [Persistent] public AutopilotMode AutopilotMode;
        public static VesselAutopilotUI UIreference = null;
        private static AutopilotMode savedAutopilotMode = AutopilotMode.StabilityAssist;

        public override string ShortName
        {
            get
            {
                return AutopilotMode.ToString();
            }
        }

        public override string Description
        {
            get
            {
                return "Autopilot: " + ShortName + Environment.NewLine + base.Description;
            }
        }

        public override bool Pop(FlightComputer f)
        {
            if (f.Vessel.Autopilot.CanSetMode(AutopilotMode))
            {
                f.Vessel.Autopilot.SetMode(AutopilotMode);
                savedAutopilotMode = AutopilotMode; // be sure to update the saved mode after setting autpilot mode
                return false;
            }
            return true;
        }

        public static StockAutopilotCommand WithNewMode(AutopilotMode newMode)
        {
            return new StockAutopilotCommand()
            {
                AutopilotMode = newMode,
                TimeStamp = RTUtil.GameTime,
            };
        }

        /// <summary>
        /// Action to be called by a RT listener on KSP's autopilot buttons
        /// </summary>
        public static void AutopilotButtonClick(int index, FlightComputer flightCom)
        {
            var satellite = flightCom.Vessel;
            if (!satellite.HasLocalControl() && flightCom.InputAllowed)
            {
                //Note: the VesselAutopilotUI's OnClickButton is delayed by FlightComputer so no further action needed
                //Note: KSP bug #13199 (http://bugs.kerbalspaceprogram.com/issues/13199) on wrong-placed Radial In & Out buttons
                var currentMode = flightCom.Vessel.Autopilot.Mode;
                var nextMode = (AutopilotMode)index;

                if (currentMode != nextMode)
                {
                    savedAutopilotMode = currentMode; // autopilot's stock actionlistener will set to new mode so we need to roll back to prev mode via IsAutoPilotEngaged()
                    var newCommand = WithNewMode(nextMode);
                    flightCom.Enqueue(newCommand);

                    //Note: Timer of returning to prev mode doesn't really work too well in Unity and KSP architecture
                }
            }
        }

        /// <summary>
        /// Check if KSP's autopilot is performing one SAS function in presence of long delay
        /// </summary>
        public static bool IsAutoPilotEngaged(FlightComputer flightCom)
        {
            if (!flightCom.Vessel.Autopilot.Enabled) // autopilot is off
            {
                if(savedAutopilotMode != AutopilotMode.StabilityAssist)
                    savedAutopilotMode = AutopilotMode.StabilityAssist; // matched to KSP's default to SAS mode when turned on 
                return false;
            }

            if (flightCom.Vessel.Autopilot.Mode != savedAutopilotMode && flightCom.Vessel.Autopilot.CanSetMode(savedAutopilotMode))
                flightCom.Vessel.Autopilot.SetMode(savedAutopilotMode); // purpose: return to the pre-click mode

            if (GameSettings.PITCH_DOWN.GetKey() || GameSettings.PITCH_UP.GetKey() ||
                GameSettings.ROLL_LEFT.GetKey() || GameSettings.ROLL_RIGHT.GetKey() ||
                GameSettings.YAW_LEFT.GetKey() || GameSettings.YAW_RIGHT.GetKey()) // player trying to manually rotate
                return false;

            if (GameSettings.TRANSLATE_FWD.GetKey() || GameSettings.TRANSLATE_BACK.GetKey() ||
                GameSettings.TRANSLATE_LEFT.GetKey() || GameSettings.TRANSLATE_RIGHT.GetKey() ||
                GameSettings.TRANSLATE_UP.GetKey() || GameSettings.TRANSLATE_DOWN.GetKey()) // player trying to manually translate
                return false;

            if (Math.Abs(GameSettings.AXIS_PITCH.GetAxis()) >= 0.0f || 
                Math.Abs(GameSettings.AXIS_ROLL.GetAxis()) >= 0.0f ||
                Math.Abs(GameSettings.AXIS_YAW.GetAxis()) >= 0.0f) // player trying to joystick
                return false;

            return true;
        }
    }
}
