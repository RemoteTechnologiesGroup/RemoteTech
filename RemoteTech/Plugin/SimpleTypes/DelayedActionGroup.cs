using System;
using System.Text;

namespace RemoteTech {
    public class DelayedActionGroup : DelayedCommand {
        public KSPActionGroup ActionGroup { get; private set; }

        public DelayedActionGroup(KSPActionGroup group, double time) {
            ActionGroup = group;

            TimeStamp = time;
            ExtraDelay = 0;
        }

        public override String ToString() {
            StringBuilder s = new StringBuilder();
            s.Append("Toggle ");
            s.AppendLine(ActionGroup.ToString());
            double time = TimeStamp - RTUtil.GetGameTime();
            if (time > 0) {
                s.Append("Active in: ");
                s.Append(time.ToString("F2"));
                s.AppendLine("s");
            }
            return s.ToString().TrimEnd('\n');
        }
    }
}