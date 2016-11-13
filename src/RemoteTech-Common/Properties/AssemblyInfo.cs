using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("RemoteTech-Common")]
[assembly: AssemblyDescription("RemoteTech Common Component")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("RemoteTechnologiesGroup")]
[assembly: AssemblyProduct("RemoteTech-Common")]
[assembly: AssemblyCopyright("Copyright ©  2013-2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d93d9a84-2a93-4cd8-b543-0ae5728554c5")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

// Don't include the patch revision in the AssemblyVersion - as this will break any dependent
// DLLs any time it changes.  Breaking on a minor revision is probably acceptable - it's
// unlikely that there wouldn't be other breaking changes on a minor version change.
[assembly: AssemblyVersion("2.0")]
[assembly: AssemblyFileVersion("2.0.0")]

// Use KSPAssembly to allow other DLLs to make this DLL a dependency in a
// non-hacky way in KSP.  Format is (AssemblyProduct, major, minor), and it
// does not appear to have a hard requirement to match the assembly version.
[assembly: KSPAssembly("RemoteTech-Common", 2, 0)]