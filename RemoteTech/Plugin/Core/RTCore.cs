using System;
using UnityEngine;

namespace RemoteTech {
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class RTCore : MonoBehaviour {

        public static RTCore Instance { get; private set; }

        public RTSettings Settings { get; private set; }
        public RTAssets Assets { get; private set; }
        public RTConnectionManager Network { get; private set; }
        public RTSatelliteManager Satellites { get; private set; }
        public RTAntennaManager Antennas { get; private set; }

        PathRenderer mPathRenderer;
        RTGUIManager mGuiManager;

        int mCounter = 0;

        void Init() {
            Instance = GameObject.Find("RTCore").GetComponent<RTCore>();

            RTUtil.Log("RTCore awake.");

            Settings = new RTSettings(this);
            Assets = new RTAssets(this);
            Satellites = new RTSatelliteManager(this);
            Antennas = new RTAntennaManager(this);
            Network = new RTConnectionManager(this);

            mPathRenderer = new PathRenderer(Network);
            mGuiManager = new RTGUIManager(this);
           
            RegisterEvents();

            RTUtil.Log("RTCore loaded.");
        }

        public void Start() {
            Init();
            foreach(Vessel v in FlightGlobals.Vessels) {
                Satellites.RegisterFor(v);
            }
        }

        public void FixedUpdate() {
            if (mCounter == 0) {
                RTUtil.Log("Tick");
                StartCoroutine(Network.EstablishConnection());
            }
            mCounter = (mCounter+1)%50;
        }

        public void OnGUI() {
            mGuiManager.Draw();
        }

        public void LateUpdate() {
            mPathRenderer.Draw();
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
            Instance = null;
        }
    }
}
