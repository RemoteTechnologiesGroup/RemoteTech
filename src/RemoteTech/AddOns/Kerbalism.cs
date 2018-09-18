using System;


namespace RemoteTech.AddOns
{
    /// <summary> Simple class to detect if Kerbalism is loaded </summary>
    public static class Kerbalism
    {
        private static readonly Type API;

        // constructor
        static Kerbalism()
        {
            foreach (AssemblyLoader.LoadedAssembly a in AssemblyLoader.loadedAssemblies)
            {
                if (a.name == "Kerbalism")
                {
                    API = a.assembly.GetType("KERBALISM.API");
                    break;
                }
            }
        }

        /// <summary> Returns true if Kerbalism is detected for the current game </summary>
        public static bool Exists
        {
            get { return API != null; }
        }
    }
}

