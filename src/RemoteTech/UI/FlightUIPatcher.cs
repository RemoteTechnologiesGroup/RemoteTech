using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens.Flight;
using RemoteTech.FlightComputer.Commands;

namespace RemoteTech.UI
{
    public class FlightUIPatcher
    {
        /// <summary>
        /// Action groups corresponding to the GUI buttons we want to hook / patch.
        /// </summary>
        public static KSPActionGroup[] PatchedActionGroups = { KSPActionGroup.Gear, KSPActionGroup.Brakes, KSPActionGroup.Light, KSPActionGroup.Abort };

        /// <summary>
        /// Hook flight action group buttons: gear, brakes, light and abort buttons.
        /// </summary>
        public static void Patch()
        {   
            var buttons = CollectActionGroupToggleButtons(PatchedActionGroups);
            for (int i = 0; i < buttons.Count; ++i)
            {
                // set our hook
                buttons[i].toggle.onToggle.AddListener( () => ActivateActionGroup(buttons[i].group) );
            }
        }

        /// <summary>
        /// Get action groups buttons depending on their group.
        /// </summary>
        /// <param name="actionGroups">The action group(s) in which the buttons should be.</param>
        /// <returns>A list of action ActionGroupToggleButton buttons, filter by actionGroups paramter.</returns>
        private static List<ActionGroupToggleButton> CollectActionGroupToggleButtons(KSPActionGroup[] actionGroups)
        {
            // get all action group buttons
            ActionGroupToggleButton[] actionGroupToggleButtons = UnityEngine.Object.FindObjectsOfType<ActionGroupToggleButton>();
            // filter them to only get the buttons that have a group in the actionGroups array
            var buttons = actionGroupToggleButtons.Where(button => actionGroups.Any(ag => button.group == ag)).ToList();

            return buttons;
        }

        /// <summary>
        /// Called when an action group button, from the KSP GUI, is pressed.
        /// </summary>
        /// <param name="ag">The action group that was pressed.</param>
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
