using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public abstract class AbstractCommand : ICommand
    {
        public double TimeStamp { get; set; }
        public virtual double ExtraDelay { get; set; }
        public virtual String Description {
            get
            {
                double delay = Math.Max(TimeStamp - RTUtil.GameTime, 0);
                if (delay > 0 || ExtraDelay > 0)
                {
                    var extra = ExtraDelay > 0 ? String.Format("{0} + {1}", RTUtil.FormatDuration(delay), RTUtil.FormatDuration(ExtraDelay)) 
                                               : RTUtil.FormatDuration(delay);
                    return "Signal delay: " + extra;
                }
                return "";
            }
        }
        public virtual int Priority { get { return 255; } }

        // true: move to active.
        public virtual bool Pop(FlightComputer f) { return false; }

        // true: delete afterwards.
        public virtual bool Execute(FlightComputer f, FlightCtrlState fcs) { return true; }

        public int CompareTo(ICommand dc)
        {
            return TimeStamp.CompareTo(dc.TimeStamp);
        }
    }
}
