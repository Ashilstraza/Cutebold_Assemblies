#if RWPre1_3
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
#endif

using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using Verse;

namespace Cutebold_Assemblies
{
    /// <summary>
    /// Conatins various harmony patches that the various cutebold hediffs touch. 
    /// </summary>
    class Cutebold_Patch_HediffRelated
    {
        /// <summary>
        /// Enables patches governing Dark Adaptation related methods depending on various options or other mods.
        /// </summary>
        /// <param name="harmony">Our harmony instance.</param>
        public Cutebold_Patch_HediffRelated(Harmony harmony)
        {
            Type thisClass = typeof(Cutebold_Patch_HediffRelated);
            if (Cutebold_Assemblies.CuteboldSettings.eyeAdaptation)
            {
                // Allows for dark adaptation, obviously not cave adaptation since that is a different game with cute kobolds.
                harmony.Patch(AccessTools.Method(typeof(StatPart_Glow), "FactorFromGlow"), postfix: new HarmonyMethod(thisClass, nameof(CuteboldFactorFromGlowPostfix)));
                if (Cutebold_Assemblies.CuteboldSettings.darknessOptions != Cutebold_DarknessOptions.IdeologyDefault)
                {
                    // Ignores the ignoreIfPrefersDarkness flag.
                    harmony.Patch(AccessTools.Method(typeof(StatPart_Glow), "ActiveFor"), postfix: new HarmonyMethod(thisClass, nameof(CuteboldGlowActiveForPostfix)));
                }
                // Applies dark adaptation to all cutebolds as they spawn.               
                harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.SpawnSetup)), postfix: new HarmonyMethod(thisClass, nameof(CuteboldAdaptationSpawnSetupPostfix)));
                // Update dark adaptation eye references.
                harmony.Patch(AccessTools.Method(typeof(HediffSet), nameof(HediffSet.DirtyCache)), postfix: new HarmonyMethod(thisClass, nameof(CuteboldHediffSetDirtyCachePostfix)));
                // Update dark adaptation goggle references.
#if RWPre1_3
                harmony.Patch(AccessTools.Method(typeof(Pawn_ApparelTracker), "Notify_ApparelAdded"), postfix: new HarmonyMethod(typeof(Cutebold_Patch_HediffRelated), "CuteboldApparelChangedPostfix"));
                harmony.Patch(AccessTools.Method(typeof(Pawn_ApparelTracker), "Notify_ApparelRemoved"), postfix: new HarmonyMethod(typeof(Cutebold_Patch_HediffRelated), "CuteboldApparelChangedPostfix"));
#else
                harmony.Patch(AccessTools.Method(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelChanged)), postfix: new HarmonyMethod(thisClass, nameof(CuteboldApparelChangedPostfix)));
#endif
            }
            else
            {
                // Removes dark adaptation to all cutebolds as they spawn in.
                harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.SpawnSetup)), postfix: new HarmonyMethod(thisClass, nameof(CuteboldNoAdaptationSpawnSetupPostfix)));
            }

            try
            {
                // Don't patch if CE is running, we will use that to put goggles below headgear.
                if (ModLister.GetActiveModWithIdentifier("CETeam.CombatExtended") == null/* && ModLister.GetActiveModWithIdentifier("OskarPotocki.VanillaFactionsExpanded.Core") == null*/)
                {
                    // Adjust layer offset for cutebold goggles before ideology got released.

#if RWPre1_3
                    harmony.Patch(AccessTools.Method(typeof(PawnRenderer), "RenderPawnInternal", [
                                        typeof(Vector3),
                                        typeof(float),
                                        typeof(bool),
                                        typeof(Rot4),
                                        typeof(Rot4),
                                        typeof(RotDrawMode),
                                        typeof(bool),
                                        typeof(bool),
                                        typeof(bool)
                                    ]), transpiler: new HarmonyMethod(typeof(Cutebold_Patch_HediffRelated), "CuteboldGogglesFixTranspiler"));
#endif
                }
            }
            catch (Exception e)
            {
                Log.Error($"{Cutebold_Assemblies.ModName}: Exception when trying to apply CuteboldGogglesFixTranspiler. Please notify the author for the cutebold mod with the logs. Thanks!\n{e}");
            }
        }

        /// <summary>
        /// Overrides the regular glow curve for cutebolds to allow for dark adaptation.
        /// </summary>
        /// <param name="__result">The previous glow curve result.</param>
        /// <param name="t">The thing that is being evaluated.</param>
        private static void CuteboldFactorFromGlowPostfix(StatPart_Glow __instance, ref float __result, Thing t)
        {
            if (t.def == Cutebold_Assemblies.CuteboldRaceDef)
            {

                switch (__instance.parentStat.defName)
                {
                    case "MoveSpeed":
                    case "SurgerySuccessChanceFactor":
                    case "WorkSpeedGlobal":
                        Hediff_CuteboldDarkAdaptation hediff = (Hediff_CuteboldDarkAdaptation)((Pawn)t).health.hediffSet.GetFirstHediffOfDef(Cutebold_DefOf.CuteboldDarkAdaptation);
                        if (hediff != null) __result = hediff.GlowCurve.Evaluate(GlowHandler((Pawn)t));
                        break;
                    default:
                        break;
                }
            }
        }

        #region  Pre 1.5 Glow Handler
