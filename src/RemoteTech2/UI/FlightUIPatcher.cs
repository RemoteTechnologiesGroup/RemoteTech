using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class FlightUIPatcher
    {
        public static void Patch()
        {
            var controller = FlightUIController.fetch;
            if (controller == null) return;
            var gears = controller.gears;
            var brakes = controller.brakes;
            var lights = controller.lights;
            var abort = controller.abort;

            gears.OnPress = () => ActivateActionGroup(KSPActionGroup.Gear);
            brakes.OnPress = () => ActivateActionGroup(KSPActionGroup.Brakes);
            lights.OnPress = () => ActivateActionGroup(KSPActionGroup.Light);
            abort.OnPress = () => ActivateActionGroup(KSPActionGroup.Abort);
        }

        private static void ActivateActionGroup(KSPActionGroup ag)
        {
            var satellite = RTCore.Instance.Satellites[FlightGlobals.ActiveVessel];
            if (satellite != null && satellite.FlightComputer != null)
            {
                satellite.SignalProcessor.FlightComputer.Enqueue(ActionGroupCommand.Group(ag));
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
