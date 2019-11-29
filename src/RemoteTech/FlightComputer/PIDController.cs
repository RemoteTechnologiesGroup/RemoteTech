using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Easy way to test the effectiveness of PID Controller is to launch a tiny rocket with a mammoth engine at the
/// bottom with FC's GRD+ command & infinite propellent cheat set. KSP's autopilot passes this test with flying colors.
/// </summary>

namespace RemoteTech.FlightComputer
{
    //Based on https://github.com/lamont-granquist/MechJim/blob/577002bccd3558d53efcee873d4bb982540c9cdf/Source/Manager/SteeringManager.cs
    //and https://github.com/KSP-KOS/KOS/blob/master/src/kOS/Control/SteeringManager.cs
    public class PIDController
    {
        public const double EPSILON = 1e-16;

        /* error */
        private double phiTotal;
        /* error in pitch, roll, yaw */
        private Vector3d phiVector = Vector3d.zero;
        private Vector3d TargetOmega = Vector3d.zero;
        /* max angular rotation */
        private Vector3d MaxOmega = Vector3d.zero;

        private Vector3d Actuation = Vector3d.zero;
        private Vector3d TargetTorque = Vector3d.zero;
        private Vector3d Omega = Vector3d.zero;

        public PIDLoop pitchRatePI;
        public PIDLoop yawRatePI;
        public PIDLoop rollRatePI;

        public TorquePI pitchPI;
        public TorquePI yawPI;
        public TorquePI rollPI;

        public Vessel Vessel;
        public Quaternion Target;

        /* temporary state vectors */
        public Quaternion VesselRotation;
        private Vector3d vesselForward;
        private Vector3d vesselTop;
        private Vector3d vesselStarboard;
        private Vector3d targetForward;
        private Vector3d targetTop;
        //private Vector3d targetStarboard;

        public Vector3d getDeviationErrors()
        {
            return phiVector;
        }

        private double rollControlRange;
        public double RollControlRange
        {
            get { return this.rollControlRange; }
            set { this.rollControlRange = Math.Max(EPSILON, Math.Min(Math.PI, value)); }
        }

        public PIDController(double kp, double ki, double kd, double maxoutput = double.MaxValue, double minoutput = double.MinValue, bool extraUnwind = false)
        {
            Vessel = null;
            Target = new Quaternion();
            RollControlRange = 5.0 * Mathf.Deg2Rad;

            //Use http://www.ni.com/white-paper/3782/en/ to fine-tune
            pitchRatePI = new PIDLoop(kp, ki, kd, maxoutput, minoutput, extraUnwind);
            yawRatePI = new PIDLoop(kp, ki, kd, maxoutput, minoutput, extraUnwind);
            rollRatePI = new PIDLoop(kp, ki, kd, maxoutput, minoutput, extraUnwind);

            pitchPI = new TorquePI();
            yawPI = new TorquePI();
            rollPI = new TorquePI();

            Reset();
        }

        public void SetVessel(Vessel vessel)
        {
            this.Vessel = vessel;
        }

        public void setPIDParameters(double kp, double ki, double kd)
        {
            pitchRatePI.Kd = kp;
            pitchRatePI.Ki = ki;
            pitchRatePI.Kd = kd;

            yawRatePI.Kd = kp;
            yawRatePI.Ki = ki;
            yawRatePI.Kd = kd;

            rollRatePI.Kd = kp;
            rollRatePI.Ki = ki;
            rollRatePI.Kd = kd;

            Reset();
        }

        public void OnFixedUpdate()
        {
            if (Vessel != null)
            {
                UpdateStateVectors(Vessel, Target);
                SteeringHelper.AnalyzeParts(Vessel);

                Vector3 Torque = SteeringHelper.TorqueAvailable;
                var CoM = Vessel.CoM;
                var MoI = Vessel.MOI;

                phiTotal = calculatePhiTotal();
                phiVector = calculatePhiVector();//deviation errors from orientation target

                for (int i = 0; i < 3; i++)
                {
                    MaxOmega[i] = Mathf.Max(Torque[i] / MoI[i], 0.0001f);
                }

                TargetOmega[0] = pitchRatePI.Update(-phiVector[0], 0, MaxOmega[0]);
                TargetOmega[1] = rollRatePI.Update(-phiVector[1], 0, MaxOmega[1]);
                TargetOmega[2] = yawRatePI.Update(-phiVector[2], 0, MaxOmega[2]);

                if (Math.Abs(phiTotal) > RollControlRange)
                {
                    TargetOmega[1] = 0;
                    rollRatePI.ResetI();
                }

                TargetTorque[0] = pitchPI.Update(Omega[0], TargetOmega[0], Vessel.MOI[0], Torque[0]);
                TargetTorque[1] = rollPI.Update(Omega[1], TargetOmega[1], Vessel.MOI[1], Torque[1]);
                TargetTorque[2] = yawPI.Update(Omega[2], TargetOmega[2], Vessel.MOI[2], Torque[2]);
            }
        }