#if RWPre1_5
        /// <summary>
        /// Checks the glowGrid at the given thing's position
        /// </summary>
        /// <param name="t">The thing to assess the glow level</param>
        /// <returns>The glow level</returns>
        public static float CuteboldGlowHandler(Pawn p)
        {
            if (p is null) return 0.5f;
            if (p.Spawned) return p.Map.glowGrid.GameGlowAt(p.Position);
            else if (p.CarriedBy != null) return p.CarriedBy.Map.glowGrid.GameGlowAt(p.CarriedBy.Position);
            else if (p.ParentHolder != null && p.ParentHolder is Caravan caravan)
            {
                float time = GenDate.HourFloat(GenTicks.TicksAbs, Find.WorldGrid.LongLatOf(caravan.Tile).x);

                if (time > 19 || time < 5) return 0f; // Night
                else if (time > 18 || time < 6) return 0.5f; // Dusk/Dawn
                else return 1f; // Day
            }
            return 0.5f;
        }
#endif
#endregion

        #region Post 1.5 Glow Handler
#if !RWPre1_5
/// <summary>
        /// Checks the glowGrid at the given thing's position
        /// </summary>
        /// <param name="t">The thing to assess the glow level</param>
        /// <returns>The glow level</returns>
        public static float GlowHandler(Pawn p)
        {
            if (p is null) return 0.5f;
            if (p.Spawned) return p.Map.glowGrid.GroundGlowAt(p.Position);
            else if (p.CarriedBy != null) return p.CarriedBy.Map.glowGrid.GroundGlowAt(p.CarriedBy.Position);
            else if (p.ParentHolder != null && p.ParentHolder is Caravan caravan)
            {
                float time = GenDate.HourFloat(GenTicks.TicksAbs, Find.WorldGrid.LongLatOf(caravan.Tile).x);

                if (time > 19 || time < 5) return 0f; // Night
                else if (time > 18 || time < 6) return 0.5f; // Dusk/Dawn
                else return 1f; // Day
            }
            return 0.5f;
        }
