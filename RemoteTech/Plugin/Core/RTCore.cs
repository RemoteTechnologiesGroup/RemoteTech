using System;
using UnityEngine;

namespace RemoteTech {
    public sealed class RTCore : MonoBehaviour {

        static RTCore mSingleton;

        public static RTCore Instance() {
            if (mSingleton == null) {
                GameObject obj = GameObject.Find("RTCore") ?? new GameObject("RTCore", typeof(RTCore));
                mSingleton = obj.GetComponent<RTCore>();
            }
            return mSingleton;
        }

        public RTSettings Settings { get; private set; }
        public RTAssets Assets { get; private set; }

        MissionControlGUI mMissionControl;
        SatelliteNetwork mSatelliteNetwork;
        PathRenderer mPathRenderer;

        int mCounter = 0;

        public void Awake() {
            RTUtil.Log("RTCore awake.");
            DontDestroyOnLoad(this);

            mMissionControl = new MissionControlGUI();
            mSatelliteNetwork = new SatelliteNetwork();
            mPathRenderer = new PathRenderer(mSatelliteNetwork);

            Settings = new RTSettings();
            Assets = new RTAssets();

            Settings.Load();
            Assets.Load();
            mMissionControl.Load();

            RTUtil.Log("RTCore loaded.");
        }

        public void OnGUI() {
            mMissionControl.Draw();
            mPathRenderer.Draw();
        }

        public void OnLateUpdate() {
            mPathRenderer.Draw();
        }

        public void FixedUpdate() {
            mCounter = (mCounter+1)%25;
            if(mCounter == 0) {
                RTUtil.Log("Tick!");
                if(FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null) {
                    mSatelliteNetwork.Update();
                    mSatelliteNetwork.FindCommandPath(FlightGlobals.ActiveVessel);
                    mPathRenderer.Update();
                }
            }

        }

    }

    class RemoteTechCoreHook : PartModule {

        static RTCore mCore;

        public override void OnAwake() {
            mCore = RTCore.Instance();
            RTUtil.Log("RemoteTechCoreHook loaded.");
        }
    }

}
