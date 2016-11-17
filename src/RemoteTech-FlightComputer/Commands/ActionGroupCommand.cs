using System;
using RemoteTech.Common;
using RemoteTech.Common.Utils;

namespace RemoteTech.FlightComputer.Commands
{
    public class ActionGroupCommand : AbstractCommand
    {
        [Persistent] public KSPActionGroup ActionGroup;

        public override string Description => ShortName + Environment.NewLine + base.Description;
        public override string ShortName => "Toggle " + ActionGroup;

        public override bool Pop(FlightComputer f)
        {
            f.Vessel.ActionGroups.ToggleGroup(ActionGroup);
            if (ActionGroup == KSPActionGroup.Stage && !(f.Vessel == FlightGlobals.ActiveVessel && FlightInputHandler.fetch.stageLock))
            {
                try
                {
                    KSP.UI.Screens.StageManager.ActivateNextStage();
                }
                catch(Exception ex)
                {
                    RTLog.Notify("Exception during ActionGroupCommand.ActivateNextStage(): " + ex, RTLogLevel.LVL4);
                }
                KSP.UI.Screens.ResourceDisplay.Instance.Refresh();
            }
            if (ActionGroup == KSPActionGroup.RCS && f.Vessel == FlightGlobals.ActiveVessel)
            {
                FlightInputHandler.fetch.rcslock = !FlightInputHandler.RCSLock;
            }

            return false;
        }

        public static ActionGroupCommand WithGroup(KSPActionGroup group)
        {
            return new ActionGroupCommand()
            {
                ActionGroup = group,
                TimeStamp = TimeUtil.GameTime,
            };
        }
    }
}
