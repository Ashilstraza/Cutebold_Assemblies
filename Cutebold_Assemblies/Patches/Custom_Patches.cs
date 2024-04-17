#if DEBUG && RW1_4
using SomeThingsFloat;
using Rumor_Code;
using GrowingZonePlus;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
using Verse;
#endif

#if !RWPre1_4
using DubsBadHygiene;
using HarmonyLib;
using System;

namespace Cutebold_Assemblies.Patches
{
    /// <summary>
    /// Dub's Bad Hygene patch for handling non-human babies.
    /// </summary>
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
    // Personal Patches
#if DEBUG && RW1_4
    internal class SomeThingsFloat
    {
        public SomeThingsFloat(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(FloatingThings_MapComponent), "updateListOfWaterCells"), transpiler: new HarmonyMethod(typeof(SomeThingsFloat), nameof(QuickFix_SomeThingsFloat_Transpiler)));
        }

        public static IEnumerable<CodeInstruction> QuickFix_SomeThingsFloat_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo map = AccessTools.Field(typeof(MapComponent), "map");
            FieldInfo terrainGrid = AccessTools.Field(typeof(Map), "terrainGrid");
            FieldInfo underGrid = AccessTools.Field(typeof(TerrainGrid), "underGrid");
            FieldInfo topGrid = AccessTools.Field(typeof(TerrainGrid), "topGrid");

            List<CodeInstruction> instructionList = instructions.ToList();
            int instructionListCount = instructionList.Count;
            int n = 0;

            List<CodeInstruction> fix =
            [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, map),
                new(OpCodes.Ldfld, terrainGrid),
                new(OpCodes.Ldfld, underGrid),
                new(OpCodes.Ldloc_0),
                new(OpCodes.Ldelem_Ref),
                new(OpCodes.Brfalse, null)
            ];

            for (int i = 0; i < instructionListCount; i++)
            {
                yield return instructionList[i];

                if (i < instructionListCount && i > 4 && instructionList[i - 4].Is(OpCodes.Ldfld, topGrid))
                {
                    if (n == 1)
                    {
                        foreach (CodeInstruction instruction in fix)
                        {
                            if (instruction.opcode == OpCodes.Brfalse) instruction.operand = instructionList[i + 8].operand;

                            yield return instruction;
                        }
                    }

                    n++;
                }
            }
        }
    }

    internal class RFRumorHasIt
    {
        public RFRumorHasIt(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(ThirdPartyManager), "FindCliques"), prefix: new HarmonyMethod(typeof(RFRumorHasIt), nameof(QuickFix_RFRumorHasIt)));
        }

        public static bool QuickFix_RFRumorHasIt()
        {
            return false;
        }
    }

    internal class GZP
    {
        public GZP(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(Zone_GrowingPlus), "ExposeData"), postfix: new HarmonyMethod(typeof(GZP), nameof(GZP_ExposeData_Postfix)));
        }
        public static void GZP_ExposeData_Postfix(object __instance)
        {
            Zone_GrowingPlus growingZonePlus = __instance as Zone_GrowingPlus;
            var billStack = growingZonePlus.customBillStack;
            foreach (var bill in billStack)
            {
                var UID = Traverse.Create(bill).Field("zoneUniqueID");
                if (UID.GetValue() == null)
                {
                    UID.SetValue(growingZonePlus.UniqueID);
                }
                bill.zgp ??= growingZonePlus;

            }
        }
    }
#endif
}
#endif
