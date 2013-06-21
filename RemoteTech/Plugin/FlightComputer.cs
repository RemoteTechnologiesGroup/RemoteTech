using System;
using UnityEngine;

namespace RemoteTech {
    public enum FlightMode {
        Off,
        KillRot,
        AttitudeHold,
        AltitudeHold,
    }

    public enum FlightAttitude {
        Prograde,
        Retrograde,
        NormalPlus,
        NormalMinus,
        RadialPlus,
        RadialMinus,
        Surface,
    }

    public enum ReferenceFrame {
        Orbit,
        Surface,
        Target,
        North,
        Maneuver,
        World,
    }

    public class FlightCommand : IComparable<FlightCommand> {
        public FlightMode Mode { get; set; }
        public FlightAttitude Attitude { get; set; }
        public ReferenceFrame Frame { get; set; }
        public Vector3 Direction { get; set; }

        public float Throttle { get; set; }
        public double Duration { get; set; }
        public double DeltaV { get; set; }

        public float AltitudeHold { get; set; }

        public double EffectiveFrom { get; set; }

        public FlightCommand(double effectiveTime) {
            Mode = FlightMode.Off;
            Attitude = FlightAttitude.Prograde;
            Frame = ReferenceFrame.Orbit;
            Direction = Vector3.zero;
            Throttle = Single.NaN;
            Duration = Double.NaN;
            DeltaV = Double.NaN;
            AltitudeHold = Single.NaN;
            EffectiveFrom = effectiveTime;
        }

        public FlightCommand WithAttitude(ReferenceFrame frame, FlightAttitude attitude) {
            Mode = FlightMode.AttitudeHold;
            Frame = frame;
            Attitude = attitude;
            return this;
        }

        public FlightCommand WithAltitude(float altitude) {
            Mode = FlightMode.AltitudeHold;
            AltitudeHold = altitude;
            return this;
        }

        public FlightCommand AddDurationBurn(float throttle, double duration) {
            Throttle = throttle;
            DeltaV = Double.NaN;
            Duration = duration;
            return this;
        }

        public FlightCommand AddDeltaVBurn(float throttle, double deltaV) {
            Throttle = throttle;
            DeltaV = deltaV;
            Duration = Double.NaN;
            return this;
        }

        public int CompareTo(FlightCommand fc) {
            return EffectiveFrom.CompareTo(fc.EffectiveFrom);
        }
    }
    
    public class FlightComputer : IDisposable {
        private const float KProportional = 120;
        private const float KIntegral = 0;
        private const float KDerivative = 800;

        private FlightCommand mCommand;
        private Quaternion mKillrot;
        private Vector3 mManeuver;
        private VesselSatellite mSatellite;
        private PriorityQueue<FlightCommand> mCommandQueue;

        private Legacy.FlightComputer mLegacyComputer;

        public FlightComputer(VesselSatellite vs) {
            mSatellite = vs;
            mLegacyComputer = new Legacy.FlightComputer(vs.Vessel);
            mCommandQueue = new PriorityQueue<FlightCommand>();
            mCommand = new FlightCommand(0.0f);
            vs.Vessel.OnFlyByWire += OnFlyByWire;
        }

        public void Dispose() {
            if (mSatellite.Vessel != null) {
                mSatellite.Vessel.OnFlyByWire -= OnFlyByWire;
            }
        }

        public void Enqueue(FlightCommand fc) {
            mCommand = fc;
            mSatellite.Vessel.OnFlyByWire += OnFlyByWire;
            mKillrot = Quaternion.LookRotation(mSatellite.Vessel.GetTransform().up,
                                               mSatellite.Vessel.GetTransform().forward);
        }

