using DubsBadHygiene;
using HarmonyLib;
using SomeThingsFloat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutebold_Assemblies.Patches
{
    internal class DBHPatches
    {
        public DBHPatches(Harmony harmony, Type alienPatches)
        {
            harmony.Patch(AccessTools.Method(typeof(NeedsUtil), "ShouldHaveNeed"), transpiler: new HarmonyMethod(alienPatches, nameof(Alien_Patches.Alien_FixBaby_Transpiler)));
            harmony.Patch(AccessTools.Method(typeof(WorkGiver_washPatient), "ShouldBeWashed"), transpiler: new HarmonyMethod(alienPatches, nameof(Alien_Patches.Alien_FixBaby_Transpiler)));
            harmony.Patch(AccessTools.Method(typeof(WorkGiver_washChild), "ShouldBeWashed"), transpiler: new HarmonyMethod(alienPatches, nameof(Alien_Patches.Alien_FixBaby_Transpiler)));
            harmony.Patch(AccessTools.Method(typeof(WorkGiver_washChild), "PotentialWorkThingsGlobal"), transpiler: new HarmonyMethod(alienPatches, nameof(Alien_Patches.Alien_FixBaby_Transpiler)));
        }
    }
    internal class SomeThingsFloat
    {
        public SomeThingsFloat(Harmony harmony, Type alienPatches)
        {
            harmony.Patch(AccessTools.Method(typeof(FloatingThings_MapComponent), "updateListOfWaterCells"), transpiler: new HarmonyMethod(alienPatches, nameof(Alien_Patches.QuickFix_SomeThingsFloat_Transpiler)));
        }
    }
}
