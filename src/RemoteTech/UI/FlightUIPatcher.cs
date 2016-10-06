using KSP.UI.Screens.Flight;
using RemoteTech.FlightComputer.Commands;

namespace RemoteTech.UI
{
    public class FlightUIPatcher
    {
        /// <summary>
        /// Hook flight action group buttons: gear, brakes, light and abort buttons.
        /// </summary>
        public static void Patch()
        {
            ActionGroupToggleButton[] actionGroupToggleButtons = UnityEngine.Object.FindObjectsOfType<ActionGroupToggleButton>();
            for (int i = 0; i < actionGroupToggleButtons.Length; ++i)
            {
                ActionGroupToggleButton button = actionGroupToggleButtons[i];
                switch(button.group)
                {
                    //only for the 4 groups below: not SAS or RCS action groups.
                    case KSPActionGroup.Gear:
                    case KSPActionGroup.Brakes:
                    case KSPActionGroup.Light:
                    case KSPActionGroup.Abort:
                        {
                            // remove all current listeners and set our hook
                            button.toggle.onToggle.RemoveAllListeners();
                            button.toggle.onToggle.AddListener( () => ActivateActionGroup(button.group) );
                            break;
                        }
                }
            }
        }

        private static void ActivateActionGroup(KSPActionGroup ag)
        {
            var satellite = RTCore.Instance.Satellites[FlightGlobals.ActiveVessel];
            if (satellite != null && satellite.FlightComputer != null)
            {
                satellite.SignalProcessor.FlightComputer.Enqueue(ActionGroupCommand.WithGroup(ag));
            }
            else if (satellite == null || (satellite != null && satellite.HasLocalControl))
            {
                if (FlightGlobals.ActiveVessel.IsControllable)
                {
                    FlightGlobals.ActiveVessel.ActionGroups.ToggleGroup(ag);
                }
            }
        }
    }
}
