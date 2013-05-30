using System;
using UnityEngine;

namespace RemoteTech {
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class RTCore : MonoBehaviour {

        public static RTCore Instance { get; private set; }

        public RTSettings Settings { get; private set; }
        public RTAssets Assets { get; private set; }
        public RTSatelliteNetwork Network { get; private set; }
        public RTSatelliteManager Satellites { get; private set; }
        public RTAntennaManager Antennas { get; private set; }

        PathRenderer mPathRenderer;

        Vessel mCurrentActiveVessel;

        int mCounter = 0;

        void Init() {
            Instance = GameObject.Find("RTCore").GetComponent<RTCore>();
            if (Instance != this) {
                DestroyImmediate(this);
                return;
            }

            RTUtil.Log("RTCore awake.");

            Settings = new RTSettings(this);
            //Assets = new RTAssets(this);
            Satellites = new RTSatelliteManager(this);
            Antennas = new RTAntennaManager(this);
            Network = new RTSatelliteNetwork(this);

            mPathRenderer = new PathRenderer(Network);
           
            RegisterEvents();

            RTUtil.Log("RTCore loaded.");
        }

        public void Start() {
            Init();
            foreach(Vessel v in FlightGlobals.Vessels) {
                if(v.HasSignalProcessor()) {
                    Satellites.RegisterFor(v);
                    Antennas.RegisterProtoFor(v);
                }
            }
        }

        public void FixedUpdate() {
            if (mCounter == 0) {
                RTUtil.Log("Tick");
                if (FlightGlobals.ActiveVessel != null) {
                    mCurrentActiveVessel = FlightGlobals.ActiveVessel;
                    ISatellite sat = Satellites.For(mCurrentActiveVessel);
                    if (sat != null) {
                        Network.FindCommandPath(sat);
                        mPathRenderer.UpdateLineCache();
                    }
                }
            }
            mCounter = (mCounter+1)%50;
        }

        public void LateUpdate() {
            mPathRenderer.Draw();
            if(Input.GetKeyDown(KeyCode.H)) {
                (new SatelliteGUIWindow(Satellites.For(FlightGlobals.ActiveVessel))).Show();
            }
        }

        void RegisterEvents() {
            MapView.OnEnterMapView += mPathRenderer.Show;
            MapView.OnExitMapView += mPathRenderer.Hide;
        }

        void UnregisterEvents() {
            MapView.OnEnterMapView -= mPathRenderer.Show;
            MapView.OnExitMapView -= mPathRenderer.Hide;
        }

        void OnDestroy() {
            Satellites.Dispose();
            Antennas.Dispose();
            UnregisterEvents();
        }
    }
}
