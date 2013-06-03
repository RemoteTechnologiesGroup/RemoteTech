using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    public interface ISignalProcessor {
        Vessel Vessel { get; }
        bool Powered { get; }
    }
}