#endif
#endregion


        /// <summary>
        /// Overrides if cutebolds ignore darkness ignores.
        /// </summary>
        /// <param name="__result">If the glow curve should be ignored.</param>
        /// <param name="t">The thing that is being evaluated.</param>
        private static void CuteboldGlowActiveForPostfix(ref bool __result, Thing t)
        {
            if (t.def == Cutebold_Assemblies.CuteboldRaceDef)
            {
                __result = t.Spawned;
            }
        }

        /// <summary>
        /// Applies the dark adaptation hediff to cutebolds as they spawn if they don't have it.
        /// </summary>
        /// <param name="__instance">The pawn</param>
        /// <param name="map">The map they are located on. (unused)</param>
        /// <param name="respawningAfterLoad">If the pawn is spawning after a reload.</param>
        private static void CuteboldAdaptationSpawnSetupPostfix(Pawn __instance, Map map, bool respawningAfterLoad)
        {
            if (__instance.Dead || __instance?.def != Cutebold_Assemblies.CuteboldRaceDef || __instance.kindDef == null) return;

            if (__instance.health.hediffSet.GetFirstHediffOfDef(Cutebold_DefOf.CuteboldDarkAdaptation) == null)
            {
                Hediff_CuteboldDarkAdaptation hediff = HediffMaker.MakeHediff(Cutebold_DefOf.CuteboldDarkAdaptation, __instance) as Hediff_CuteboldDarkAdaptation;
                float minSeverity = 0f;
                float maxSeverity = 0.1f;
                bool goggles = false;

                foreach (Apparel apparel in __instance.apparel.WornApparel)
                {
                    if (apparel.def == Cutebold_DefOf.Cutebold_Goggles)
                    {
                        goggles = true;
                    }
                }

                if (!(__instance.story.traits.HasTrait(TraitDef.Named("Wimp")) && __instance.health.hediffSet.PainTotal > 0.0f) && __instance.health.hediffSet.PainTotal <= 0.5f && !respawningAfterLoad)
                {
#if RWPre1_4
                    if (__instance.story.childhood != null && Cutebold_Patch_Names.CuteboldUndergroundChildBackstories.Contains(__instance.story.childhood))
#else
                    if (__instance.story.Childhood != null && Cutebold_Patch_Names.CuteboldUndergroundChildBackstories.Contains(__instance.story.Childhood))
#endif
                    {
                        minSeverity += 0.05f;
                        maxSeverity += 0.15f;
                    }

#if RWPre1_4
                    if (__instance.story.adulthood != null && Cutebold_Patch_Names.CuteboldUndergroundAdultBackstories.Contains(__instance.story.adulthood))
#else
                    if (__instance.story.Adulthood != null && Cutebold_Patch_Names.CuteboldUndergroundAdultBackstories.Contains(__instance.story.Adulthood))
#endif
                    {
                        minSeverity += 0.20f;
                        maxSeverity += 0.30f;
                    }

                    if (__instance.story.traits.HasTrait(TraitDefOf.Undergrounder))
                    {
                        minSeverity += 0.10f;
                        maxSeverity += 0.20f;
                    }

                    if (__instance.story.traits.HasTrait(TraitDef.Named("Wimp")) && !goggles)
                    {
                        minSeverity = minSeverity > 0.5f ? 0.5f : minSeverity;
                        maxSeverity = maxSeverity > 0.7f ? 0.7f : maxSeverity;
                    }
                    else if (goggles)
                    {
                        minSeverity = minSeverity * 3f + 0.3f;
                        maxSeverity = maxSeverity * 3f + 0.4f;
                    }
                }

                hediff.Severity = new FloatRange(minSeverity, maxSeverity).RandomInRange;

                __instance.health.AddHediff(hediff);
            }

            Hediff_CuteboldDarkAdaptation darkAdaptation = __instance.health.hediffSet.GetFirstHediffOfDef(Cutebold_DefOf.CuteboldDarkAdaptation) as Hediff_CuteboldDarkAdaptation;
            darkAdaptation.UpdateGoggles();
            darkAdaptation.UpdateEyes();
        }

        /// <summary>
        /// Removes the dark adaptation hediff on cutebolds if they spawn with it.
        /// </summary>
        /// <param name="__instance">The pawn</param>
        private static void CuteboldNoAdaptationSpawnSetupPostfix(Pawn __instance)
        {
            if (__instance.Dead || __instance?.def != Cutebold_Assemblies.CuteboldRaceDef || __instance.kindDef == null) return;

            Hediff hediff = __instance.health.hediffSet.GetFirstHediffOfDef(Cutebold_DefOf.CuteboldDarkAdaptation);

            if (hediff != null)
            {
                __instance.health.RemoveHediff(hediff);
            }
        }

        /// <summary>
        /// Notifies the cutebold dark adaptation hediff that list of hediffs has changed.
        /// </summary>
        /// <param name="__instance">Instance of a pawn's hediff set.</param>
        private static void CuteboldHediffSetDirtyCachePostfix(HediffSet __instance)
        {
            if (__instance.pawn.def != Cutebold_Assemblies.CuteboldRaceDef) return;

            Hediff_CuteboldDarkAdaptation hediff = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(Cutebold_DefOf.CuteboldDarkAdaptation) as Hediff_CuteboldDarkAdaptation;

            hediff?.UpdateEyes();
        }

        /// <summary>
        /// Notifies the cutebold dark adaptation hediff that the apparel has changed.
        /// </summary>
        /// <param name="__instance"></param>
        private static void CuteboldApparelChangedPostfix(HediffSet __instance)
        {
            if (__instance.pawn.def != Cutebold_Assemblies.CuteboldRaceDef) return;

            Hediff_CuteboldDarkAdaptation hediff = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(Cutebold_DefOf.CuteboldDarkAdaptation) as Hediff_CuteboldDarkAdaptation;

            hediff?.UpdateGoggles();
        }

        #region 1.1 GoggleLayerTranspiler
