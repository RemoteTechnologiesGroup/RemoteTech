using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{

    public static class RTUtils
    {
        public static TriggerState triggerstate
        {
            get
            {
                TriggerState state = new TriggerState();
                if (GameSettings.LAUNCH_STAGES.GetKeyDown())
                    state.ActionGroup = KSPActionGroup.Stage;
                else if (GameSettings.AbortActionGroup.GetKeyDown())
                    state.ActionGroup = KSPActionGroup.Abort;
                else if (GameSettings.RCS_TOGGLE.GetKeyDown())
                    state.ActionGroup = KSPActionGroup.RCS;
                else if (GameSettings.BRAKES.GetKeyDown())
                    state.ActionGroup = KSPActionGroup.Brakes;
                else if (GameSettings.LANDING_GEAR.GetKeyDown())
                    state.ActionGroup = KSPActionGroup.Gear;
                else if (GameSettings.HEADLIGHT_TOGGLE.GetKeyDown())
                    state.ActionGroup = KSPActionGroup.Light;
                else if (GameSettings.CustomActionGroup1.GetKeyDown())
                    state.ActionGroup = KSPActionGroup.Custom01;
                else if (GameSettings.CustomActionGroup2.GetKeyDown())
                    state.ActionGroup = KSPActionGroup.Custom02;
                else if (GameSettings.CustomActionGroup3.GetKeyDown())
                    state.ActionGroup = KSPActionGroup.Custom03;
                else if (GameSettings.CustomActionGroup4.GetKeyDown())
                    state.ActionGroup = KSPActionGroup.Custom04;
                else if (GameSettings.CustomActionGroup5.GetKeyDown())
                    state.ActionGroup = KSPActionGroup.Custom05;
                else if (GameSettings.CustomActionGroup6.GetKeyDown())
                    state.ActionGroup = KSPActionGroup.Custom06;
                else if (GameSettings.CustomActionGroup7.GetKeyDown())
                    state.ActionGroup = KSPActionGroup.Custom07;
                else if (GameSettings.CustomActionGroup8.GetKeyDown())
                    state.ActionGroup = KSPActionGroup.Custom08;
                else if (GameSettings.CustomActionGroup9.GetKeyDown())
                    state.ActionGroup = KSPActionGroup.Custom09;
                else if (GameSettings.CustomActionGroup10.GetKeyDown())
                    state.ActionGroup = KSPActionGroup.Custom10;
                else
                    state.ActionGroup = KSPActionGroup.None;

                return state;
            }
        }


        public static bool containsField(PartModule module, string fieldName)
        {
            try
            {
                return module.Fields.GetValue(fieldName) != null;
            }
            catch { return false; }
        }


        public static void applyLocks()
        {
            if (!InputLockManager.IsLocked(ControlTypes.STAGING))
                InputLockManager.SetControlLock(ControlTypes.STAGING, "LockStaging");
            if (!InputLockManager.IsLocked(ControlTypes.SAS))
                InputLockManager.SetControlLock(ControlTypes.SAS, "LockSAS");
            if (!InputLockManager.IsLocked(ControlTypes.GROUPS_ALL))
                InputLockManager.SetControlLock(ControlTypes.GROUPS_ALL, "LockActions");
        }

        public static void removeLocks()
        {
            if (InputLockManager.IsLocked(ControlTypes.STAGING))
                InputLockManager.RemoveControlLock("LockStaging");

            if (InputLockManager.IsLocked(ControlTypes.SAS))
                InputLockManager.RemoveControlLock("LockSAS");

            if (InputLockManager.IsLocked(ControlTypes.GROUPS_ALL))
                InputLockManager.RemoveControlLock("LockActions");
        }

        public static string FormatNumString(String inputString, bool allowNegative)
        {
            Char[] Input = inputString.ToCharArray();
            String num = "0123456789";
            String point = ".,";
            String Output = (inputString.StartsWith("-") && allowNegative) ? "-" : "";
            bool hasnNum = false;
            bool hasPoint = false;

            foreach (Char c in Input)
            {
                if (num.Contains(c))
                {
                    Output += c;
                    if (!hasnNum)
                        hasnNum = true;
                }
                else if (point.Contains(c) && !hasPoint)
                {
                    if (!hasnNum)
                    {
                        Output += '0';
                        hasnNum = true;
                    }

                    Output += '.';
                    hasPoint = true;
                }
            }
            return Output;
        }

        public static string FormatNumString(String inputString)
        {
            return FormatNumString(inputString, true);
        }


        public static string TFormat(String inputString)
        {
            string s = "";

            foreach (char a in inputString)
            {
                if ("0123456789".ToCharArray().Contains(a)) s += a;
            }

            return s;
        }

        public static float ForwardSpeed(Vessel v)
        {
            return  Vector3.Dot(v.ReferenceTransform.up, v.obt_velocity);
        }

        public static Vector3d Invert(Vector3d vector)
        {
            return new Vector3d(1 / vector.x, 1 / vector.y, 1 / vector.z);
        }

        public static Vector3d Sign(Vector3d vector)
        {
            return new Vector3d(Math.Sign(vector.x), Math.Sign(vector.y), Math.Sign(vector.z));
        }

        public static Vector3d Reorder(Vector3d vector, int order)
        {
            switch (order)
            {
                case 123:
                    return new Vector3d(vector.x, vector.y, vector.z);
                case 132:
                    return new Vector3d(vector.x, vector.z, vector.y);
                case 213:
                    return new Vector3d(vector.y, vector.x, vector.z);
                case 231:
                    return new Vector3d(vector.y, vector.z, vector.x);
                case 312:
                    return new Vector3d(vector.z, vector.x, vector.y);
                case 321:
                    return new Vector3d(vector.z, vector.y, vector.x);
            }
            throw new ArgumentException("Invalid order", "order");
        }


        public static Vector3d averageVector3d(Vector3d[] vectorArray, Vector3d newVector)
        {
            double x = 0.0, y = 0.0, z = 0.0;
            int n = vectorArray.Length;
            int k = 0;

            // Loop through the array to determine average
            // Give more weight to newer items and less weight to older items
            for (int i = 0; i < n; i++)
            {
                k += i + 1;
                if (i < n - 1) { vectorArray[i] = vectorArray[i + 1]; }
                else { vectorArray[i] = newVector; }
                x += vectorArray[i].x * (i + 1);
                y += vectorArray[i].y * (i + 1);
                z += vectorArray[i].z * (i + 1);
            }
            return new Vector3d(x / k, y / k, z / k);
        }

        public static int restForPeriod(int[] restArray, double vector, int resting)
        {
            int n = restArray.Length - 1; //Last elemet in the array, useful in loops and inseting element to the end
            int insertPos = -1; //Position to insert a vector into
            int vectorSign = Mathf.Clamp(Mathf.RoundToInt((float)vector), -1, 1); //Is our vector a negative, 0 or positive
            float threshold = 0.95F; //Vector must be above this to class as hitting the upper limit
            bool aboveThreshold = Mathf.Abs((float)vector) > threshold; //Determines if the input vector is above the threshold

            // Decrease our resting count so we don't rest forever
            if (resting > 0) resting--;

            // Move all values in restArray towards 0 by 1, effectly taking 1 frame off the count
            // Negative values indicate the vector was at the minimum threshold
            for (int i = n; i >= 0; i--) restArray[i] = restArray[i] - Mathf.Clamp(restArray[i], -1, 1);

            // See if the oldest value has reached 0, if it has move all values 1 to the left
            if (restArray[0] == 0 && restArray[1] != 0)
            {
                for (int i = 0; i < n - 1; i++) restArray[i] = restArray[i + 1]; //shuffle everything down to the left
                restArray[n] = 0; //item n has just been shifted to the left, so reset value to 0
            }

            // Find our position to insert the vector sign, insertPos will be -1 if no empty position is found
            for (int i = n; i >= 0; i--)
            {
                if (restArray[i] == 0) insertPos = i;
            }

            // If we found a valid insert position, and the sign is different to the last sign, then insert it
            if (
                aboveThreshold && ( // First make sure we are above the threshold

                    // If in position 0, the sign is always different
                    (insertPos == 0) ||

                    // If in position 1 to n-1, then make sure the previous sign is different
                // We don't want to rest if the axis is simply at the max for a while such as it may be for hard turns
                    (insertPos > 0 && vectorSign != Mathf.Clamp(restArray[insertPos - 1], -1, 1))
                )
               ) // end if aboveThreshold
            {
                // Insert restFrameTolerance and the vectors sign
                restArray[insertPos] = 90 * vectorSign;
            }

            // Determine if the array is full, we are above the threshold, and the sign is different to the last
            if (aboveThreshold && insertPos == -1 && vectorSign != Mathf.Clamp(restArray[n], -1, 1))
            {
                // Array is full, remove oldest value to make room for new value
                for (int i = 0; i < n - 1; i++)
                {
                    restArray[i] = restArray[i + 1];
                }
                // Insert new value
                restArray[n] = 90 * vectorSign;

                // Sets the axis to rest for the length of time 3/4 of the frame difference between the first item and last item
                resting = (int)Math.Ceiling(((Math.Abs(restArray[n]) - Math.Abs(restArray[0])) / 4.0) * 3.0);
            }

            // Returns number of frames to rest for, or 0 for no rest
            return resting;
        }

        public static string length(double m)
        {
            if (m >= 1000000000000000000)
                return roundS(m / 1000000000000000000) + " E";

            if (m >= 1000000000000000)
                return roundS(m / 1000000000000000) + " P";

            if (m >= 1000000000000)
                return roundS(m / 1000000000000) + " T";

            if (m >= 1000000000)
                return roundS(m / 1000000000) + " G";

            if (m >= 1000000)
                return roundS(m / 1000000) + " M";
            
            if (m >= 1000)
                return roundS(m / 1000) + " k";
            

            return roundS(m)+" ";
        }


        public static string roundS(double t)
        {
            return t.ToString("0.00");
        }


        public static string time(double t)
        {
            if (t < 10)
                return roundS(t)+"s";   

            int seconds = (int)Math.Round(t, 0);

            int days = 0, hours = 0, minutes = 0;

            if (seconds > 60)
                minutes = Math.DivRem(seconds, 60, out seconds);
            if (minutes > 60)
                hours = Math.DivRem(minutes, 60, out minutes);
            if (hours > 24)
                days = Math.DivRem(hours, 24, out hours);

            if(days > 0)
                return days + "d " + hours + "h " + minutes + "m " + seconds+"s";
            if (hours > 0)
                return hours + "h " + minutes + "m " + seconds + "s";
            if (minutes > 0)
                return minutes + "m " + seconds + "s";

            return seconds + "s";
        }


        public static void findTransformsWithPrefix(Transform input, ref List<Transform> list, string prefix)
        {
            if (input.name.ToLower().StartsWith(prefix.ToLower()))
                list.Add(input);
            foreach (Transform t in input)
                findTransformsWithPrefix(t, ref list, prefix);
        }


        public static Color PlanetColor(String s)
        {
            if (s.Equals("Sun"))
                return Color.yellow;

            if (s.Equals("Moho"))
                return new Color(144 / 255f, 71 / 255f, 37 / 255f,1f);
            //return new Color(82 / 255f, 40 / 255f, 21 / 255f);

            if (s.Equals("Eve"))
                return new Color(136 / 255f, 43 / 255f, 248 / 255f);
            if (s.Equals("Gilly"))
                return new Color(104 / 255f, 82 / 255f, 74 / 255f);

            if (s.Equals("Kerbin"))
                return new Color(159 / 255f, 232 / 255f, 225 / 255f);
            if (s.Equals("Mun"))
                return new Color(169 / 255f, 176 / 255f, 198 / 255f);
            if (s.Equals("Minmus"))
                return new Color(91 / 255f, 76 / 255f, 106 / 255f);

            if (s.Equals("Duna"))
                return new Color(207 / 255f, 82 / 255f, 55 / 255f);
            if (s.Equals("Ike"))
                return new Color(169 / 255f, 177 / 255f, 199 / 255f);
            
            if (s.Equals("Dres"))
                return new Color(162 / 255f, 160 / 255f, 156 / 255f);
            
            if (s.Equals("Jool"))
                return new Color(84 / 255f, 221 / 255f, 37 / 255f);
            if (s.Equals("Laythe"))
                return new Color(79 / 255f, 101 / 255f, 174 / 255f);
            //return new Color(45 / 255f, 57 / 255f, 103 / 255f);
            if (s.Equals("Vall"))
                return new Color(139 / 255f, 197 / 255f, 230 / 255f);
            if (s.Equals("Tylo"))
                return new Color(246 / 255f, 216 / 255f, 218 / 255f);
            if (s.Equals("Bop"))
                return new Color(119 / 255f, 104 / 255f, 83 / 255f);
            if (s.Equals("Pol"))
                return new Color(150 / 255f, 152 / 255f, 121 / 255f);

            return Color.white;
        }


        public static string targetName(string ident)
        {
            if (ident.Equals("Mission Control"))
                return ident;

            foreach (CelestialBody bodies in FlightGlobals.Bodies)
                if (bodies.name.ToLower().Equals(ident.ToLower()))
                    return bodies.theName;

            foreach (Vessel v in FlightGlobals.Vessels)
                if (v.id.ToString().Equals(ident))
                    return v.vesselName;

            return "None";
        }

        public static bool IsComsat(Vessel v)
        {
            if (v.loaded)
            {
                foreach (Part p in v.parts)
                    foreach (PartModule m in p.Modules)
                        if (RTUtils.containsField(m, "isRemoteCommand"))
                            return true;
            }
            else
            {
                foreach (ProtoPartSnapshot p in v.protoVessel.protoPartSnapshots)
                {
                    foreach (ProtoPartModuleSnapshot s in p.modules)
                    {
                        ConfigNode n = new ConfigNode();
                        s.Save(n);
                        if (n.HasValue("isRemoteCommand"))
                            return true;
                    }
                }
            }
            return false;

        }



    }
}
