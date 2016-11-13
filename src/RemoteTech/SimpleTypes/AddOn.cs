using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using RemoteTech.Common;

namespace RemoteTech.SimpleTypes
{
    public abstract class AddOn
    {
        /// <summary>Holds the current assembly type</summary>
        protected Type AssemblyType;
        /// <summary>Binding flags for invoking the methods</summary>
        protected BindingFlags BindFlags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static;
        /// <summary>Instance object for invoking instance methods</summary>
        protected object Instance;

        /// <summary>Assembly loaded?</summary>
        public bool AssemblyLoaded { get; }


        protected AddOn(string assemblyName, string assemblyType)
        {
            RTLog.Verbose("Connecting with {0} ...", RTLogLevel.Assembly, assemblyName);

            var loadedAssembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name.Equals(assemblyName));
            if (loadedAssembly == null)
                return;

            RTLog.Notify("Successfull connected to Assembly {0}", RTLogLevel.Assembly, assemblyName);
            AssemblyType = loadedAssembly.assembly.GetTypes().FirstOrDefault(t => t.FullName.Equals(assemblyType));

            AssemblyLoaded = true;
        }

        /// <summary>
        /// Reads the current called method name and takes this method
        /// over to the assembly. The return value on a not successfull
        /// call is null.
        /// </summary>
        /// <param name="parameters">Object parameter list, given to the assembly method</param>
        /// <returns>Null on a non successfull call</returns>
        protected object Invoke(object[] parameters)
        {
            if (!AssemblyLoaded)
                return null;

            // look 1 call behind to get the name of the method who is called
            var stackTrace = new StackTrace();
            var stackFrame = stackTrace.GetFrame(1);

            try
            {
                // invoke the method
                var result = AssemblyType.InvokeMember(stackFrame.GetMethod().Name, BindFlags, null, Instance, parameters);
                RTLog.Verbose("AddOn.InvokeResult for {0} with instance: {1} is '{2}'", RTLogLevel.Assembly, stackFrame.GetMethod().Name, Instance, result);

                return result;
            }
            catch (Exception ex)
            {
                RTLog.Verbose("AddOn.InvokeException for {0} with instance: {1} is '{2}'", RTLogLevel.Assembly, stackFrame.GetMethod().Name, Instance, ex);
            }

            // default value is null
            return null;
        }
    }
}
