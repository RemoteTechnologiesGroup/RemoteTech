using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    struct ThrottleState
    {
        public double ActTime;
        public float Throttle;
        public double Target;
        public bool Bt;
    }

    public struct RoverState
    {
        public bool Steer,
            reverse;
        public double
            ActTime,
            longitude,
            latitude;
        public float
            Speed,
            Steering,
            Target;
        public Quaternion roverRotation;
    }

    public struct AttitudeButtonState
    {
        public bool Active;
        public double ActTime;
        public float HDG;
        public float PIT;
        public float ROL;
        public bool USEroll;
        public Quaternion MN;
    }

    struct TimedCtrlState
    {
        public FlightCtrlState flightCtrlState;
        public double ActTime;
    }

    public struct TriggerState
    {
        public double ActTime;
        public KSPActionGroup ActionGroup;
    }


    public struct RTVesselState
    {
        public bool isPowered;
        public bool inRadioContact;
        public bool localControl;
        public double controlDelay;
    }

    public enum AttitudeReference
    {
        INERTIAL,          //world coordinate system.
        ORBIT,             //forward = prograde, left = normal plus, up = radial plus
        ORBIT_HORIZONTAL,  //forward = surface projection of orbit velocity, up = surface normal
        SURFACE_NORTH,     //forward = north, left = west, up = surface normal
        SURFACE_VELOCITY,  //forward = surface frame vessel velocity, up = perpendicular component of surface normal
    }

    public enum AttitudeMode
    {
        KILLROT,
        PROGRADE,
        RETROGRADE,
        SRF_PROGRADE,
        SRF_RETROGRADE,
        NORMAL_PLUS,
        NORMAL_MINUS,
        RADIAL_PLUS,
        RADIAL_MINUS,
        RELATIVE_PLUS,
        MANEUVERNODE,
        SURFACE
    }

    public struct DishData
    {
        public string pointedAt;
        public float dishRange;
    }




}
