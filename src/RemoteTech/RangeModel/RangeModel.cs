﻿using System;

namespace RemoteTech.RangeModel
{
    [Flags]
    public enum RangeModel
    {
        Standard,
        Additive,
        Root = Additive,
    }
}
