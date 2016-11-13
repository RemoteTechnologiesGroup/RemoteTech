using System;

namespace RemoteTech.Common.Utils
{
    public static class ClampUtil
    {
        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            return (value.CompareTo(min) < 0) ? min : (value.CompareTo(max) > 0) ? max : value;
        }

        public static double ClampDegrees360(double angle)
        {
            angle = angle % 360.0;
            return angle < 0 ? angle + 360.0 : angle;
        }

        public static double ClampDegrees180(double angle)
        {
            angle = ClampDegrees360(angle);
            if (angle > 180)
                angle -= 360;
            return angle;
        }

        public static float ClampDegrees360(float angle)
        {
            angle = angle % 360f;
            return angle < 0 ? angle + 360f : angle;
        }

        public static float ClampDegrees180(float angle)
        {
            angle = ClampDegrees360(angle);
            if (angle > 180)
                angle -= 360;
            return angle;
        }

        public static float AngleBetween(float angleFrom, float angleTo)
        {
            var angle = angleFrom - angleTo;
            while (angle < -180) angle += 360;
            while (angle > 180) angle -= 360;
            return angle;
        }

        public static float ClampDegrees90(float angle)
        {
            if (angle > 90)
                angle -= 180;
            else if (angle < -90)
                angle += 180;
            return angle;
        }
    }
}
