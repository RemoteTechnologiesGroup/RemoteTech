using System;
using UnityEngine;

namespace RemoteTech {
    public class GuiManager : IDisposable, IConfigNode {
        private readonly MapViewConfigFragment mConfig = new MapViewConfigFragment();
        private readonly TimeQuadrantPatcher mPatcher = new TimeQuadrantPatcher();

        public GuiManager() {
            if (TimeWarp.fetch != null) {
                mPatcher.Patch(TimeWarp.fetch);
            }
            RTCore.Instance.GuiUpdated += Draw;
        }

        public void Dispose() {
            if (RTCore.Instance != null) {
                RTCore.Instance.GuiUpdated -= Draw;
            }
            
            mPatcher.Undo();
            mConfig.Dispose();
        }

        public void Load(ConfigNode node) {

        }

        public void Save(ConfigNode node) {

        }

        public void Draw() {
            if(MapView.MapIsEnabled) {
                mConfig.Draw();
            }
        }

        public void OpenFlightComputer() {
            Vessel v = FlightGlobals.ActiveVessel;
            OpenFlightComputer(v);
        }

        public void OpenFlightComputer(Vessel v) {
            VesselSatellite vs = RTCore.Instance.Satellites[v];
            if (vs != null && vs.Master.FlightComputer != null) {
                (new FlightComputerWindow(vs.Master.FlightComputer)).Show();
            }
        }

        public void OpenFlightComputer(ISignalProcessor sp) {
            if (sp.FlightComputer != null) {
                (new FlightComputerWindow(sp.FlightComputer)).Show();
            }
        }

        public void OpenAntennaConfig(IAntenna a, Vessel v) {
            ISatellite s = RTCore.Instance.Satellites[v];
            if (s != null) {
                (new AntennaWindow(a, s)).Show();
            }
        }

        public void OpenAntennaConfig(IAntenna a, ISatellite s) {
            (new AntennaWindow(a, s)).Show();
        }
    }
}