        private void OnFlyByWire(FlightCtrlState fs) {
            while (mCommandQueue.Count > 0 && mCommandQueue.Peek().EffectiveFrom < RTUtil.GetGameTime()) {
                mCommand = mCommandQueue.Dequeue();
            }
            if (mCommand.Mode == FlightMode.Off) return;
            Quaternion direction = Quaternion.identity;
            switch (mCommand.Mode) {
                case FlightMode.KillRot:
                    ProcessOrientation(fs, mKillrot);
                    break;
                case FlightMode.AttitudeHold:
                    HoldAttitude(fs);
                    break;
                case FlightMode.AltitudeHold:
                    HoldAltitude(fs);
                    break;
            }
            Burn(fs);
        }
        private void ProcessOrientation(FlightCtrlState fs, Quaternion target) {
            mLegacyComputer.drive(fs, target);
        }

        private void HoldAttitude(FlightCtrlState fs) {
            Vessel v = mSatellite.Vessel;
            Vector3 forward = Vector3.zero;
            Vector3 up = Vector3.zero;
            Quaternion rotationReference;
            switch (mCommand.Frame) {
                case ReferenceFrame.Orbit:
                    forward = v.GetObtVelocity().normalized;
                    up = (v.mainBody.position - v.CoM);
                    Vector3.OrthoNormalize(ref forward, ref up);
                    break;
                case ReferenceFrame.Surface:
                    forward = v.GetSrfVelocity().normalized;
                    up = (v.mainBody.position - v.CoM);
                    Vector3.OrthoNormalize(ref forward, ref up);
                    break;
                case ReferenceFrame.Target: // TODO
                    forward = v.GetObtVelocity().normalized;
                    up = (v.mainBody.position - v.CoM);
                    Vector3.OrthoNormalize(ref forward, ref up);
                    break;
                case ReferenceFrame.North:
                    up = (v.mainBody.position - v.CoM);
                    forward = Vector3.Exclude(
                        up, 
                        v.mainBody.position + v.mainBody.transform.up * (float) v.mainBody.Radius - v.CoM
                     ).normalized;
                    break;
                case ReferenceFrame.Maneuver: // TODO
                    forward = v.GetObtVelocity().normalized;
                    up = (v.mainBody.position - v.CoM);
                    break;
            }
            Vector3.OrthoNormalize(ref forward, ref up);
            rotationReference = Quaternion.LookRotation(forward, up);
            switch (mCommand.Attitude) {
                case FlightAttitude.Prograde:
                    break;
                case FlightAttitude.Retrograde:
                    rotationReference = rotationReference * Quaternion.AngleAxis(180, Vector3.up);
                    break;
                case FlightAttitude.NormalPlus:
                    rotationReference = rotationReference * Quaternion.AngleAxis(90, Vector3.up);
                    break;
                case FlightAttitude.NormalMinus:
                    rotationReference = rotationReference * Quaternion.AngleAxis(90, Vector3.down);
                    break;
                case FlightAttitude.RadialPlus:
                    rotationReference = rotationReference * Quaternion.AngleAxis(90, Vector3.right);
                    break;
                case FlightAttitude.RadialMinus:
                    rotationReference = rotationReference * Quaternion.AngleAxis(90, Vector3.left);
                    break;
                case FlightAttitude.Surface:
                    rotationReference = rotationReference * Quaternion.Euler(mCommand.Direction);
                    break;
            }
            ProcessOrientation(fs, rotationReference);
            // Leave out the mapping of the vessel's up to forward because of legacy code.
        }

        private void HoldAltitude(FlightCtrlState fs) {

        }

        private void Burn(FlightCtrlState fs) {
            if (!Single.IsNaN(mCommand.Throttle)) {
                if (!Double.IsNaN(mCommand.Duration) && mCommand.Duration > Double.Epsilon) {
                    fs.mainThrottle = mCommand.Throttle;
                    mCommand.Duration -= Time.deltaTime;
                } else if (!Double.IsNaN(mCommand.DeltaV) && mCommand.DeltaV > Double.Epsilon) {
                    fs.mainThrottle = mCommand.Throttle;
                } else {
                    fs.mainThrottle = 0;
                    mCommand.Throttle = Single.NaN;
                }
            }
        }
    }
}