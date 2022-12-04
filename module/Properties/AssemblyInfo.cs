using System.Reflection;
using System.Runtime.InteropServices;


// RHINO Plugin ID, different for Debug and Release build.
#if DEBUG
[assembly: Guid("95ABCCA2-5CE3-486A-9592-32464857F283")]
#else
[assembly: Guid("95ABCCA2-5CE3-486A-9592-32464857F282")]
#endif

[assembly: AssemblyTitle("Walkie")]
[assembly: AssemblyDescription("A plugin for Rhinoceros with game-like walk mode and cursor zoom depth")]
[assembly: AssemblyCompany("Cunarist")]
[assembly: AssemblyProduct("Walkie")]
[assembly: AssemblyCopyright("© 2022 Cunarist")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("4.9.0.0")]