#if RW1_1
        /// <summary>
        /// RW1.1
        /// Adjusts the layer offset for cutebold goggles so that they are drawn under other headgear.
        /// </summary>
        /// <param name="instructions">The instructions we are messing with.</param>
        /// <param name="ilGenerator">The IDGenerator that allows us to create local variables and labels.</param>
        /// <returns>All the code!</returns>
        private static IEnumerable<CodeInstruction> CuteboldGogglesFixTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
                {
                    FieldInfo hatInFront = AccessTools.Field(typeof(ApparelProperties), "hatRenderedFrontOfFace");
                    MethodInfo drawMeshNowOrLater = AccessTools.Method(typeof(GenDraw), "DrawMeshNowOrLater");

                    bool nextDraw = false;
                    float offset = 0.001f;
                    Label notGoggles = ilGenerator.DefineLabel();
                    LocalBuilder modified = ilGenerator.DeclareLocal(typeof(bool));

                    List<CodeInstruction> instructionList = instructions.ToList();

                    //
                    // See drSpy decompile of PawnRenderer.RenderPawnInternal() for variable references
                    // 
                    // Adjusts the y offset to put goggles below other headgear.
                    // 
                    // modified = false;
                    // 
                    // if (apparelGraphics[j].sourceApparel.def == Cutebold_DefOf.Cutebold_Goggles)
                    // {
                    //     loc2.y -= offset;
                    //     modified = true;
                    // }
                    //
                    List<CodeInstruction> checkForGoggles = [
                        new(OpCodes.Ldc_I4_0), // Load zero
                        new(OpCodes.Stloc_S, modified), // Set modified to zero (false)

                        new(OpCodes.Ldloc_S, 14), // Loads apparelGraphics
                        new(OpCodes.Ldloc_S, 15), // Loads j (apparel number)
                        new(OpCodes.Callvirt, AccessTools.Method(typeof(List<ApparelGraphicRecord>), "get_Item")), // Get the apparel graphic
                        new(OpCodes.Ldfld, AccessTools.Field(typeof(ApparelGraphicRecord), "sourceApparel")), // Get the apparel
                        new(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "def")), // Get the def of the apparel

                        new(OpCodes.Ldsfld, AccessTools.Field(typeof(Cutebold_DefOf), "Cutebold_Goggles")), // Load the def for cutebold goggles

                        new(OpCodes.Ceq), // Checks if the apparel are cutebold goggles
                        new(OpCodes.Brfalse, notGoggles), // If not, jump to regular execution

                            new(OpCodes.Ldloca_S, 11), // Loads loc2 (apparel drawing offset)
                            new(OpCodes.Ldflda, AccessTools.Field(typeof(Vector3), "y")), // Gets the address to loc2.y
                            new(OpCodes.Dup), // Copy it
                            new(OpCodes.Ldind_R4), // Load the loc2.y value
                            new(OpCodes.Ldc_R4, offset), // Loads the offset
                            new(OpCodes.Sub), // Subtract offset from loc2.y
                            new(OpCodes.Stind_R4), // Store new value at address of loc2.y

                            new(OpCodes.Ldc_I4_1), // Load one
                            new(OpCodes.Stloc_S, modified), // Set modified to one (true)

                    ];

                    //
                    // See drSpy decompile of PawnRenderer.RenderPawnInternal() for variable references
                    // 
                    // Reverts the y offset for other headgear.
                    // 
                    // if(modified)
                    // {
                    //     loc2.y += offset;
                    // }
                    //
                    List<CodeInstruction> revertChange =
                    [
                        new(OpCodes.Ldloc_S, modified), // Load modified
                        new(OpCodes.Brfalse, null), // Check if modified is false and if it is, jump to the end (null to be replaced)

                            new(OpCodes.Ldloca_S, 11), // Loads loc2 (apparel drawing offset)
                            new(OpCodes.Ldflda, AccessTools.Field(typeof(Vector3), "y")), // Gets the address to loc2.y
                            new(OpCodes.Dup), // Copy it
                            new(OpCodes.Ldind_R4), // Load the loc2.y value
                            new(OpCodes.Ldc_R4, offset), // Loads the offset
                            new(OpCodes.Add), // Subtract offset from loc2.y
                            new(OpCodes.Stind_R4) // Store new value at address of loc2.y
                    ];

                    int x = -1;

                    for (int i = 0; i < instructionList.Count; i++)
                    {
                        CodeInstruction instruction = instructionList[i];

                        if (i > 4 && instructionList[i - 4].OperandIs(hatInFront))
                        {
                            x = 30;

                            foreach (CodeInstruction codeInstruction in checkForGoggles)
                            {
                                //Log.Message("    +" + codeInstruction.ToString() + (codeInstruction.labels.Count > 0 ? codeInstruction.labels[0].ToString() : ""));
                                yield return codeInstruction;
                            }

                            instruction.labels.Add(notGoggles);
                            nextDraw = true;
                        }

                        if (nextDraw && instructionList[i - 1].OperandIs(drawMeshNowOrLater))
                        {
                            revertChange[1].operand = instruction.operand; // Sets the jump value

                            foreach (CodeInstruction codeInstruction in revertChange)
                            {
                               //Log.Message("    +" + codeInstruction.ToString() + (codeInstruction.labels.Count > 0 ? codeInstruction.labels[0].ToString() : ""));
                                yield return codeInstruction;
                            }

                            nextDraw = false;
                        }

                        if (x > 0)
                        {
                            //Log.Message("    "+instruction.ToString() + (instruction.labels.Count > 0 ? instruction.labels[0].ToString() : ""));
                            x--;
                        }

                        yield return instruction;
                    }
                }
