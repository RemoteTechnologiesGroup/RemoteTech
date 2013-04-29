using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    
    //used to delay ctrl inputs by saving FlightCtrlStates between the time they are entered
    //by the user and the time they are applied to the craft
    public class FlightCtrlStateBuffer
    {
        Queue<TimedCtrlState> states = new Queue<TimedCtrlState>();
        public float pitch, roll, yaw;

        //save the flight control state entered at a given time
        public void push(FlightCtrlState state, double time)
        {
            TimedCtrlState savedCopy = new TimedCtrlState();
            savedCopy.flightCtrlState = new FlightCtrlState();
            savedCopy.flightCtrlState.CopyFrom(state);
            savedCopy.ActTime = time;
            states.Enqueue(savedCopy);

            // this saves the current attitude control values to a sepperate controlstate, that way attitude control is allways accesible without delay
            pitch = state.pitch;
            roll = state.roll;
            yaw = state.yaw;
        }
        
        public void setNeutral(FlightCtrlState s)
        {
            s.fastThrottle = 0;
            s.gearDown = false;
            s.gearUp = false;
            s.headlight = false;
            s.killRot = false;
            s.mainThrottle = 0;
            s.pitch = 0;
            s.pitchTrim = 0;
            s.roll = 0;
            s.rollTrim = 0;
            s.X = 0;
            s.Y = 0;
            s.yaw = 0;
            s.yawTrim = 0;
            s.Z = 0;
            s.wheelSteer = 0;
            s.wheelSteerTrim = 0;
            s.wheelThrottle = 0;
            s.wheelThrottleTrim = 0;
        }

        //retrieve the flight control state entered at a given time
        public void pop(FlightCtrlState controls, double time)
        {
            if (states.Count == 0 || states.Peek().ActTime > time)
            {
                setNeutral(controls);
                return; //returns neutral if no controls are entered or the entered controls are not within the signal delay
            }
            TimedCtrlState popState = states.Peek();
            while (states.Peek().ActTime < time)
            {
                popState = states.Dequeue();
            }


            controls.CopyFrom(popState.flightCtrlState);

            if (controls.killRot) // this overrides the delay in attitute control if SAS is activated. Thereby allowing SAS to make control changes without delay
            {
                controls.pitch = pitch;
                controls.roll = roll;
                controls.yaw = yaw;
            }

        }
    }
}
