using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Cutebold_Assemblies")]
[assembly: AssemblyDescription("Miscellaneous methods for the Rimworld cutebold race.")]
[assembly: AssemblyProduct("Cutebold Race")]
[assembly: AssemblyCopyright("Copyright Ashilstraza, see License.txt for more information.")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(true)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("595e3fe8-c183-4363-83f6-03e75c56d839")]

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
[assembly: AssemblyVersion("1.5.5")]

// 1.5.5 Fixed Cutebold Spawning, added error checking for patch handler
// 1.5.4 Switched cutebold blinking to be more centralized.
// 1.5.3 Split alien patches off into their own project, external patch handler
// 1.5.2 A few tweaks, re-enabled DBH patches, changed version stuff to be: Major Version, RW Version, Update Number.
// 1.0.17 Update to RW 1.5; rejoice for we don't have to fuck with the pawnrenderer! Also lots of refactoring and code cleanup.
// 1.0.16 Dub's Bad Hygene patches, error when clicking on baby stats. Added various patches. Removed tongue fixer.
// 1.0.15 Allow for renaming pawns that lack a first and/or last name, enabes alien patches for any alien, made speed optimizations
// 1.0.14.2 Built against latest versions of HAR and Rimworld as of 2/21/23
// 1.0.14.1 Built against latest versions of HAR and Rimworld as of 2/3/23
// 1.0.14 Changes to allow for nicer interactions with transpilers and enable goggle layering
// 1.0.13 Fix for VFE fuckery
// 1.0.12 Rimworld 1.4 compatability
// 1.0.11 Changed settings UI to be able to be translated.
// 1.0.10 Added fixer button for hediffs that get tongued.
// 1.0.9: Limit GlowFactor to just the three main stats.
// 1.0.8: Changes for Ideology
// 1.0.7: Rimworld 1.3 compatability
// 1.0.6: Added assembly version into settings debug, fix for worn goggles on load, fix for cast error in name generator
// 1.0.5: Cutebold Mime Eye Color
// 1.0.4: Yield Bug Fix
// 1.0.3: Dark Adaptation + Yield
// 1.0.0: Initial