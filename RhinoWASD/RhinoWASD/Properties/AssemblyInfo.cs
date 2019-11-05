using System.Reflection;
using System.Runtime.InteropServices;


// RHINO Plugin ID, different for Debug and Release build.
#if DEBUG
[assembly: Guid("95ABCCA2-5CE3-486A-9592-32464857F283")]
#else
    [assembly: Guid("95ABCCA2-5CE3-486A-9592-32464857F282")]
#endif

[assembly: AssemblyTitle("RhinoWASD")]
[assembly: AssemblyDescription("Game-like navigation for Rhinoceros")]
[assembly: AssemblyCompany("blickfeld7")]
[assembly: AssemblyProduct("RhinoWASD")]
[assembly: AssemblyCopyright("© 2019 blickfeld7.com")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("0.1.*")]
[assembly: AssemblyInformationalVersion("2")]   // make compatible with Rhino Installer Engine