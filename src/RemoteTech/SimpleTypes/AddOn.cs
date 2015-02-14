using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace RemoteTech.SimpleTypes
{
    public abstract class AddOn
    {
        private bool loaded = false;
        /// <summary>Holds the current assembly type</summary>
        private Type assemblyType;
        /// <summary>Binding flags for invoking the methods</summary>
        private BindingFlags bFlags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static;

        /// <summary>Assemlby loaded?</summary>
        public bool assemblyLoaded { get { return this.loaded; } }


        protected AddOn(string assemblyName, string assemblyType)
        {
            RTLog.Notify("Connecting with {0} ...", RTLogLevel.Assembly, assemblyName);

            var loadedAssembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name.Equals(assemblyName));
            if (loadedAssembly != null)
            {
                RTLog.Notify("Successfull connected to Assembly {0}", RTLogLevel.Assembly, assemblyName);
                this.assemblyType = loadedAssembly.assembly.GetTypes().FirstOrDefault(t => t.FullName.Equals(assemblyType));

                this.loaded = true;
            }
        }

        /// <summary>
        /// Reads the current called method name and takes this method
        /// over to the assembly. The return value on a not successfull
        /// call is null.
        /// </summary>
        /// <param name="parameters">Object parameter list, given to the assembly method</param>
        /// <returns>Null on a non successfull call</returns>
        protected object invoke(object[] parameters)
        {
            if(this.assemblyLoaded)
            {
                try
                {
                    // look 1 call behind to get the name of the method who is called
                    StackTrace st = new StackTrace();
                    StackFrame sf = st.GetFrame(1);

                    // invoke the method
                    var result = assemblyType.InvokeMember(sf.GetMethod().Name, bFlags, null, null, parameters);
                    RTLog.Verbose("AddOn.InvokeResult for {0} is '{1}'", RTLogLevel.Assembly, sf.GetMethod().Name, result);

                    return result;
                }
                catch (Exception ex)
                {
                    RTLog.Verbose("Exception from {0}", RTLogLevel.Assembly, ex);
                }
            }

            // default value is null
            return null;
        }
    }
}
