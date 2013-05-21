using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{


    public class RelayNode : IEquatable<RelayNode>
    {
        Vessel vessel = null;
        bool hasCommand = false;
        bool hasAntenna = false;
        bool hasDish = false;
        float antennaRange = 0;
        CelestialBody referenceBody = null;
        List<DishData> dishData = new List<DishData>();

        void ConstructFromUnloaded()
        {
            foreach (ProtoPartSnapshot p in this.vessel.protoVessel.protoPartSnapshots)
            {
                foreach (ProtoPartModuleSnapshot s in p.modules)
                {
                    ConfigNode n = new ConfigNode();
                    s.Save(n);
                    if (n.HasValue("isRemoteCommand") && bool.Parse(n.GetValue("isRemoteCommand")))
                    {
                        this.hasCommand = true;
                        break;
                    }
                }
                if (hasCommand) break;
            }

            foreach (ProtoPartSnapshot p in this.vessel.protoVessel.protoPartSnapshots)
            {
                foreach (ProtoPartModuleSnapshot s in p.modules)
                {
                    ConfigNode n = new ConfigNode();
                    s.Save(n);

                    if (n.HasValue("antennaRange"))
                    {
                        float lngth = float.Parse(n.GetValue("antennaRange"));
                        if (lngth > this.antennaRange)
                        {
                            this.hasAntenna = true;
                            this.antennaRange = lngth;
                        }
                    }

                    if (n.HasValue("dishRange"))
                    {
                        DishData tmp = new DishData();

                        tmp.dishRange = float.Parse(n.GetValue("dishRange"));

                        tmp.pointedAt = n.HasValue("pointedAt") ? n.GetValue("pointedAt") : "None";

                        if (tmp.dishRange > 0)
                        {
                            this.hasDish = true;
                            this.dishData.Add(tmp);
                        }
                    }

                }
            }
        }




        void ConstructFromLoaded()
        {
            foreach (Part p in vessel.parts)
            {
                foreach (PartModule m in p.Modules)
                    if (RTUtils.containsField(m, "isRemoteCommand") && (bool)m.Fields.GetValue("isRemoteCommand"))
                    {
                        this.hasCommand = true;
                        break;
                    }
                if (hasCommand) break;
            }

            foreach (Part p in vessel.parts)
                foreach (PartModule m in p.Modules)
                {
                    if (RTUtils.containsField(m, "antennaRange"))
                    {
                        float lngth = (float)m.Fields.GetValue("antennaRange");
                        if (lngth > this.antennaRange)
                        {
                            this.hasAntenna = true;
                            this.antennaRange = lngth;
                        }
                    }

                    if (RTUtils.containsField(m, "dishRange"))
                    {
                        DishData tmp = new DishData();

                        tmp.dishRange = m.Fields.GetValue("dishRange") == null ? 0 : (float)m.Fields.GetValue("dishRange");

                        tmp.pointedAt = m.Fields.GetValue("pointedAt") == null ? "None" : (string)m.Fields.GetValue("pointedAt");

                        if (tmp.dishRange > 0)
                        {
                            this.hasDish = true;
                            this.dishData.Add(tmp);
                        }
                    }
                }

        }

        public RelayNode(Vessel v)
        {
            this.vessel = v;

            if (v.loaded)
                ConstructFromLoaded();
            else
                ConstructFromUnloaded();
        }


        public RelayNode()
        {
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body.name.ToLower().Contains("kerbin"))
                {
                    referenceBody = body;
                    break;
                }
            }
            this.hasCommand = true;
            this.hasAntenna = true;
            this.antennaRange = 5000;
        }


        public void Reload()
        {
            if (this.vessel == null) return;
            this.dishData.Clear();
            this.hasAntenna = false;
            this.hasDish = false;
            this.hasCommand = false;
            this.antennaRange = 0;

            if (this.IsLoaded)
                ConstructFromLoaded();
            else
                ConstructFromUnloaded();
        }

        public Vessel Vessel
        {
            get
            {
                return this.vessel;
            }
        }

        public string TypeName
        {
            get
            {
                if (this.vessel == null) return "Celestial Body";
                return vessel.vesselType.ToString();
            }
        }

        public string ID
        {
            get
            {
                if (this.vessel == null) return "Mission Control";
                return this.vessel.id.ToString();
            }
        }

        public bool IsLoaded
        {
            get
            {
                return (this.vessel.loaded);
            }

        }

        public Vector3d Position
        {
            get
            {
                if (this.vessel == null)
                    return referenceBody.position + 600094 * referenceBody.GetSurfaceNVector(-0.11641926192966, -74.606391806057);
                return this.vessel.transform.localPosition;
            }
        }


        public Vector3d ScaledPosition
        {
            get
            {
                if (this.vessel == null)
                    return ScaledSpace.LocalToScaledSpace(referenceBody.position + 600094 * referenceBody.GetSurfaceNVector(-0.11641926192966, -74.606391806057));
                return ScaledSpace.LocalToScaledSpace(this.vessel.transform.localPosition);
            }
        }

        public bool HasCommand
        {
            get
            {
                if (vessel == null) return this.hasCommand;
                else
                {
                    if (vessel.loaded)
                        return this.hasCommand && vessel.GetCrewCount() >= RTGlobals.RemoteCommandCrew;
                    else
                        return this.hasCommand && this.vessel.protoVessel.GetVesselCrew().Count >= RTGlobals.RemoteCommandCrew;
                }

            }
        }

        public bool IsBase
        {
            get
            {
                return (this.vessel == null);
            }
        }

        public bool HasAntenna
        {
            get
            {
                return this.hasAntenna;
            }
        }

        public bool HasDish
        {
            get
            {
                return this.hasDish;
            }
        }

        public float AntennaRange
        {
            get
            {
                return this.antennaRange;
            }
        }

        public string Orbits
        {
            get
            {
                if (this.vessel == null && this.referenceBody != null) return this.referenceBody.name;
                return this.vessel.mainBody.name;
            }
        }


        public List<DishData> DishData
        {
            get
            {
                return this.dishData;
            }
        }


        public string descript
        {
            get
            {
                if (this.vessel == null) return this.ToString();
                string s;
                if (this.vessel.Splashed)
                {
                    s = this.vessel.vesselName + ": In the waters of " + this.vessel.mainBody.theName + ".";
                    if (this.hasAntenna || this.hasCommand || this.hasDish)
                    {
                        s += "\n Has ";
                        if (this.hasAntenna) s += ", antenna(" + RTUtils.length(this.antennaRange * 1000) + "m)";
                        if (this.hasDish && this.dishData.Count > 0)
                        {
                            s += ", " + this.dishData.Count + " SatDish";
                            if (this.dishData.Count > 1) s += "es";
                        }
                        if (this.hasCommand) s += ", Command";
                    }
                    return s;
                }

                if (this.vessel.Landed)
                {
                    s = this.vessel.vesselName + ": Landed on " + this.vessel.mainBody.theName + ".";
                    if (this.hasAntenna || this.hasCommand || this.hasDish)
                    {
                        s += "\n Has ";
                        if (this.hasAntenna) s += ", antenna(" + RTUtils.length(this.antennaRange * 1000) + "m)";
                        if (this.hasDish && this.dishData.Count > 0)
                        {
                            s += ", " + this.dishData.Count + " SatDish";
                            if (this.dishData.Count > 1) s += "es";
                        }
                        if (this.hasCommand) s += ", Command";
                    }
                    return s;
                }


                s = (String.Format(this.vessel.vesselName + ": {0:0}m above " + this.vessel.mainBody.theName, RTUtils.length(((this.vessel.transform.position - this.vessel.mainBody.position).magnitude - this.vessel.mainBody.Radius))));
                if (this.hasAntenna || this.hasCommand || this.hasDish)
                {
                    s += "\n Has ";
                    if (this.hasAntenna) s += ", antenna(" + RTUtils.length(this.antennaRange * 1000) + "m)";
                    if (this.hasDish && this.dishData.Count > 0)
                    {
                        s += ", " + this.dishData.Count + " SatDish";
                        if (this.dishData.Count > 1) s += "es";
                    }
                    if (this.hasCommand) s += ", Command";
                }
                return s;
            }
        }

        public string LongName
        {
            get
            {
                if (this.vessel == null)
                    return "";
                else
                    return this.vessel.vesselName + " (" + this.vessel.mainBody.theName + ")";
            }
        }

        public double semiMajorAxis
        {
            get
            {
                if (this.vessel == null)
                    return 0;
                if (this.vessel.LandedOrSplashed)
                    return 0;
                return this.vessel.orbit.semiMajorAxis;
            }
        }

        public bool MainBodyIs(CelestialBody CheckBody)
        {
            if (this.vessel == null)
                return false;
            return this.vessel.mainBody == CheckBody;
        }

        public string VesselName
        {
            get
            {
                if (this.vessel == null)
                    return "";
                return this.vessel.vesselName;
            }
        }

        public bool Equals(RelayNode other)
        {
            return (this.Vessel == other.Vessel && this.Position == other.Position);
        }

        public override String ToString()
        {
            if (this.vessel == null) return "Mission Control";
            return this.vessel.vesselName;
        }
    }
}
