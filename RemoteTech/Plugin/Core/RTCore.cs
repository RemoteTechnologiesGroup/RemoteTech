using System;
using UnityEngine;

namespace RemoteTech {
    public class RTCore : MonoBehaviour {
        public static RTCore Instance { get; protected set; }

        public RTSettings Settings { get; protected set; }
        public RTNetworkManager Network { get; protected set; }
        public RTSatelliteManager Satellites { get; protected set; }
        public RTAntennaManager Antennas { get; protected set; }
        public RTGUIManager GUI { get; protected set; }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class RTCoreFlight : RTCore {
        RTNetworkRenderer mPathRenderer;

        void Init() {
            RTCore.Instance = GameObject.Find("RTCoreFlight").GetComponent<RTCoreFlight>();

            Settings = new RTSettings(this);
            Satellites = new RTSatelliteManager(this);
            Antennas = new RTAntennaManager(this);
            Network = new RTNetworkManager(this);
            GUI = new RTGUIManager(this);

            mPathRenderer = RTNetworkRenderer.Attach(this);
            mPathRenderer.Show();
           
            RegisterEvents();

            RTUtil.Log("RTCore loaded.");
        }

        public void Start() {
            Init();
            foreach(Vessel v in FlightGlobals.Vessels) {
                Satellites.RegisterProtoFor(v);
                Antennas.RegisterProtoFor(v);
            }
        }

        public void FixedUpdate() {
            StartCoroutine(Network.Tick());
        }

        public void OnGUI() {
            GUI.Draw();
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
            GUI.Dispose();
            UnregisterEvents();
            Instance = null;
        }
    }

    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    class RTCoreTracking : RTCore {

        RTNetworkRenderer mPathRenderer;

        void Init() {
            RTCore.Instance = GameObject.Find("RTCoreTracking").GetComponent<RTCoreTracking>();

            Settings = new RTSettings(this);
            Satellites = new RTSatelliteManager(this);
            Antennas = new RTAntennaManager(this);
            Network = new RTNetworkManager(this);

            mPathRenderer = RTNetworkRenderer.Attach(this);
            mPathRenderer.Show();
        }

        public void Start() {
            Init();
            foreach (Vessel v in FlightGlobals.Vessels) {
                Satellites.RegisterProtoFor(v);
                Antennas.RegisterProtoFor(v);
            }
        }

        public void FixedUpdate() {
            StartCoroutine(Network.Tick());
        }

        void OnDestroy() {
            Satellites.Dispose();
            Antennas.Dispose();
            Instance = null;
        }
    }
}
