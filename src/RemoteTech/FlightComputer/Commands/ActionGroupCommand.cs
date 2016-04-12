﻿using System;

namespace RemoteTech.FlightComputer.Commands
{
    public class ActionGroupCommand : AbstractCommand
    {
        [Persistent] public KSPActionGroup ActionGroup;

        public override string Description
        {
            get { return ShortName + Environment.NewLine + base.Description; }
        }
        public override string ShortName { get { return "Toggle " + ActionGroup; } }

        public override bool Pop(FlightComputer f)
        {
            f.Vessel.ActionGroups.ToggleGroup(ActionGroup);
            if (ActionGroup == KSPActionGroup.Stage && !(f.Vessel == FlightGlobals.ActiveVessel && FlightInputHandler.fetch.stageLock))
            {
                KSP.UI.Screens.StageManager.ActivateNextStage();
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
                TimeStamp = RTUtil.GameTime,
            };
        }
    }
}
