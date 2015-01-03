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
        public virtual double Delay { get { return Math.Max(TimeStamp - RTUtil.GameTime, 0); } }
        public virtual String Description {
            get
            {
                double delay = this.Delay;
                if (delay > 0 || ExtraDelay > 0)
                {
                    var extra = ExtraDelay > 0 ? String.Format("{0} + {1}", RTUtil.FormatDuration(delay), RTUtil.FormatDuration(ExtraDelay)) 
                                               : RTUtil.FormatDuration(delay);
                    return "Signal delay: " + extra;
                }
                return "";
            }
        }
        public abstract String ShortName { get; }
        public virtual int Priority { get { return 255; } }

        // true: move to active.
        public virtual bool Pop(FlightComputer f) { return false; }

        // true: delete afterwards.
        public virtual bool Execute(FlightComputer f, FlightCtrlState fcs) { return true; }

        public virtual void Abort() { }

        public int CompareTo(ICommand dc)
        {
            return TimeStamp.CompareTo(dc.TimeStamp);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="fc"></param>
        public virtual void Save(ConfigNode n, FlightComputer fc)
        {
            ConfigNode save = new ConfigNode(this.GetType().Name);
            try
            {
                ConfigNode.CreateConfigFromObject(this, 0, save);
            }
            catch (Exception) {
            }

            save.AddValue("TimeStamp", TimeStamp);
            save.AddValue("ExtraDelay", ExtraDelay);
            n.AddNode(save);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="fc"></param>
        public virtual void Load(ConfigNode n, FlightComputer fc)
        {
            // nothing
            if (n.HasValue("TimeStamp"))
            {
                TimeStamp = double.Parse(n.GetValue("TimeStamp"));
            }
            if (n.HasValue("ExtraDelay"))
            {
                ExtraDelay = double.Parse(n.GetValue("ExtraDelay"));
            }
        }

        protected ConfigNode getCommandConfigNode(ConfigNode n)
        {
            return n.GetNode(this.GetType().Name);
        }
    }
}