        public Vector3d GetActuation(Quaternion thisTarget)
        {
            Target = thisTarget;

            UpdateStateVectors(Vessel, Target);
            SteeringHelper.UpdateRCSThrustAndTorque(Vessel);
            Vector3 Torque = SteeringHelper.TorqueAvailable;

            for (int i = 0; i < 3; i++)
            {
                Actuation[i] = TargetTorque[i] / Torque[i];
                if (Math.Abs(Actuation[i]) < EPSILON || double.IsNaN(Actuation[i]) || double.IsInfinity(Actuation[i]))
                {
                    Actuation[i] = 0;
                }
            }

            return Actuation;
        }

        public Vector3d calculatePhiVector()
        {
            Vector3d Phi = Vector3d.zero;

            Phi[0] = Vector3d.Angle(vesselForward, Vector3d.Exclude(vesselStarboard, targetForward)) * Mathf.Deg2Rad;
            if (Vector3d.Angle(vesselTop, Vector3d.Exclude(vesselStarboard, targetForward)) > 90)
                Phi[0] *= -1;

            Phi[1] = Vector3d.Angle(vesselTop, Vector3d.Exclude(vesselForward, targetTop)) * Mathf.Deg2Rad;
            if (Vector3d.Angle(vesselStarboard, Vector3d.Exclude(vesselForward, targetTop)) > 90)
                Phi[1] *= -1;

            Phi[2] = Vector3d.Angle(vesselForward, Vector3d.Exclude(vesselTop, targetForward)) * Mathf.Deg2Rad;
            if (Vector3d.Angle(vesselStarboard, Vector3d.Exclude(vesselTop, targetForward)) > 90)
                Phi[2] *= -1;

            return Phi;
        }

        public double calculatePhiTotal()
        {
            double PhiTotal = Vector3d.Angle(vesselForward, targetForward) * Mathf.Deg2Rad;
            if (Vector3d.Angle(vesselTop, targetForward) > 90)
                PhiTotal *= -1;

            return PhiTotal;
        }

        private void UpdateStateVectors(Vessel thisVessel, Quaternion thisTarget)
        {
            if (thisVessel != null)
            {
                VesselRotation = thisVessel.ReferenceTransform.rotation * Quaternion.Euler(-90, 0, 0);
                vesselForward = VesselRotation * Vector3d.forward;
                vesselTop = VesselRotation * Vector3d.up;
                vesselStarboard = VesselRotation * Vector3d.right;

                Omega = -thisVessel.angularVelocity;
            }

            if (thisTarget != null)
            {
                targetForward = thisTarget * Vector3d.forward;
                targetTop = thisTarget * Vector3d.up;
                //targetStarboard = thisTarget * Vector3d.right; // comment: no use?
            }
        }

        public void Reset()
        {
            pitchPI.ResetI();
            yawPI.ResetI();
            rollPI.ResetI();

            pitchRatePI.ResetI();
            yawRatePI.ResetI();
            rollRatePI.ResetI();
        }
    }

    /// <summary>
    /// Imported from kOS with minor modifications
    /// 9 Sept 2017
    /// https://github.com/KSP-KOS/KOS/blob/3ccee6786e52be7a2276b24837fcb1a562e51be4/src/kOS.Safe/Encapsulation/PIDLoop.cs
    /// Based on https://github.com/lamont-granquist/MechJim/blob/6a10e09b86b969b1b0ffe00881c6f4626ba52bd7/Source/PIDLoop.cs
    /// </summary>
    public class PIDLoop
    {
        public double Kp { get; set; }
        public double Ki { get; set; }
        public double Kd { get; set; }
        public double Input { get; set; }
        public double Setpoint { get; set; }
        public double Error { get; set; }
        public double Output { get; set; }
        public double MaxOutput { get; set; }
        public double MinOutput { get; set; }
        public double ErrorSum { get; set; }
        public double PTerm { get; set; }
        public double ITerm { get; set; }
        public double DTerm { get; set; }
        public bool ExtraUnwind { get; set; }
        public double ChangeRate { get; set; }
        private bool unWinding;

