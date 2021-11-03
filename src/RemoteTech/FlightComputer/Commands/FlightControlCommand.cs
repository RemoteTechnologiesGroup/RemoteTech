using System;

namespace RemoteTech.FlightComputer.Commands
{
    public class FlightControlCommand : AbstractCommand
    {
        [Persistent] public bool ignorePitchOutput;
        [Persistent] public bool ignoreHeadingOutput;
        [Persistent] public bool ignoreRollOutput;

        private bool mAbort;
        private string stringReady = "";

        public override string ShortName
        {
            get
            {
                return "Pitch, Heading and Roll controls";
            }
        }

        public override string Description
        {
            get
            {
                return ShortName + ":" + Environment.NewLine + stringReady + base.Description;
            }
        }

        public static FlightControlCommand WithPHR(bool ignore_pitch, bool ignore_heading, bool ignore_roll)
        {
            return new FlightControlCommand()
            {
                ignorePitchOutput = ignore_pitch,
                ignoreHeadingOutput = ignore_heading,
                ignoreRollOutput = ignore_roll,
                TimeStamp = RTUtil.GameTime,
            };
        }

        public override bool Pop(FlightComputer fc)
        {
            if (fc != null)
            {
                //remove active command if existing
                var activeCommand = FlightControlCommand.findActiveControlCmd(fc);
                if(activeCommand != null && activeCommand.CmdGuid != this.CmdGuid)
                {
                    activeCommand.Abort();
                }

                //build custom string
                var list = new string[3];
                var count = 0;
                if (ignorePitchOutput) { list[count++] = "Pitch ignored"; }
                if (ignoreHeadingOutput) { list[count++] = "Heading ignored"; }
                if (ignoreRollOutput) { list[count++] = "Roll ignored"; }

                for(int i=0; i<count;i++)
                {
                    stringReady += list[i];
                    if (i < count -1) { stringReady += ", "; }
                }
                if (stringReady.Length > 0) { stringReady += Environment.NewLine; }

                return true;
            }
            return false;
        }

        public override bool Execute(FlightComputer fc, FlightCtrlState ctrlState)
        {
            SteeringHelper.FlightOutputControlMask = 0; //blank off

            if (mAbort || (!ignorePitchOutput && !ignoreHeadingOutput && !ignoreRollOutput)) { return true; }

            if (ignorePitchOutput) { SteeringHelper.FlightOutputControlMask |= SteeringHelper.FlightControlOutput.IgnorePitch; }
            if (ignoreHeadingOutput) { SteeringHelper.FlightOutputControlMask |= SteeringHelper.FlightControlOutput.IgnoreHeading; }
            if (ignoreRollOutput) { SteeringHelper.FlightOutputControlMask |= SteeringHelper.FlightControlOutput.IgnoreRoll; }

            return false;
        }

        public static FlightControlCommand findActiveControlCmd(FlightComputer fc)
        {
            var cmdItr = fc.ActiveCommands.GetEnumerator();
            while (cmdItr.MoveNext())
            {
                if (cmdItr.Current is FlightControlCommand)
                {
                    return cmdItr.Current as FlightControlCommand;
                }
            }

            return null;
        }

        public override void Abort() { mAbort = true; }
    }
}
