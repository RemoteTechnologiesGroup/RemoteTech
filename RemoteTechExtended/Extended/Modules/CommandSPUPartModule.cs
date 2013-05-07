using System;

namespace RemoteTech
{
    public class CommandSPUPartModule : SPUPartModule {

        public int Crew { get { this.vessel.GetCrewCount(); } }

        public CommandSPUPartModule() {
        }

    }

    public static class CommandSPUExtensions {
        public static CommandSPUPartModule FindCommandSPU(this Vessel vessel) {
            foreach (Part p in vessel.parts) {
                if (p.Modules.Contains(typeof(CommandSPUPartModule).ToString())) {
                    return p.Modules[typeof(CommandSPUPartModule).ToString()] as CommandSPUPartModule;
                }
            }
            return null;
        }
    }

}

