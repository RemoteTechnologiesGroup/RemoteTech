using System;

namespace RemoteTech.FlightComputer.Commands
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
        /// Save the basic informations for every command.
        /// </summary>
        /// <param name="n">Node to save in</param>
        /// <param name="fc">Current flightcomputer</param>
        public virtual void Save(ConfigNode n, FlightComputer fc)
        {
            ConfigNode save = new ConfigNode(this.GetType().Name);
            try
            {
                // try to serialize 'this'
                ConfigNode.CreateConfigFromObject(this, 0, save);
            }
            catch (Exception) {}

            if (Delay == 0) {
                // only save the current gametime if we have no signal delay.
                // We need this to calculate the correct delta time for the
                // ExtraDelay if we come back to this satellite.
                TimeStamp = RTUtil.GameTime;
            }

            save.AddValue("TimeStamp", TimeStamp);
            save.AddValue("ExtraDelay", ExtraDelay);
            n.AddNode(save);
        }

        /// <summary>
        /// Load the basic informations for every command.
        /// </summary>
        /// <param name="n">Node with the command infos</param>
        /// <param name="fc">Current flightcomputer</param>
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
        
        /// <summary>
        /// Load and creates a command after saving a command. Returns null if no object
        /// has been loaded.
        /// </summary>
        /// <param name="n">Node with the command infos</param>
        /// <param name="fc">Current flightcomputer</param>
        public static ICommand LoadCommand(ConfigNode n, FlightComputer fc)
        {
            ICommand command = null;

            // switch the different commands
            switch (n.name)
            {
                case "AttitudeCommand":     { command = new AttitudeCommand(); break; }
                case "ActionGroupCommand":  { command = new ActionGroupCommand(); break; }
                case "BurnCommand":         { command = new BurnCommand(); break; }
                case "ManeuverCommand":     { command = new ManeuverCommand(); break; }
                case "CancelCommand":       { command = new CancelCommand(); break; }
                case "TargetCommand":       { command = new TargetCommand(); break; }
                case "EventCommand":        { command = new EventCommand(); break; }
                case "DriveCommand":        { command = new DriveCommand(); break; }
            }

            if (command != null)
            {
                ConfigNode.LoadObjectFromConfig(command, n);
                // additional loadings
                command.Load(n, fc);
            }

            return command;
        }
    }
}
