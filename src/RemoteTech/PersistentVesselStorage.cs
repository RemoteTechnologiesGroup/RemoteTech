using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using RemoteTech.Modules;
using UnityEngine;

namespace RemoteTech
{
    /// <summary>
    /// Class keeping track of RemoteTech satellites.
    /// Acts as a list of vessels managed by RemoteTech.
    /// </summary>
    /// 
    public class FlatVesselInfo
    {
        public Guid Guid;
        public string Name;
        public CelestialBody Body;

        public FlatVesselInfo(Guid Id, string Name, CelestialBody Body)
        {
            this.Guid = Id;
            this.Name = Name;
            this.Body = Body;
        }
    }

    [KSPAddon(KSPAddon.Startup.AllGameScenes, true)]
    public class PersistentVesselStorage : MonoBehaviour, IEnumerable<FlatVesselInfo>
    {
        
        public static PersistentVesselStorage Instance { get; protected set;}
        public readonly List<FlatVesselInfo> VesselInfoCache = new List<FlatVesselInfo>();

        public void Start()
        {
            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);
            if (Instance == null)
            {
                Instance = this;
            }
            DontDestroyOnLoad(this);
        }

        public void CacheSatList()
        {
            VesselInfoCache.Clear();
            foreach (ISatellite s in RTCore.Instance.Network)
            {
                // only real sats and do not cache the active vessel
                // dont want the active vessel when reverting.
                if((s.GetType()==typeof(MissionControlSatellite)) || 
                    (s.parentVessel != null && s.parentVessel != FlightGlobals.ActiveVessel))
                {
                    VesselInfoCache.Add(new FlatVesselInfo(s.Guid, s.Name, s.Body));
                }
            }
        }

        public void OnGameSceneLoadRequested(GameScenes Scene)
        {
            // when the current scene has vessels loaded
            if (Scene!=GameScenes.MAINMENU && Scene == GameScenes.EDITOR && FlightGlobals.Vessels.Count != 0)
            {
                CacheSatList();
            }
        }

        public IEnumerator<FlatVesselInfo> GetEnumerator()
        {
            return VesselInfoCache.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)VesselInfoCache;
        }

        IEnumerator<FlatVesselInfo> IEnumerable<FlatVesselInfo>.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}