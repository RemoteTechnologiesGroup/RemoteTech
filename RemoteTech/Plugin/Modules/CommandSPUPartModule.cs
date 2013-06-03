using System;

namespace RemoteTech
{
    public class CommandSPUPartModule : ModuleSPU {

        public bool Active { get { return this.vessel.GetCrewCount() > 0; } }

        public CommandSPUPartModule() {
        }

    }
}

