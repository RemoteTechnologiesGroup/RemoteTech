using System;
using System.Text;

namespace RemoteTech {
    public class DelayedActionGroup : IComparable<DelayedActionGroup> {
        public KSPActionGroup ActionGroup { get; private set; }
        public double EffectiveFrom { get; set; }

        public DelayedActionGroup(KSPActionGroup group, double time) {
            ActionGroup = group;
            EffectiveFrom = time;
        }

        public int CompareTo(DelayedActionGroup ag) {
            return this.EffectiveFrom.CompareTo(ag.EffectiveFrom);
        }

        public override String ToString() {
            StringBuilder s = new StringBuilder();
            s.Append("Toggle ");
            s.AppendLine(ActionGroup.ToString());
            double time = EffectiveFrom - RTUtil.GetGameTime();
            if (time > 0) {
                s.Append("Active in: ");
                s.Append(time.ToString("F2"));
                s.AppendLine("s");
            }
            return s.ToString().TrimEnd('\n');
        }
    }
}