using System;

namespace RemoteTech {
    public class AttitudeChange {
        public readonly Attitude Attitude;
        public readonly float Heading;
        public readonly float Pitch;
        public readonly float Roll;

        public AttitudeChange(Attitude attitude, float pitch, float heading, float roll) {
            Attitude = attitude;
            Pitch = pitch;
            Heading = heading;
            Roll = roll;
        }

        public AttitudeChange(Attitude attitude, float pitch, float heading) {
            Attitude = attitude;
            Pitch = pitch;
            Heading = heading;
            Roll = Single.NaN;
        }

        public AttitudeChange(Attitude attitude) {
            Attitude = attitude;
            Pitch = Single.NaN;
            Heading = Single.NaN;
            Roll = Single.NaN;
        }
    }
}
