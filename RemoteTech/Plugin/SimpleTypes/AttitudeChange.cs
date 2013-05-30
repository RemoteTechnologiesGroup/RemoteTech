using System;

namespace RemoteTech
{
    public class AttitudeChange {
        public readonly Attitude Attitude;
        public readonly float Pitch;
        public readonly float Heading;
        public readonly float Roll;

        public AttitudeChange(Attitude attitude, float pitch, float heading, float roll) {
            this.Attitude = attitude;
            this.Pitch = pitch;
            this.Heading = heading;
            this.Roll = roll;
        }

        public AttitudeChange(Attitude attitude, float pitch, float heading) {
            this.Attitude = attitude;
            this.Pitch = pitch;
            this.Heading = heading;
            this.Roll = Single.NaN;
        }
        
        public AttitudeChange(Attitude attitude) { 
            this.Attitude = attitude;
            this.Pitch = Single.NaN;
            this.Heading = Single.NaN;
            this.Roll = Single.NaN;
        }

    }
}

