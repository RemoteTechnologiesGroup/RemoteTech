using System;

namespace RemoteTech
{
    public class Dish
    {
        public readonly float Range;
        public readonly double Radians;
        public readonly Guid Target;

        public Dish(Guid target, double radians, float distance)
        {
            Target = target;
            Radians = radians;
            Range = distance;
        }

        public override String ToString()
        {
            return String.Format("Dish(Range: {0}, Radians: {1}, Target: {2}", 
                Range.ToString("F2"), 
                (Radians / Math.PI * 180).ToString("F2") + "deg",
                String.Format("{0} ({1})", Target, RTCore.Instance.Satellites[Target] != null 
                    ? RTCore.Instance.Satellites[Target].ToString() 
                    : "Unknown"));
        }
    }
}