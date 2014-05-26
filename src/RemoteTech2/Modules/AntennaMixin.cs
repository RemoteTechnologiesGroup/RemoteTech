using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class AntennaMixin : IVesselAntenna, IDisposable, IConfigNode
    {
        private static ILogger Logger = RTLogger.CreateLogger(typeof(AntennaMixin));

        // IAntenna 
        public event Action<IVesselAntenna> Destroyed = delegate { };
        public bool Broken              { get { return _.Broken; } }
        public bool Powered             { get { return _.Powered; } }
        public bool Activated           { get { return _.Activated; } set { _.Activated = SetState(value); } }
        IList<Target> IAntenna.Targets  { get { return _.Targets; } }
        public EventListWrapper<Target> Targets { get { return _.Targets; } }
        public String Name              { get { return getName(); } }
        public Guid Guid                { get { return Vessel.Guid; } }
        public Vector3d Position        { get { return Vessel.Position; } }
        public bool CanTarget           { get { return _.ActiveDishRange > 0; } }
        public float CurrentDishRange   { get { return (Activated && Powered && !_.Broken) ? _.ActiveDishRange * RangeMultiplier : 0.0f; } }
        public double CurrentRadians    { get { return _.Radians; } }
        public float CurrentOmniRange   { get { return (Activated && Powered && !_.Broken) ? _.ActiveOmniRange * RangeMultiplier : 0.0f; } }
        public float CurrentConsumption { get { return (Activated && !_.Broken) ? _.Consumption * ConsumptionMultiplier : 0.0f; } }
        public IVessel Vessel           { get { return getVessel(); } }

        // Parameters
        public float ActiveDishRange { get { return _.ActiveDishRange; } }
        public float ActiveOmniRange { get { return _.ActiveOmniRange; } }
        public float ActiveConsumption { get { return _.Consumption; } }
        public float DishAngle { get { return _.DishAngle; } }

        // Shortcuts
        private float RangeMultiplier { get { return RTSettings.Instance.RangeMultiplier; } }
        private float ConsumptionMultiplier { get { return RTSettings.Instance.ConsumptionMultiplier; } }

        private class Store {
            public const String Node = "Antenna";

            [Persistent] public bool Activated = false;
            [Persistent] public bool Powered = false;
            [Persistent] public bool Broken = false;
            [Persistent] public float DishRange = 0.0f;
            [Persistent] public float OmniRange = 0.0f;
            [Persistent] public double Radians = 1.0d;
            [Persistent] public EventListWrapper<Target> Targets = new EventListWrapper<Target>(new List<Target>());

            public String OffName = "Off";
            public String OnName = "Operational";
            public String ActionOffName = "Deactivate";
            public String ActionOnName = "Activate";
            public String ActionToggleName = "Toggle";

            public float ActiveDishRange = -1;
            public float ActiveOmniRange = 0;
            public float Consumption = 0;
            public float DishAngle = 0;
        }

        private Store _ = new Store();

        private Func<String> getName;
        private Func<IVessel> getVessel;
        private Func<String, double, double> requestResource;
        private AnimationMixin animation;
        private TransmitterMixin transmitter;

        public AntennaMixin(Func<IVessel> getVessel, Func<String> getName,
                            Func<String, double, double> requestResource,
                            AnimationMixin animation = null, 
                            TransmitterMixin transmitter = null)
        {
            this.getVessel = getVessel;
            this.getName = getName;
            this.requestResource = requestResource;
            this.animation = animation;
            this.transmitter = transmitter;
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

        public bool SetState(bool state)
        {
            state &= !Broken;

            if (animation != null)
                animation.Deployed = state;

            if (transmitter != null)
                transmitter.CheckTransmitter();

            return state;
        }

        public void Start()
        {
            _.Radians = (Math.Cos(DishAngle / 2 * Math.PI / 180));

            if (RTCore.Instance != null)
            {
                RTCore.Instance.Antennas.Register(this);

                if (transmitter != null) {
                    RTCore.Instance.Network.ConnectionAdded += UpdateTransmitter;
                    RTCore.Instance.Network.ConnectionRemoved += UpdateTransmitter;
                }
            }
        }

        public void Dispose()
        {
            Destroyed(this);
            if (RTCore.Instance != null)
            {
                RTCore.Instance.Network.ConnectionAdded -= UpdateTransmitter;
                RTCore.Instance.Network.ConnectionRemoved -= UpdateTransmitter;
            }
        }

        public State Update()
        {
            _.DishRange = CurrentDishRange;
            _.OmniRange = CurrentOmniRange;

            _.Powered = false;

            if (RTCore.Instance == null)
                return State.Operational;


            if (Broken)
                return State.Malfunction;

            if (!Activated)
                return State.Off;

            var request = new ModuleResource();
            double resourceRequest = ActiveConsumption * TimeWarp.fixedDeltaTime;
            double resourceAmount = requestResource("ElectricCharge", resourceRequest);
            if (resourceAmount < resourceRequest * 0.9)
                return State.NoResources;

            _.Powered = true;
            return State.Operational;
        }

        private void UpdateTransmitter(ISatellite station, ISatellite target)
        {
            if (target.Guid != Guid) return;
            transmitter.CheckTransmitter();
        }

        public enum State
        {
            Off,
            Operational,
            NoResources,
            Malfunction,
        }
    }
}
