using System;

namespace RemoteTech
{
    public class AttitudeChange {
        public readonly Attitude Attitude;
        public readonly double Pitch;
        public readonly double Heading;
        public readonly double Roll;

        public AttitudeChange(Attitude attitude, double pitch, double heading, double roll) {
            this.Attitude = attitude;
            this.Pitch = pitch;
            this.Heading = heading;
            this.Roll = roll;
        }

        public AttitudeChange(Attitude attitude, double pitch, double heading) {
            this.Attitude = attitude;
            this.Pitch = pitch;
            this.Heading = heading;
            this.Roll = Double.NaN;
        }
        
        public AttitudeChange(Attitude attitude) { 
            if (attitude == Attitude.Surface) {
                throw new ArgumentException("This constructor can not be used with Attitude.Surface.");
            }
            this.Attitude = attitude;
            this.Pitch = Double.NaN;
            this.Heading = Double.NaN;
            this.Roll = Double.NaN;
        }

    }
}

