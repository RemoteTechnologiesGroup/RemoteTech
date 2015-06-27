using RemoteTech.SimpleTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    public class VesselSatellite : ISatellite
    {
        public bool Visible
        {
            get { return SignalProcessor.Visible; }
        }

        public String Name
        {
            get { return SignalProcessor.VesselName; }
            set { SignalProcessor.VesselName = value; }
        }

        public Guid Guid
        {
            get { return SignalProcessor.Guid; }
        }

        public Vector3d Position
        {
            get { return SignalProcessor.Position; }
        }

        public CelestialBody Body
        {
            get { return SignalProcessor.Body; }
        }

        public Color MarkColor
        {
            get { return new Color(0.996078f, 0, 0, 1); }
        }

        public List<ISignalProcessor> SignalProcessors { get; set; }

        public bool Powered
        {
            get { return SignalProcessors.Any(s => s.Powered); }
        }

        public bool IsCommandStation
        {
            get { return SignalProcessors.Any(s => s.IsCommandStation); }
        }

        public ISignalProcessor SignalProcessor
        {
            get
            {
                return SignalProcessors.FirstOrDefault(s => s.FlightComputer != null) ?? SignalProcessors[0];
            }
        }

        public bool HasLocalControl
        {
            get
            {
                return RTUtil.CachePerFrame(ref mLocalControl, () => SignalProcessor.Vessel.HasLocalControl());
            }
        }

        public bool isVessel { get { return true; } }

        public Vessel parentVessel
        {
            get
            {
                return SignalProcessor.Vessel;
            }
        }

        public IEnumerable<IAntenna> Antennas
        {
            get { return RTCore.Instance.Antennas[this]; }
        }

        bool ISatellite.isHibernating
        {
        	get { return isHibernating; }
        }

        public bool isHibernating = false;

        public IEnumerable<PartResource> ElectricChargeResources {
			get
			{
				if (this.parentVessel != null && this.parentVessel.rootPart != null) {
					int ecid = PartResourceLibrary.Instance.GetDefinition ("ElectricCharge").id;
					List<PartResource> resources = new List<PartResource> ();
					this.parentVessel.rootPart.GetConnectedResources (ecid, ResourceFlowMode.ALL_VESSEL, resources);
					return resources;
				}
				return new List<PartResource>();
            }
        }

        public double TotalElectricCharge
        {
            get { return this.ElectricChargeResources.Sum(x => x.amount); }
        }

        public double TotalElectricChargeCapacity
        {
        	get { return this.ElectricChargeResources.Sum(x => x.maxAmount); }
        }

        public double ElectricChargeFillLevel
        {
        	get { return this.TotalElectricCharge / this.TotalElectricChargeCapacity; }
        }

        public FlightComputer.FlightComputer FlightComputer
        {
            get { return SignalProcessor.FlightComputer; }
        }

        // Helpers
        public List<NetworkRoute<ISatellite>> Connections
        {
            get { return RTCore.Instance.Network[this]; }
        }

        public void OnConnectionRefresh(List<NetworkRoute<ISatellite>> routes)
        {
        	double level = ElectricChargeFillLevel;
        	if (!isHibernating && level < 0.1f)
        	{
        		RTLog.Notify("Vessel entered hibernation");
        		isHibernating = true;
        	}
			else if (isHibernating && level > 0.9f)
			{
        		RTLog.Notify("Vessel awoke from hibernation");
				isHibernating = false;
            }
            foreach (IAntenna a in Antennas)
            {
                a.OnConnectionRefresh();
            }
        }

        private CachedField<bool> mLocalControl;

        public VesselSatellite(List<ISignalProcessor> parts)
        {
            if (parts == null) throw new ArgumentNullException();
            SignalProcessors = parts;
        }

        public override String ToString()
        {
            return String.Format("VesselSatellite({0}, {1})", Name, Guid);
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }
    }
}