#endif
        #endregion

        #region 1.2 Goggle Layer Transpiler
#if RW1_2
        /// <summary>
        /// RW1.1
        /// Adjusts the layer offset for cutebold goggles so that they are drawn under other headgear.
        /// </summary>
        /// <param name="instructions">The instructions we are messing with.</param>
        /// <param name="ilGenerator">The IDGenerator that allows us to create local variables and labels.</param>
        /// <returns>All the code!</returns>
        private static IEnumerable<CodeInstruction> CuteboldGogglesFixTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            FieldInfo hatInFront = AccessTools.Field(typeof(ApparelProperties), "hatRenderedFrontOfFace");
            MethodInfo drawMeshNowOrLater = AccessTools.Method(typeof(GenDraw), "DrawMeshNowOrLater");
            FieldInfo gogglesDef = AccessTools.Field(typeof(Cutebold_DefOf), "Cutebold_Goggles");

            bool nextDraw = false;
            float offset = 0.001f;
            Label notGoggles = ilGenerator.DefineLabel();
            LocalBuilder modified = ilGenerator.DeclareLocal(typeof(bool));

            List<CodeInstruction> instructionList = instructions.ToList();

            //
            // See drSpy decompile of PawnRenderer.RenderPawnInternal() for variable references
            // 
            // Adjusts the y offset to put goggles below other headgear.
            // 
            // modified = false;
            // 
            // if (apparelGraphics[j].sourceApparel.def == Cutebold_DefOf.Cutebold_Goggles)
            // {
            //     loc2.y -= offset;
            //     modified = true;
            // }
            //
            List<CodeInstruction> checkForGoggles = [
                        new(OpCodes.Ldc_I4_0), // Load zero
                        new(OpCodes.Stloc_S, modified), // Set modified to zero (false)

                        new(OpCodes.Ldloc_S, 5), // Loads apparelGraphics
                        new(OpCodes.Ldloc_S, 16), // Loads j (apparel number)
                        new(OpCodes.Callvirt, AccessTools.Method(typeof(List<ApparelGraphicRecord>), "get_Item")), // Get the apparel graphic
                        new(OpCodes.Ldfld, AccessTools.Field(typeof(ApparelGraphicRecord), "sourceApparel")), // Get the apparel
                        new(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "def")), // Get the def of the apparel

                        new(OpCodes.Ldsfld, gogglesDef), // Load the def for cutebold goggles

                        new(OpCodes.Ceq), // Checks if the apparel are cutebold goggles
                        new(OpCodes.Brfalse, notGoggles), // If not, jump to regular execution

                            new(OpCodes.Ldloca_S, 13), // Loads loc2 (apparel drawing offset)
                            new(OpCodes.Ldflda, AccessTools.Field(typeof(Vector3), "y")), // Gets the address to loc2.y
                            new(OpCodes.Dup), // Copy it
                            new(OpCodes.Ldind_R4), // Load the loc2.y value
                            new(OpCodes.Ldc_R4, offset), // Loads the offset
                            new(OpCodes.Sub), // Subtract offset from loc2.y
                            new(OpCodes.Stind_R4), // Store new value at address of loc2.y

                            new(OpCodes.Ldc_I4_1), // Load one
                            new(OpCodes.Stloc_S, modified), // Set modified to one (true)

                    ];

            //
            // See drSpy decompile of PawnRenderer.RenderPawnInternal() for variable references
            // 
            // Reverts the y offset for other headgear.
            // 
            // if(modified)
            // {
            //     loc2.y += offset;
            // }
            //
            List<CodeInstruction> revertChange =
                    [
                        new(OpCodes.Ldloc_S, modified), // Load modified
                        new(OpCodes.Brfalse, null), // Check if modified is false and if it is, jump to the end (null to be replaced)

                            new(OpCodes.Ldloca_S, 13), // Loads loc2 (apparel drawing offset)
                            new(OpCodes.Ldflda, AccessTools.Field(typeof(Vector3), "y")), // Gets the address to loc2.y
                            new(OpCodes.Dup), // Copy it
                            new(OpCodes.Ldind_R4), // Load the loc2.y value
                            new(OpCodes.Ldc_R4, offset), // Loads the offset
                            new(OpCodes.Add), // Subtract offset from loc2.y
                            new(OpCodes.Stind_R4) // Store new value at address of loc2.y
                    ];

            //int x = -1;

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (i > 4 && instructionList[i - 4].OperandIs(hatInFront))
                {
                    //x = 30;

                    foreach (CodeInstruction codeInstruction in checkForGoggles)
                    {
                        //Log.Message("    +" + codeInstruction.ToString() + (codeInstruction.labels.Count > 0 ? codeInstruction.labels[0].ToString() : ""));
                        yield return codeInstruction;
                    }

                    instruction.labels.Add(notGoggles);
                    nextDraw = true;
                }

                if (nextDraw && instructionList[i - 1].OperandIs(drawMeshNowOrLater))
                {
                    revertChange[1].operand = instruction.operand; // Sets the jump value

                    foreach (CodeInstruction codeInstruction in revertChange)
                    {
                        //Log.Message("    +" + codeInstruction.ToString() + (codeInstruction.labels.Count > 0 ? codeInstruction.labels[0].ToString() : ""));
                        yield return codeInstruction;
                    }

                    nextDraw = false;
                }

                /*if (x > 0)
                {
                    Log.Message("    "+instruction.ToString() + (instruction.labels.Count > 0 ? instruction.labels[0].ToString() : ""));
                    x--;
                }*/

                yield return instruction;
            }
        }
#endif
        #endregion
    }
}
