using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public enum RangeModelType
    {
        Standard,
        Additive,
        Root,
    }
    public class RangeModelBuilder
    {
        public static IRangeModel Create(RangeModelType type)
        {
            switch (type)
            {
                default:
                case RangeModelType.Standard:
                    return new RangeModelStandard();
                case RangeModelType.Root:
                case RangeModelType.Additive:
                    return new RangeModelRoot();
            }
        }
    }
}
