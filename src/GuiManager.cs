using System;
using UnityEngine;

namespace RemoteTech {
    public class GuiManager : IDisposable, IConfigNode {
        private readonly MapViewConfigFragment mConfig = new MapViewConfigFragment();
        private readonly TimeQuadrantPatcher mPatcher = new TimeQuadrantPatcher();
        private readonly RTCore mCore;

        public GuiManager(RTCore core) {
            mCore = core;
            if (TimeWarp.fetch != null) {
                mPatcher.Patch(TimeWarp.fetch);
            }
            mCore.GuiUpdated += Draw;
        }

        public void Dispose() {
            mCore.GuiUpdated -= Draw;
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
            VesselSatellite vs = mCore.Satellites[v];
            if (vs != null && vs.FlightComputer != null) {
                (new FlightComputerWindow(vs.FlightComputer)).Show();
            }
        }

        public void OpenFlightComputer(ISignalProcessor sp) {
            if (sp.FlightComputer != null) {
                (new FlightComputerWindow(sp.FlightComputer)).Show();
            }
        }

        public void OpenAntennaConfig(IAntenna a, Vessel v) {
            ISatellite s = mCore.Satellites[v];
            if (s != null) {
                (new AntennaWindow(a, s)).Show();
            }
        }

        public void OpenAntennaConfig(IAntenna a, ISatellite s) {
            (new AntennaWindow(a, s)).Show();
        }
    }
}
