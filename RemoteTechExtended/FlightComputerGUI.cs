using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RemoteTech
{
    public class roverControl
    {
        FlightComputerGUI computer;
        RoverState state = new RoverState();
        public Queue<RoverState> states = new Queue<RoverState>();
        bool reverse = false;
        float
            Speed = 0,
            SteeringBar = 0,
            Distance = 0,
            Degrees = 0;
        double lastActTime = -10;
        string DegS = "0", DistS = "0", SpeedS = "0";

        public roverControl(FlightComputerGUI computerin)
        {
            this.computer = computerin;
        }

        public bool sending
        {
            get
            {
                return states.Count > 0;
            }
        }

        public void update()
        {
            if (sending && states.Peek().ActTime <= Planetarium.GetUniversalTime())
            {
                state = states.Dequeue();
                state.longitude = computer.core.vessel.longitude;
                state.latitude = computer.core.vessel.latitude;
                state.roverRotation = computer.core.vessel.ReferenceTransform.rotation;

                computer.core.computer.setRover(state);
            }
        }


        public void ShutDown()
        {
            states.Clear();
        }

        public void draw()
        {
            GUILayout.Label((SteeringBar >= 0 ? "Right: " : "Left: ") + Math.Abs(Mathf.RoundToInt(SteeringBar * 100)) + "%", GUI.skin.textField);
            SteeringBar = GUILayout.HorizontalSlider(SteeringBar, -1, 1);

            reverse = GUILayout.Toggle(reverse, reverse ? "Reverse" : "Forward");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Speed (m/s):", GUI.skin.textField, GUILayout.Width(100));
            SpeedS = GUILayout.TextField(SpeedS, GUILayout.Width(50));
            SpeedS = RTUtils.FormatNumString(SpeedS);
            if (SpeedS == "")
                SpeedS = "0";

            if (GUILayout.Button("+", GUI.skin.textField, GUILayout.Width(21.0F)))
            {
                float tmp = Convert.ToSingle(SpeedS);
                tmp += 1;
                SpeedS = Mathf.RoundToInt(tmp).ToString();
            }
            if (GUILayout.Button("-", GUI.skin.textField, GUILayout.Width(21.0F)))
            {
                float tmp = Convert.ToSingle(SpeedS);
                tmp -= 1;
                SpeedS = Mathf.RoundToInt(tmp).ToString();
            }

            Speed = Mathf.Clamp(Convert.ToSingle(SpeedS), 0, float.MaxValue);
            SpeedS = Speed.ToString();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Drive (m):", GUI.skin.textField, GUILayout.Width(100));
            DistS = GUILayout.TextField(DistS, GUILayout.Width(50));
            DistS = RTUtils.FormatNumString(DistS);
            if (DistS == "")
                DistS = "0";

            if (GUILayout.Button("+", GUI.skin.textField, GUILayout.Width(21.0F)))
            {
                float tmp = Convert.ToSingle(DistS);
                tmp += 1;
                DistS = Mathf.RoundToInt(tmp).ToString();
            }
            if (GUILayout.Button("-", GUI.skin.textField, GUILayout.Width(21.0F)))
            {
                float tmp = Convert.ToSingle(DistS);
                tmp -= 1;
                DistS = Mathf.RoundToInt(tmp).ToString();
            }

            Distance = Mathf.Clamp(Convert.ToSingle(DistS), 0, float.MaxValue);
            DistS = Distance.ToString();

            if (GUILayout.Button("Send", GUI.skin.textField))
            {
                RoverState r = new RoverState();
                r.Steer = false;
                r.Target = Distance;
                r.Speed = Speed;
                r.reverse = reverse;
                r.Steering = 0;
                lastActTime = r.ActTime = Planetarium.GetUniversalTime() + (computer.core.localControl ? 0 : computer.core.path.ControlDelay);
                states.Enqueue(r);
            }

            GUILayout.EndHorizontal();



            GUILayout.BeginHorizontal();

            GUILayout.Label("Turn (°):", GUI.skin.textField, GUILayout.Width(100));
            DegS = GUILayout.TextField(DegS, GUILayout.Width(50));
            DegS = RTUtils.FormatNumString(DegS);
            if (DegS == "")
                DegS = "0";

            if (GUILayout.Button("+", GUI.skin.textField, GUILayout.Width(21.0F)))
            {
                float tmp = Convert.ToSingle(DegS);
                tmp += 1;
                DegS = Mathf.RoundToInt(tmp).ToString();
            }
            if (GUILayout.Button("-", GUI.skin.textField, GUILayout.Width(21.0F)))
            {
                float tmp = Convert.ToSingle(DegS);
                tmp -= 1;
                DegS = Mathf.RoundToInt(tmp).ToString();
            }

            Degrees = Mathf.Clamp(Convert.ToSingle(DegS), 0, 90);
            DegS = Degrees.ToString();

            if (GUILayout.Button("Send", GUI.skin.textField))
            {
                RoverState r = new RoverState();
                r.Steer = true;
                r.Target = Degrees;
                r.Speed = Speed;
                r.reverse = reverse;
                r.Steering = Mathf.RoundToInt(SteeringBar * 100) == 0 ? 0 : -SteeringBar;
                lastActTime = r.ActTime = Planetarium.GetUniversalTime() + (computer.core.localControl ? 0 : computer.core.path.ControlDelay);
                states.Enqueue(r);
            }

            GUILayout.EndHorizontal();



            GUILayout.BeginHorizontal();
            GUILayout.Label(sending ? "Sending " + computer.arrows :
                (computer.core.computer.roverActive ? DriveDescript : "")
            , GUI.skin.textField);
            GUILayout.Label(sending ? RTUtils.time(lastActTime - Planetarium.GetUniversalTime()) : (computer.core.computer.roverActive ? TargetDescript : "")
                , GUI.skin.textField, GUILayout.Width(100));
            GUILayout.EndHorizontal();

        }

        string DriveDescript
        {
            get
            {
                if (computer.core.computer.roverState.Steer)
                    return "Turning " + Quaternion.Angle(computer.core.computer.roverState.roverRotation, computer.core.vessel.ReferenceTransform.rotation).ToString("0.00") + "° of";
                else
                    return "Driving " + RTUtils.length(Vector3d.Distance(computer.core.vessel.mainBody.position + computer.core.computer.altitude * computer.core.vessel.mainBody.GetSurfaceNVector(computer.core.computer.roverState.latitude, computer.core.computer.roverState.longitude), computer.core.vessel.transform.position)) + "m of";
            }
        }
        string TargetDescript
        {
            get
            {
                if (computer.core.computer.roverState.Steer)
                    return computer.core.computer.roverState.Target.ToString("0.00") + "°";
                else
                    return RTUtils.length(computer.core.computer.roverState.Target) + "m";
            }
        }

    }



    public class AttitudeStateButton
    {
        public bool on = false;
        public bool lastOn = false;

        FlightComputerGUI computer;
        public AttitudeMode mode;
        public AttitudeButtonState state = new AttitudeButtonState();
        Queue<AttitudeButtonState> states = new Queue<AttitudeButtonState>();
        string name;
        float HDG = 0;
        float PIT = 0;
        float ROL = 0;
        bool USEroll = false;
        string HDGs, PITs, ROLs;
        double lastActTime = -10;

        public AttitudeStateButton(FlightComputerGUI computerin, AttitudeMode modein, string namein)
        {
            this.computer = computerin;
            this.mode = modein;
            this.name = namein;
            if (mode == AttitudeMode.SURFACE)
            {
                HDG = PIT = 90;
                HDGs = PITs = HDG.ToString();
                ROL = 0;
                ROLs = ROL.ToString();
            }
        }

        public bool sending
        {
            get
            {
                return states.Count > 0;
            }
        }

        public void Draw()
        {
            bool locked = on;
            GUILayout.BeginHorizontal();

            Color savedContentColor = GUI.contentColor;

            if (state.Active)
            {
                if (mode == AttitudeMode.MANEUVERNODE)
                {
                    if (state.Active)
                    {
                        if (FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count > 0)
                            GUI.contentColor = Color.green;
                        else
                            GUI.contentColor = Color.yellow;
                    }
                }
                else
                    if (state.Active) GUI.contentColor = Color.green;
            }
            else
                if (mode == AttitudeMode.MANEUVERNODE && FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count == 0)
                    GUI.contentColor = Color.red;

            on = GUILayout.Toggle(on, 
                (RTGlobals.ColFriend && state.Active ? name+"<" : name)
                , GUI.skin.textField, GUILayout.Width(100));
            GUI.contentColor = savedContentColor;

            if (!(computer.core.localControl || computer.core.InContact) || (mode == AttitudeMode.MANEUVERNODE && FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count == 0 && !state.Active))
                on = locked;

            GUILayout.Label(sending ? computer.arrows : "", GUI.skin.textField, GUILayout.Width(50));
            GUILayout.Label(sending ? RTUtils.time((lastActTime - Planetarium.GetUniversalTime() > 0) ? lastActTime - Planetarium.GetUniversalTime() : 0) : "", GUI.skin.textField, GUILayout.Width(90));
            GUILayout.EndHorizontal();

            if (mode != AttitudeMode.SURFACE || !on) return;
            GUILayout.BeginHorizontal();

            GUILayout.Label("Pitch:", GUI.skin.textField, GUILayout.Width(100));
            PITs = GUILayout.TextField(PITs, GUILayout.Width(50));
            PITs = RTUtils.FormatNumString(PITs);

            if (GUILayout.Button("+", GUI.skin.textField, GUILayout.Width(21.0F)))
            {
                float tmp = Convert.ToSingle(PITs);
                tmp += 1;
                if (tmp >= 360.0F)
                {
                    tmp -= 360.0F;
                }
                PITs = Mathf.RoundToInt(tmp).ToString();
            }
            if (GUILayout.Button("-", GUI.skin.textField, GUILayout.Width(21.0F)))
            {
                float tmp = Convert.ToSingle(PITs);
                tmp -= 1;
                if (tmp < 0)
                {
                    tmp += 360.0F;
                }
                PITs = Mathf.RoundToInt(tmp).ToString();
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            GUILayout.Label("Heading:", GUI.skin.textField, GUILayout.Width(100));
            HDGs = GUILayout.TextField(HDGs, GUILayout.Width(50));
            HDGs = RTUtils.FormatNumString(HDGs);

            if (GUILayout.Button("+", GUI.skin.textField, GUILayout.Width(21.0F)))
            {
                float tmp = Convert.ToSingle(HDGs);
                tmp += 1;
                if (tmp >= 360.0F)
                {
                    tmp -= 360.0F;
                }
                HDGs = Mathf.RoundToInt(tmp).ToString();
            }
            if (GUILayout.Button("-", GUI.skin.textField, GUILayout.Width(21.0F)))
            {
                float tmp = Convert.ToSingle(HDGs);
                tmp -= 1;
                if (tmp < 0)
                {
                    tmp += 360.0F;
                }
                HDGs = Mathf.RoundToInt(tmp).ToString();
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            GUILayout.Label("Roll:", GUI.skin.textField, GUILayout.Width(100));
            ROLs = GUILayout.TextField(ROLs, GUILayout.Width(50));
            ROLs = RTUtils.FormatNumString(ROLs);

            if (GUILayout.Button("+", GUI.skin.textField, GUILayout.Width(21.0F)))
            {
                float tmp = Convert.ToSingle(ROLs);
                tmp += 1;
                if (tmp >= 360.0F)
                {
                    tmp -= 360.0F;
                }
                ROLs = Mathf.RoundToInt(tmp).ToString();
            }
            if (GUILayout.Button("-", GUI.skin.textField, GUILayout.Width(21.0F)))
            {
                float tmp = Convert.ToSingle(ROLs);
                tmp -= 1;
                if (tmp < 0)
                {
                    tmp += 360.0F;
                }
                ROLs = Mathf.RoundToInt(tmp).ToString();
            }
            USEroll = GUILayout.Toggle(USEroll, " ", GUI.skin.toggle, GUILayout.Width(21.0F));

            GUILayout.EndHorizontal();
            if (GUILayout.Button("Update", GUI.skin.textField) && (computer.core.localControl || computer.core.InContact))
            {
                if (PITs.EndsWith("."))
                    PITs = PITs.Substring(0, PITs.Length - 1);

                if (HDGs.EndsWith("."))
                    HDGs = HDGs.Substring(0, HDGs.Length - 1);
                if (ROLs.EndsWith("."))
                    ROLs = ROLs.Substring(0, ROLs.Length - 1);

                PIT = Convert.ToSingle(PITs);
                HDG = Convert.ToSingle(HDGs);
                ROL = Convert.ToSingle(ROLs);
                lastOn = false;
            }

        }


        public void ShutDown()
        {
            on = lastOn = state.Active = false;
            states.Clear();
        }


        public void Update()
        {
            if (on != lastOn && (computer.core.localControl || computer.core.InContact))
            {
                AttitudeButtonState tmp = new AttitudeButtonState();
                tmp.Active = lastOn = on;
                tmp.ActTime = lastActTime = Planetarium.GetUniversalTime() + (computer.core.localControl ? 0 : computer.core.path.ControlDelay);
                tmp.HDG = this.HDG;
                tmp.PIT = this.PIT;
                tmp.ROL = this.ROL;
                tmp.USEroll = this.USEroll;

                if (this.mode == AttitudeMode.MANEUVERNODE)
                {
                    if (FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count > 0)
                    {
                        tmp.MN = Quaternion.LookRotation(FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(FlightGlobals.ActiveVessel.orbit).normalized, -FlightGlobals.ActiveVessel.ReferenceTransform.forward);
                        states.Enqueue(tmp);
                    }
                    else
                    {
                        if (state.Active)
                        {
                            tmp.MN = Quaternion.identity;
                            tmp.Active = false;
                            states.Enqueue(tmp);
                        }
                    }
                }
                else
                {
                    tmp.MN = Quaternion.identity;
                    states.Enqueue(tmp);
                }

            }

            if (sending && states.Peek().ActTime <= Planetarium.GetUniversalTime())
            {
                state = states.Dequeue();
                foreach (AttitudeStateButton b in computer.attitudeButtons)
                {
                    if (b != this)
                    {
                        b.on = b.lastOn = b.state.Active = false;
                    }
                }
                computer.core.computer.SetMode(mode, state);
            }

        }

    }


    public class SimpleThrottle
    {
        FlightComputerGUI computer;
        ThrottleState state = new ThrottleState();
        Queue<ThrottleState> states = new Queue<ThrottleState>();
        double lastActTime;
        float ThrottleBar = 0;
        string BTS = "";
        string burnAt = "";
        float burnAtF = 0;
        double speedT0 = 0;

        public SimpleThrottle(FlightComputerGUI computerin)
        {
            this.computer = computerin;
            state.Target = -10;
        }

        bool doOnce = false;
        float ThrottleIncrement = 0;

        public void update()
        {
            if (sending && states.Peek().ActTime <= Planetarium.GetUniversalTime())
            {
                state = states.Dequeue();
                if (state.Bt)
                    state.Target = state.Target + Planetarium.GetUniversalTime();
                else
                {
                    speedT0 = RTUtils.ForwardSpeed(computer.core.vessel);
                }
            }

            if (burning)
            {
                doOnce = true;
                if (ThrottleIncrement < state.Throttle)
                {
                    ThrottleIncrement = Mathf.Clamp(ThrottleIncrement + 0.1F, 0, 1);
                    computer.core.computer.SetThrottle(ThrottleIncrement);
                }
                else
                    computer.core.computer.SetThrottle(Mathf.Clamp(state.Throttle, 0, 1));
            }
            else
                if (doOnce)
                {
                    ThrottleIncrement = 0;
                    computer.core.computer.SetThrottle(ThrottleIncrement);
                    doOnce = false;
                    if (!state.Bt)
                    {
                        state.Target = -10;
                        state.Bt = true;
                    }
                }
        }

        public void ShutDown()
        {
            ThrottleIncrement = 0;
            computer.core.computer.SetThrottle(ThrottleIncrement);
            if (!state.Bt)
            {
                state.Target = -10;
                state.Bt = true;
            }

            states.Clear();
        }


        public bool sending
        {
            get
            {
                return states.Count > 0;
            }
        }

        public bool burning
        {
            get
            {
                if (state.Bt)
                    return state.Target >= Planetarium.GetUniversalTime();
                else
                    return Math.Abs(speedT0 - RTUtils.ForwardSpeed(computer.core.vessel)) < state.Target;
            }
        }

        bool BT = true;
        public void draw()
        {
            GUILayout.Label("Throttle: " + Mathf.RoundToInt(ThrottleBar * 100) + "%", GUI.skin.textField);
            ThrottleBar = GUILayout.HorizontalSlider(ThrottleBar, 0, 1);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(BT ? "Burn time (s)" : "ΔV (m/s)", GUI.skin.textField, GUILayout.Width(100)))
                BT = !BT;
            BTS = GUILayout.TextField(BTS, GUILayout.Width(50));
            BTS = RTUtils.FormatNumString(BTS, false);
            if (GUILayout.Button("Send", GUI.skin.textField) && (computer.core.localControl || computer.core.InContact))
            {
                ThrottleState tmp = new ThrottleState();
                tmp.Throttle = ThrottleBar;
                if (BTS.EndsWith("."))
                    BTS = BTS.Substring(0, BTS.Length - 1);
                tmp.Target = Convert.ToSingle(BTS);
                tmp.Bt = BT;
                lastActTime = tmp.ActTime = Planetarium.GetUniversalTime() + (computer.core.localControl ? (double)burnAtF :
                    (computer.core.path.ControlDelay <= (double)burnAtF ? (double)burnAtF : computer.core.path.ControlDelay)
                    );
                states.Enqueue(tmp);
                BTS = "";
                burnAt = "";
                burnAtF = 0;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            GUILayout.Label(sending ? "Sending " + computer.arrows : (burning && state.Bt ? "Burning" : (BTS == "" ? "" : "in HH:MM:SS")), GUI.skin.textField, GUILayout.Width(100));

            if (BTS == "")
            {
                GUILayout.Label(sending ? RTUtils.time((lastActTime - Planetarium.GetUniversalTime() > 0) ? lastActTime - Planetarium.GetUniversalTime() : 0) :
                    (burning && state.Bt ? RTUtils.time((state.Target - Planetarium.GetUniversalTime() > 0) ? state.Target - Planetarium.GetUniversalTime() : 0) : "")
                , GUI.skin.textField, GUILayout.Width(100));
            }
            else
            {
                burnAt = GUILayout.TextField(burnAt, GUILayout.Width(100));
                List<String> temp = burnAt.Split(":".ToCharArray()).ToList();
                string seconds = "", minutes = "", hours = "";

                while (temp.Count < 3) temp.Insert(0, "");

                seconds = RTUtils.TFormat(temp[2]);
                burnAtF = Convert.ToSingle(seconds == "" ? "0" : seconds);
                burnAt = seconds;


                if (temp[1] != "")
                {
                    minutes = RTUtils.TFormat(temp[1]);
                    burnAtF += Convert.ToSingle(minutes == "" ? "0" : minutes) * 60;
                    burnAt = minutes == "" ? burnAt : (minutes + ":" + burnAt);
                }

                if (temp[0] != "")
                {
                    hours = RTUtils.TFormat(temp[0]);
                    burnAtF += Convert.ToSingle(hours == "" ? "0" : hours) * 3600;
                    burnAt = hours == "" ? burnAt : (hours + ":" + burnAt);
                }
            }


            GUILayout.EndHorizontal();


        }




    }


    public class FlightComputerGUI
    {
        public RemoteCore core;
        public int ATTITUDE_ID = 72138;
        public int THROTTLE_ID = 72238;
        public SimpleThrottle throttle;
        public roverControl rover;
        public List<AttitudeStateButton> attitudeButtons;

        public FlightComputerGUI(RemoteCore corein)
        {
            this.core = corein;
            throttle = new SimpleThrottle(this);
            rover = new roverControl(this);

            attitudeButtons = new List<AttitudeStateButton>();
            attitudeButtons.Add(new AttitudeStateButton(this, AttitudeMode.KILLROT, "KillRot"));
            attitudeButtons.Add(new AttitudeStateButton(this, AttitudeMode.PROGRADE, "Prograde"));
            attitudeButtons.Add(new AttitudeStateButton(this, AttitudeMode.RETROGRADE, "Retrograde"));
            attitudeButtons.Add(new AttitudeStateButton(this, AttitudeMode.NORMAL_PLUS, "NML +"));
            attitudeButtons.Add(new AttitudeStateButton(this, AttitudeMode.NORMAL_MINUS, "NML -"));
            attitudeButtons.Add(new AttitudeStateButton(this, AttitudeMode.RADIAL_PLUS, "RAD +"));
            attitudeButtons.Add(new AttitudeStateButton(this, AttitudeMode.RADIAL_MINUS, "RAD -"));
            attitudeButtons.Add(new AttitudeStateButton(this, AttitudeMode.MANEUVERNODE, "Maneuver"));
            attitudeButtons.Add(new AttitudeStateButton(this, AttitudeMode.SURFACE, "Surface"));
        }

        private bool _RM = false;
        public bool roverMode
        {
            get
            {
                return core.Wheel && _RM;
            }
            set
            {
                _RM = value;
            }
        }

        double t = 0;
        public string arrows = "";
        public void update()
        {
            if (t <= Math.Round(Planetarium.GetUniversalTime(), 0))
            {
                t = Math.Round(Planetarium.GetUniversalTime(), 0) + 1;

                arrows += "»";
                if (arrows.Length > 4)
                    arrows = "";
            }

            throttle.update();

            if (roverMode)
                rover.update();

            foreach (AttitudeStateButton b in attitudeButtons)
                b.Update();
        }


        public void ShutDown()
        {
            foreach (AttitudeStateButton b in attitudeButtons)
                b.ShutDown();
            rover.ShutDown();
            throttle.ShutDown();
            core.computer.ShutDown();
        }

        public void ThrottleGUI(int windowID)
        {
            throttle.draw();

            GUI.DragWindow();
        }

        public void AttitudeGUI(int windowID)
        {
            if (core.Wheel
                &&
                GUILayout.Button(_RM ? "Rover" : "Attitude", GUI.skin.textField)
                )
            {
                _RM = !_RM;

                if (_RM)
                {
                    RoverState r = new RoverState();
                    r.Steer = false;
                    r.Target = -0;
                    r.Steering = 0;
                    r.ActTime = Planetarium.GetUniversalTime() + (core.localControl ? 0 : core.path.ControlDelay);
                    rover.states.Enqueue(r);
                }
            }


            if (roverMode)
            {
                attitudeButtons[0].Draw();
                rover.draw();
            }
            else
                foreach (AttitudeStateButton b in attitudeButtons)
                {
                    b.Draw();
                }
            GUI.DragWindow();
        }


    }
}
