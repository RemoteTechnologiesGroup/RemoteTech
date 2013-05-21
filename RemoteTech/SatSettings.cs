using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{

    public class SatSettingNode
    {
        public bool isLoaded;
        public string antennaName = "Dish";
        public ProtoPartModuleSnapshot protoPartModule;
        public PartModule partModule;
        public ProtoPartSnapshot snapshot;
        public Vessel vessel;
        public Target pointedAt;
        public float dishRange;
        public int selectedTarget;
        public bool SubmenuOpen = false;

        public void ReloadDishRange()
        {
            if (isLoaded)
            {
                try
                {
                    if (RTUtils.containsField(partModule, "dishRange"))
                        this.dishRange = (float)partModule.Fields.GetValue("dishRange");
                    else
                        this.dishRange = 0;
                }
                catch (Exception)
                { }
            }
            else
            {
                ConfigNode n = new ConfigNode();
                protoPartModule.Save(n);

                if (n.HasValue("dishRange"))
                    this.dishRange = float.Parse(n.GetValue("dishRange"));
                else
                    this.dishRange = 0;
            }
        }

        public SatSettingNode(PartModule m)
        {
            this.partModule = m;
            isLoaded = true;


            if (RTUtils.containsField(partModule, "pointedAt"))
                this.pointedAt = new Target((string)partModule.Fields.GetValue("pointedAt"));
            else
                this.pointedAt = new Target();

            if (RTUtils.containsField(partModule, "dishRange"))
                this.dishRange = (float)partModule.Fields.GetValue("dishRange");
            else
                this.dishRange = 0;

            if (RTUtils.containsField(partModule, "antennaName"))
                this.antennaName = (string)partModule.Fields.GetValue("antennaName");


            for (int i = 0; i < RTGlobals.targets.Count; i++)
            {
                if (pointedAt.Equals(RTGlobals.targets[i])) { selectedTarget = i; break; }
            }
        }

        public SatSettingNode(ProtoPartModuleSnapshot s, Vessel v, ProtoPartSnapshot sn)
        {
            this.protoPartModule = s;
            this.vessel = v;
            this.snapshot = sn;
            isLoaded = false;

            ConfigNode n = new ConfigNode();
            protoPartModule.Save(n);

            if (n.HasValue("pointedAt"))
                this.pointedAt = new Target(n.GetValue("pointedAt"));
            else
                this.pointedAt = new Target();

            if (n.HasValue("dishRange"))
                this.dishRange = float.Parse(n.GetValue("dishRange"));
            else
                this.dishRange = 0;

            if (n.HasValue("antennaName"))
                this.antennaName = n.GetValue("antennaName");


            for (int i = 0; i < RTGlobals.targets.Count; i++)
            {
                if (pointedAt.Equals(RTGlobals.targets[i])) { selectedTarget = i; break; }
            }


        }

        public void save()
        {
            if (this.isLoaded)
                SavefromLoaded();
            else
                SaveFromUnLoaded();
        }

        void SavefromLoaded()
        {
            this.partModule.Fields.SetValue("pointedAt", this.pointedAt.value);
            if (this.partModule.Events.Contains("UpdatePA"))
                this.partModule.Events["UpdatePA"].Invoke();
        }

        void SaveFromUnLoaded()
        {
            bool stopIt = false;
            foreach (ProtoPartSnapshot p in vessel.protoVessel.protoPartSnapshots)
            {
                for (int i = 0; i < p.modules.Count; i++)    // not foreach because you can't overwrite from it
                {
                    if (p.modules[i].Equals(protoPartModule))
                    {
                        ConfigNode n = new ConfigNode();
                        p.modules[i].Save(n);

                        n.SetValue("pointedAt", this.pointedAt.value);

                        p.modules[i] = new ProtoPartModuleSnapshot(n);

                        stopIt = true;
                        break;
                    }
                }
                if (stopIt)
                    break;
            }
        }

        public void ListTargets()
        {
            GUIStyle myskin = new GUIStyle(GUI.skin.textField);
            myskin.fontStyle = FontStyle.Bold;
            myskin.normal.textColor = Color.white;
            myskin.onNormal.textColor = Color.white;
            myskin.onHover.textColor = Color.white;
            myskin.onFocused.textColor = Color.white;
            myskin.onActive.textColor = Color.white;

            foreach (Target t in RTGlobals.targets)
            {
                if (t.isPlanet)
                    myskin.fontStyle = FontStyle.Bold;
                else
                    myskin.fontStyle = FontStyle.Normal;

                myskin.normal.textColor = t.color;

                if (GUILayout.Button(
                    t == this.pointedAt ? t.GUIname+" <": t.GUIname, myskin))
                    this.pointedAt = t;
            }
        }


    }

    public class Target
    {
        public bool isPlanet = false;
        public CelestialBody referenceBody;
        RelayNode node;
        public Color color = Color.white;
        bool emptytarget = false;

        public string GUIname;

        public Target(CelestialBody bodyIn)
        {
            this.referenceBody = bodyIn;
            this.isPlanet = true;
        }
        public Target(RelayNode nodeIn)
        { this.node = nodeIn; }

        public Target()
        {
            emptytarget = true;
        }

        public Target(string bodyOrNode)
        {
            foreach (CelestialBody bodies in FlightGlobals.Bodies)
            {
                if (bodies.name.ToLower().Equals(bodyOrNode.ToLower()))
                {
                    this.referenceBody = bodies;
                    this.isPlanet = true;
                    return;
                }
            }
            foreach (RelayNode nodes in RTGlobals.network.all)
            {
                if (nodes.ID.Equals(bodyOrNode))
                {
                    this.node = nodes;
                }
            }
            if (this.referenceBody == null && this.node == null)
            {
                emptytarget = true;
            }
        }

        public string value
        {
            get
            {
                if (referenceBody != null)
                    return referenceBody.name;
                if (node != null)
                    return node.ID;
                else
                    return "None";

            }
        }

        public string Name
        {
            get
            {
                if (referenceBody != null)
                    return referenceBody.name;
                if (node != null)
                    return node.ToString();
                else
                    return "None";
            }
        }

        public bool Equals(Target other)
        {
            if (this.emptytarget && other.emptytarget) return true;
            if (this.node != null && other.node != null)
                return this.node.ID.Equals(other.node.ID);
            else
                return (this.referenceBody != null && other.referenceBody != null && this.referenceBody == other.referenceBody);
        }
    }


    class CBOrV
    {
        CelestialBody RefBody;
        RelayNode RefNode;
        RelayNode ExclNode;
        string Prefix;
        List<CBOrV> Children;


        public CBOrV(CelestialBody inBody, RelayNode inExclNode)
        {
            this.RefBody = inBody;
            this.RefNode = null;
            this.ExclNode = inExclNode;
            Prefix = "";

            Children = new List<CBOrV>();

            if (RefBody.name.Equals("Kerbin"))
                Children.Add(new CBOrV(new RelayNode(), "	"));

            if (RefBody.orbitingBodies.Count > 0)
                foreach (CelestialBody b in RefBody.orbitingBodies)
                {
                    Children.Add(new CBOrV(b, ExclNode, "	"));
                }
            foreach (RelayNode n in RTGlobals.network.all)
                if (n.MainBodyIs(RefBody) && !ExclNode.Equals(n) && (n.HasDish || n.HasAntenna))
                    Children.Add(new CBOrV(n, "	"));
            if (Children.Count > 1)
                Children.Sort(delegate(CBOrV n1, CBOrV n2) { return n1.semiMajorAxis.CompareTo(n2.semiMajorAxis); });
        }

        public CBOrV(CelestialBody inBody, RelayNode inExclNode, string inPrefix)
        {
            this.RefBody = inBody;
            this.RefNode = null;
            this.Prefix = inPrefix;
            this.ExclNode = inExclNode;

            Children = new List<CBOrV>();

            if (RefBody.name.Equals("Kerbin"))
                Children.Add(new CBOrV(new RelayNode(), Prefix + "	"));

            if (RefBody.orbitingBodies.Count > 0)
                foreach (CelestialBody b in RefBody.orbitingBodies)
                {
                    Children.Add(new CBOrV(b, ExclNode, Prefix + "	"));
                }
            foreach (RelayNode n in RTGlobals.network.all)
                if (n.MainBodyIs(RefBody) && !ExclNode.Equals(n) && (n.HasDish || n.HasAntenna))
                    Children.Add(new CBOrV(n, Prefix + "	"));
            if (Children.Count > 1)
                Children.Sort(delegate(CBOrV n1, CBOrV n2) { return n1.semiMajorAxis.CompareTo(n2.semiMajorAxis); });

        }

        public CBOrV(RelayNode inNode, string inPrefix)
        {
            this.RefNode = inNode;
            this.RefBody = null;
            this.Prefix = inPrefix;
            Children = new List<CBOrV>();
        }

        public double semiMajorAxis
        {
            get
            {
                if (RefNode == null)
                    return RefBody.orbit.semiMajorAxis;
                else
                    return RefNode.semiMajorAxis;
            }
        }

        Target target
        {
            get
            {
                Target target;
                if (RefNode == null)
                {
                    target = new Target(RefBody);
                    target.color = RTUtils.PlanetColor(RefBody.name);
                    target.GUIname = Prefix + RefBody.theName;
                }
                else
                {
                    target = new Target(RefNode);
                    target.color = Color.white;
                    if (RefNode.IsBase)
                        target.GUIname = Prefix + RefNode.ToString();
                    else
                    {
                        target.GUIname = Prefix + RefNode.VesselName + " (" + RefNode.TypeName + ")";
                    }
                }
                return target;
            }
        }
        public void createTargets(ref List<Target> t)
        {
            t.Add(this.target);
            if (Children.Count > 0)
                foreach (CBOrV o in Children)
                    o.createTargets(ref t);
        }

    }

    public class SatSettings
    {
        RelayNode node;
        RemoteCore core;
        public int WINDOW_ID = 72168;
        List<SatSettingNode> settingNodes;
        Vector2 SettingListScroll = new Vector2();
        string vesselName;
        public bool show = false;

        public RelayNode Node
        {
            get
            {
                return this.node;
            }
        }

        public void updateDishes()
        {
            foreach (SatSettingNode node in settingNodes)
                node.ReloadDishRange();
        }

        public SatSettings(RemoteCore corein)
        {
            this.core = corein;
        }

        public void Open(RelayNode nodeIn)
        {
            if (show && nodeIn.Equals(this.node))
            {
                Close();
                return;
            }
            this.node = nodeIn;

            RTGlobals.targets = new List<Target>();

            CBOrV SortNetwork = new CBOrV(Planetarium.fetch.Sun, this.node);
            SortNetwork.createTargets(ref RTGlobals.targets);



            RTGlobals.targets.Add(new Target());
            RTGlobals.targets[RTGlobals.targets.Count - 1].GUIname = RTGlobals.targets[RTGlobals.targets.Count - 1].Name;
            RTGlobals.targets[RTGlobals.targets.Count - 1].color = Color.red;


            settingNodes = new List<SatSettingNode>();

            if (node.IsLoaded)
                LoadFromLoaded();
            else
                LoadFromUnLoaded();
            this.show = true;
        }

        void LoadFromLoaded()
        {
            foreach (Part p in node.Vessel.parts)
                foreach (PartModule m in p.Modules)
                    if (RTUtils.containsField(m, "dishRange") && (float)m.Fields.GetValue("dishRange") > 0)
                        settingNodes.Add(new SatSettingNode(m));
            vesselName = node.Vessel.vesselName;
        }

        void LoadFromUnLoaded()
        {
            foreach (ProtoPartSnapshot p in node.Vessel.protoVessel.protoPartSnapshots)
            {
                foreach (ProtoPartModuleSnapshot s in p.modules)
                {
                    ConfigNode n = new ConfigNode();
                    s.Save(n);
                    if (n.HasValue("dishRange") && float.Parse(n.GetValue("dishRange")) > 0)
                        settingNodes.Add(new SatSettingNode(s, node.Vessel, p));
                }
            }
            vesselName = node.Vessel.protoVessel.vesselName;
        }

        public void Close()
        {
            this.show = false;
        }

        public void SaveAndClose()
        {

            this.Close();


            node.Vessel.protoVessel.vesselName = this.vesselName;
            node.Vessel.vesselName = this.vesselName;

            if (settingNodes.Count > 0 && this.node.IsLoaded == settingNodes[0].isLoaded)
            {
                foreach (SatSettingNode savestate in settingNodes)
                    savestate.save();

                RTGlobals.network = new RelayNetwork();
                core.path = RTGlobals.network.GetCommandPath(core.Rnode);
            }
        }


        public void SettingsGUI(int windowID)
        {
            try
            {
                this.vesselName = GUILayout.TextField(this.vesselName);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Save & Close", new GUIStyle(GUI.skin.button))) SaveAndClose();
                if (GUILayout.Button("Close without saving", new GUIStyle(GUI.skin.button))) Close();
                GUILayout.EndHorizontal();


                SettingListScroll = GUILayout.BeginScrollView(SettingListScroll, false, true);

                foreach (SatSettingNode setupnode in settingNodes)
                {
                    setupnode.SubmenuOpen = GUILayout.Toggle(setupnode.SubmenuOpen, setupnode.antennaName + "(" + RTUtils.length(setupnode.dishRange * 1000) + "m) Pointed At: " + setupnode.pointedAt.Name, new GUIStyle(GUI.skin.button));

                    if (setupnode.SubmenuOpen)
                        setupnode.ListTargets();

                }

                GUILayout.EndScrollView();
                GUI.DragWindow();
            }
            catch (NullReferenceException)
            { this.Close(); }
        }


    }
}
