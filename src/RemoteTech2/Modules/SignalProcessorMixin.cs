using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class SignalProcessorMixin : IConfigNode, ISignalProcessor, IDisposable
    {
        private static ILogger Logger = RTLogger.CreateLogger(typeof(SignalProcessorMixin));

        public event Action<ISignalProcessor> Destroyed = delegate { };
        public String Name                   { get { return getName(); } }
        public Guid Guid                     { get { return Vessel.Guid; } }
        public bool Powered                  { get { return Powered; } set { Powered = value; } }
        public Group Group                   { get { return Group; } set { Group = value; } }
        public bool? IsCommandStation        { get { return _.CommandStation.CrewRequirement > -1 ? (Vessel.CrewCount > _.CommandStation.CrewRequirement ? true : false) : (bool?)null; } }
        public FlightComputer FlightComputer { get { return _.FlightComputer; } }
        public IVessel Vessel                { get { return getVessel(); } }

        public int CrewRequirement { get { return _.CommandStation.CrewRequirement; } }
        private VesselSatellite Satellite { get { return RTCore.Instance.Satellites[Guid]; } }

        private class Store
        {
            public const String Node = "SignalProcessor";

            [Persistent] public bool HackControls;
            [Persistent] public bool Powered;
            [Persistent] public Group Group;
            [Persistent] public CommandStation CommandStation;
            [Persistent] public FlightComputer FlightComputer;
        }

        private Store _ = new Store();

        private Func<String> getName;
        private Func<IVessel> getVessel;
        private Func<bool> hasControl;

        public SignalProcessorMixin(Func<String> getName, 
                                    Func<IVessel> getVessel,
                                    Func<bool> hasControl)
        {
            this.getName = getName;
            this.getVessel = getVessel;
            this.hasControl = hasControl;
        }

        public void Load(ConfigNode node)
        {
            if (node.HasNode(Store.Node))
            {
                ((IConfigNode)this).Load(node.GetNode(Store.Node));
            }
        }

        public void Save(ConfigNode node)
        {
            if (node.HasNode(Store.Node))
            {
                node.RemoveNode(Store.Node);
            }
            var save = new ConfigNode(Store.Node);
            ((IConfigNode)this).Save(save);
            node.AddNode(save);
        }

        void IConfigNode.Load(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(_, node);
        }

        void IConfigNode.Save(ConfigNode node)
        {
            ConfigNode.CreateConfigFromObject(_, 0, node);
        }

        public void Start()
        {
            if (RTCore.Instance != null)
            {
                RTCore.Instance.Satellites.Register(this);
            }

        }

        public void Dispose()
        {
            Logger.Info("Dispose");
            if (FlightComputer != null)
            {
                FlightComputer.Dispose();
            }
            Destroyed.Invoke(this);
        }

        public State Update()
        {
            if (FlightComputer != null)
            {
                FlightComputer.OnUpdate();
            }

            HookPartMenus();

            Powered = hasControl();
            if (!RTCore.Instance)
                return State.Operational;
            if (!Powered)
                return State.ParentDefect;
            if (Satellite == null || !RTCore.Instance.Network[Satellite].Any())
                return State.NoConnection;
            return State.Operational;
        }

        public void HookPartMenus()
        {
            if (!_.HackControls) return;
            UIPartActionMenuPatcher.Patch(Vessel);
        }

        public enum State
        {
            Operational,
            ParentDefect,
            NoConnection
        }
    }
}