        public PIDLoop() : this(1, 0, 0) { }

        public PIDLoop(double kp, double ki, double kd, double maxoutput = double.MaxValue, double minoutput = double.MinValue, bool extraUnwind = false)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;
            Input = 0;
            Setpoint = 0;
            Error = 0;
            Output = 0;
            MaxOutput = maxoutput;
            MinOutput = minoutput;
            ErrorSum = 0;
            PTerm = 0;
            ITerm = 0;
            DTerm = 0;
            ExtraUnwind = extraUnwind;
        }

        public double Update(double input, double setpoint, double minOutput, double maxOutput)
        {
            MaxOutput = maxOutput;
            MinOutput = minOutput;
            Setpoint = setpoint;
            return Update(input);
        }

        public double Update(double input, double setpoint, double maxOutput)
        {
            return Update(input, setpoint, -maxOutput, maxOutput);
        }

        public double Update(double input)
        {
            double error = Setpoint - input;
            double pTerm = error * Kp;
            double iTerm = 0;
            double dTerm = 0;
            double dt = TimeWarp.fixedDeltaTime;

            if (Ki != 0)
            {
                if (ExtraUnwind)
                {
                    if (Math.Sign(error) != Math.Sign(ErrorSum))
                    {
                        if (!unWinding)
                        {
                            Ki *= 2;
                            unWinding = true;
                        }
                    }
                    else if (unWinding)
                    {
                        Ki /= 2;
                        unWinding = false;
                    }
                }
                iTerm = ITerm + error * dt * Ki;
            }

            ChangeRate = (input - Input) / dt;
            if (Kd != 0)
            {
                dTerm = -ChangeRate * Kd;
            }
            
            Output = pTerm + iTerm + dTerm;
            if (Output > MaxOutput)
            {
                Output = MaxOutput;
                if (Ki != 0)
                {
                    iTerm = Output - Math.Min(pTerm + dTerm, MaxOutput);
                }
            }

            if (Output < MinOutput)
            {
                Output = MinOutput;
                if (Ki != 0)
                {
                    iTerm = Output - Math.Max(pTerm + dTerm, MinOutput);
                }
            }
            
            Input = input;
            Error = error;
            PTerm = pTerm;
            ITerm = iTerm;
            DTerm = dTerm;
            if (Ki != 0) ErrorSum = iTerm / Ki;
            else ErrorSum = 0;
            return Output;
        }

        public void ResetI()
        {
            ErrorSum = 0;
            ITerm = 0;
        }

        public override string ToString()
        {
            return string.Format("PIDLoop(Kp:{0}, Ki:{1}, Kd:{2}, Setpoint:{3}, Error:{4}, Output:{5})",
                Kp, Ki, Kd, Setpoint, Error, Output);
        }

        public string ConstrutorString()
        {
            return string.Format("pidloop({0}, {1}, {2}, {3}, {4})", Ki, Kp, Kd, MaxOutput, ExtraUnwind);
        }
    }

    /// <summary>
    /// Imported along with PIDLoop
    /// 9 Sept 2017
    /// </summary>
    public class TorquePI
    {
        public PIDLoop Loop { get; set; }
        public double I { get; private set; }
        public MovingAverage TorqueAdjust { get; set; }

        private double tr;
        public double Tr
        {
            get { return tr; }
            set
            {
                tr = value;
                ts = 4.0 * tr / 2.76;
            }
        }

        private double ts;
        public double Ts
        {
            get { return ts; }
            set
            {
                ts = value;
                tr = 2.76 * ts / 4.0;
            }
        }

        public TorquePI()
        {
            Loop = new PIDLoop();
            Ts = 2;
            TorqueAdjust = new MovingAverage();
        }

        public double Update(double input, double setpoint, double MomentOfInertia, double maxOutput)
        {
            I = MomentOfInertia;

            Loop.Ki = MomentOfInertia * (Math.Pow(4.0 / ts, 2)); //weird bug pf Ki = 1 without ()
            Loop.Kp = 2 * Math.Pow(MomentOfInertia * Loop.Ki, 0.5);
            return TorqueAdjust.Update(Loop.Update(input, setpoint, maxOutput));
        }

        public void ResetI()
        {
            Loop.ResetI();
            TorqueAdjust.Reset();
        }
    }

    /// <summary>
    /// Imported along with PIDLoop
    /// 9 Sept 2017
    /// </summary>
    public class MovingAverage
    {
        public List<double> Values { get; set; }
        public double Mean { get; private set; }
        public int ValueCount { get { return Values.Count; } }
        public int SampleLimit { get; set; }

        public MovingAverage()
        {
            Reset();
            SampleLimit = 2;
        }

        public void Reset()
        {
            Mean = 0;
            if (Values == null)
                Values = new List<double>();
            else
                Values.Clear();
        }

        public double Update(double value)
        {
            if (double.IsInfinity(value) || double.IsNaN(value)) return value;

            Values.Add(value);

            while (Values.Count > SampleLimit)
            {
                Values.RemoveAt(0);
            }

            double sum = 0;
            double count = 0;
            double max = double.MinValue;
            double min = double.MaxValue;

            for (int i = 0; i < Values.Count; i++)
            {
                double val = Values[i];
                if (val > max)
                {
                    if (max != double.MinValue)
                    {
                        sum += max;
                        count++;
                    }
                    max = val;
                }
                else if (val < min)
                {
                    if (min != double.MaxValue)
                    {
                        sum += min;
                        count++;
                    }
                    min = val;
                }
                else
                {
                    sum += val;
                    count++;
                }
            }
            if (count == 0)
            {
                if (max != double.MinValue)
                {
                    sum += max;
                    count++;
                }
                if (min != double.MaxValue)
                {
                    sum += min;
                    count++;
                }
            }
            Mean = sum / count;
            return Mean;
        }
    }

    /// <summary>
    /// Imported from MechJeb2 for torque calculations
    /// 17 June 2019
    /// </summary>
    public class Vector6
    {
        public Vector3d positive = Vector3d.zero, negative = Vector3d.zero;
        public enum Direction { FORWARD = 0, BACK = 1, UP = 2, DOWN = 3, RIGHT = 4, LEFT = 5 };
        public static readonly Vector3d[] directions = { Vector3d.forward, Vector3d.back, Vector3d.up, Vector3d.down, Vector3d.right, Vector3d.left };
        public static readonly Direction[] Values = (Direction[])Enum.GetValues(typeof(Direction));

        public double forward { get { return positive.z; } set { positive.z = value; } }
        public double back { get { return negative.z; } set { negative.z = value; } }
        public double up { get { return positive.y; } set { positive.y = value; } }
        public double down { get { return negative.y; } set { negative.y = value; } }
        public double right { get { return positive.x; } set { positive.x = value; } }
        public double left { get { return negative.x; } set { negative.x = value; } }

        public double this[Direction index]
        {
            get
            {
                switch (index)
                {
                    case Direction.FORWARD:
                        return forward;
                    case Direction.BACK:
                        return back;
                    case Direction.UP:
                        return up;
                    case Direction.DOWN:
                        return down;
                    case Direction.RIGHT:
                        return right;
                    case Direction.LEFT:
                        return left;
                }
                return 0;
            }
            set
            {
                switch (index)
                {
                    case Direction.FORWARD:
                        forward = value;
                        break;
                    case Direction.BACK:
                        back = value;
                        break;
                    case Direction.UP:
                        up = value;
                        break;
                    case Direction.DOWN:
                        down = value;
                        break;
                    case Direction.RIGHT:
                        right = value;
                        break;
                    case Direction.LEFT:
                        left = value;
                        break;
                }
            }
        }

        public Vector6()
        {
        }

        public Vector6(Vector3d positive, Vector3d negative)
        {
            this.positive = positive;
            this.negative = negative;
        }

        public void Reset()
        {
            positive = Vector3d.zero;
            negative = Vector3d.zero;
        }

        public void Add(Vector3d vector)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                Direction d = Values[i];
                double projection = Vector3d.Dot(vector, directions[(int)d]);
                if (projection > 0)
                {
                    this[d] += projection;
                }
            }
        }

        public double GetMagnitude(Vector3d direction)
        {
            double sqrMagnitude = 0;
            for (int i = 0; i < Values.Length; i++)
            {
                Direction d = Values[i];
                double projection = Vector3d.Dot(direction.normalized, directions[(int)d]);
                if (projection > 0)
                {
                    sqrMagnitude += Math.Pow(projection * this[d], 2);
                }
            }
            return Math.Sqrt(sqrMagnitude);
        }
    }
}
