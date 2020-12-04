using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace Cutebold_Assemblies
{
    class Cutebold_Patch_HediffRelated
    {
        public Cutebold_Patch_HediffRelated(Harmony harmony, Cutebold_Settings settings)
        {
            if (settings.eyeAdaptation)
            {
                // Allows for dark adaptation, obviously not cave adaptation since that is a different game with cute kobolds.
                harmony.Patch(AccessTools.Method(typeof(StatPart_Glow), "FactorFromGlow", null, null), null, new HarmonyMethod(typeof(Cutebold_Patch_HediffRelated), "CuteboldFactorFromGlowPostfix", null), null, null);
                // Applies dark adaptation to all cutebolds as they spawn.               
                harmony.Patch(AccessTools.Method(typeof(Pawn), "SpawnSetup", null, null), null, new HarmonyMethod(typeof(Cutebold_Patch_HediffRelated), "CuteboldAdaptationSpawnSetupPostfix", null), null, null);
                // Update dark adaptation eye references.
                harmony.Patch(AccessTools.Method(typeof(HediffSet), "DirtyCache", null, null), null, new HarmonyMethod(typeof(Cutebold_Patch_HediffRelated), "CuteboldHediffSetDirtyCachePostfix", null), null, null);
                // Update dark adaptation goggle references.
                harmony.Patch(AccessTools.Method(typeof(Pawn_ApparelTracker), "ApparelChanged", null, null), null, new HarmonyMethod(typeof(Cutebold_Patch_HediffRelated), "CuteboldApparelChangedPostfix", null), null, null);
            }
            else
            {
                // Removes dark adaptation to all cutebolds as they spawn in.
                harmony.Patch(AccessTools.Method(typeof(Pawn), "SpawnSetup", null, null), null, new HarmonyMethod(typeof(Cutebold_Patch_Stats), "CuteboldNoAdaptationSpawnSetupPostfix", null), null, null);
            }

            // Adjust layer offset for cutebold goggles.
            harmony.Patch(AccessTools.Method(typeof(PawnRenderer), "RenderPawnInternal", new[] { 
                    typeof(Vector3), 
                    typeof(float), 
                    typeof(bool), 
                    typeof(Rot4), 
                    typeof(Rot4), 
                    typeof(RotDrawMode), 
                    typeof(bool), 
                    typeof(bool), typeof(bool) 
                }), null, null, new HarmonyMethod(typeof(Cutebold_Patch_HediffRelated), "CuteboldRenderPawnInternalTranspiler", null), null);
        }

        /// <summary>
        /// Overrides the regular glow curve for cutebolds to allow for dark adaptation.
        /// </summary>
        /// <param name="__result">The previous glow curve result.</param>
        /// <param name="t">The thing that is being evaluated.</param>
        private static void CuteboldFactorFromGlowPostfix(ref float __result, Thing t)
        {
            if (t.def.defName == Cutebold_Assemblies.RaceName)
            {
                Hediff_CuteboldDarkAdaptation hediff = (Hediff_CuteboldDarkAdaptation)((Pawn)t).health.hediffSet.GetFirstHediffOfDef(Cutebold_DefOf.CuteboldDarkAdaptation);
                if (hediff != null) __result = hediff.GlowCurve.Evaluate(t.Map.glowGrid.GameGlowAt(t.Position));
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
            if (__instance.Dead || __instance.def == null || __instance.def.defName != Cutebold_Assemblies.RaceName || __instance.kindDef == null) return;

            if (__instance.health.hediffSet.GetFirstHediffOfDef(Cutebold_DefOf.CuteboldDarkAdaptation) == null)
            {
                Hediff_CuteboldDarkAdaptation hediff = (Hediff_CuteboldDarkAdaptation)HediffMaker.MakeHediff(Cutebold_DefOf.CuteboldDarkAdaptation, __instance);
                float minSeverity = 0f;
                float maxSeverity = 0.1f;

                if (!(__instance.story.traits.HasTrait(TraitDef.Named("Wimp")) && __instance.health.hediffSet.PainTotal > 0.0f) && __instance.health.hediffSet.PainTotal <= 0.5f && !respawningAfterLoad)
                {
                    if (__instance.story.childhood != null && Cutebold_Patch_Names.CuteboldUndergroundChildBackstories.Contains(__instance.story.childhood))
                    {
                        minSeverity += 0.05f;
                        maxSeverity += 0.15f;
                    }

                    if (__instance.story.adulthood != null && Cutebold_Patch_Names.CuteboldUndergroundAdultBackstories.Contains(__instance.story.childhood))
                    {
                        minSeverity += 0.20f;
                        maxSeverity += 0.30f;
                    }

                    if (__instance.story.traits.HasTrait(TraitDefOf.Undergrounder))
                    {
                        minSeverity += 0.10f;
                        maxSeverity += 0.20f;
                    }

                    if (__instance.story.traits.HasTrait(TraitDef.Named("Wimp")))
                    {
                        minSeverity = minSeverity > 0.5f ? 0.5f : minSeverity;
                        maxSeverity = maxSeverity > 0.7f ? 0.7f : maxSeverity;
                    }
                }

                hediff.Severity = new FloatRange(minSeverity, maxSeverity).RandomInRange;

                __instance.health.AddHediff(hediff);
            }
        }

        /// <summary>
        /// Removes the dark adaptation hediff on cutebolds if they spawn with it.
        /// </summary>
        /// <param name="__instance">The pawn</param>
        /// <param name="map">The map they are located on. (unused)</param>
        /// <param name="respawningAfterLoad">If the pawn is spawning after a reload. (unused)</param>
        private static void CuteboldNoAdaptationSpawnSetupPostfix(Pawn __instance, Map map, bool respawningAfterLoad)
        {
            if (__instance.Dead || __instance.def?.defName != Cutebold_Assemblies.RaceName || __instance.kindDef == null) return;
            //if (__instance.Dead || __instance.def == null || __instance.def.defName != Cutebold_Assemblies.RaceName || __instance.kindDef == null) return;

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
            if (__instance.pawn.def.defName != Cutebold_Assemblies.RaceName) return;

            var hediff = (Hediff_CuteboldDarkAdaptation)__instance.pawn.health.hediffSet.GetFirstHediffOfDef(Cutebold_DefOf.CuteboldDarkAdaptation);

            if (hediff != null) hediff.UpdateEyes();
        }

        /// <summary>
        /// Notifies the cutebold dark adaptation hediff that the apparel has changed.
        /// </summary>
        /// <param name="__instance"></param>
        private static void CuteboldApparelChangedPostfix(HediffSet __instance)
        {
            if (__instance.pawn.def.defName != Cutebold_Assemblies.RaceName) return;

            var hediff = (Hediff_CuteboldDarkAdaptation)__instance.pawn.health.hediffSet.GetFirstHediffOfDef(Cutebold_DefOf.CuteboldDarkAdaptation);

            if (hediff != null) hediff.UpdateGoggles();
        }

        /// <summary>
        /// Adjusts the layer offset for cutebold goggles so that they are drawn under other headgear.
        /// </summary>
        /// <param name="instructions">The instructions we are messing with.</param>
        /// <param name="ilGenerator">The IDGenerator that allows us to create local variables and labels.</param>
        /// <returns>All the code!</returns>
        private static IEnumerable<CodeInstruction> CuteboldRenderPawnInternalTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            FieldInfo hatInFront = AccessTools.Field(typeof(ApparelProperties), "hatRenderedFrontOfFace");
            MethodInfo drawMeshNowOrLater = AccessTools.Method(typeof(GenDraw), "DrawMeshNowOrLater");

            bool nextDraw = false;
            float offset = 0.001f;
            Label notGoggles = ilGenerator.DefineLabel();
            LocalBuilder modified = ilGenerator.DeclareLocal(typeof(bool));

            List<CodeInstruction> instructionList = instructions.ToList();

            /*
             * See drSpy decompile of PawnRenderer.RenderPawnInternal() for variable references
             * 
             * modified = false;
             * 
             * if (apparelGraphics[j].sourceApparel.def == Cutebold_DefOf.Cutebold_Goggles)
             * {
             *     loc2.y -= offset;
             *     modified = true;
             * }
             */
            List<CodeInstruction> checkForGoggles = new List<CodeInstruction>() {
                new CodeInstruction(OpCodes.Ldc_I4_0), // Load zero
                new CodeInstruction(OpCodes.Stloc_S, modified), // Set modified to zero (false)

                new CodeInstruction(OpCodes.Ldloc_S, 5), // Loads apparelGraphics
                new CodeInstruction(OpCodes.Ldloc_S, 16), // Loads j (apparel number)
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<ApparelGraphicRecord>), "get_Item")), // Get the apparel graphic
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ApparelGraphicRecord), "sourceApparel")), // Get the apparel
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "def")), // Get the def of the apparel

                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Cutebold_DefOf), "Cutebold_Goggles")), // Load the def for cutebold goggles

                new CodeInstruction(OpCodes.Ceq), // Checks if the apparel are cutebold goggles
                new CodeInstruction(OpCodes.Brfalse, notGoggles), // If not, jump to regular execution
                
                    new CodeInstruction(OpCodes.Ldloca_S, 13), // Loads loc2 (apparel drawing offset)
                    new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(Vector3), "y")), // Gets the address to loc2.y
                    new CodeInstruction(OpCodes.Dup), // Copy it
                    new CodeInstruction(OpCodes.Ldind_R4), // Load the loc2.y value
                    new CodeInstruction(OpCodes.Ldc_R4, offset), // Loads the offset
                    new CodeInstruction(OpCodes.Sub), // Subtract offset from loc2.y
                    new CodeInstruction(OpCodes.Stind_R4), // Store new value at address of loc2.y
                
                    new CodeInstruction(OpCodes.Ldc_I4_1), // Load one
                    new CodeInstruction(OpCodes.Stloc_S, modified), // Set modified to one (true)

            };

            /*
             * See drSpy decompile of PawnRenderer.RenderPawnInternal() for variable references
             * 
             * if(modified)
             * {
             *     loc2.y += offset;
             * }
             */
            List<CodeInstruction> revertChange = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldloc_S, modified), // Load modified
                new CodeInstruction(OpCodes.Brfalse, null), // Check if modified is false and if it is, jump to the end (null to be replaced)

                    new CodeInstruction(OpCodes.Ldloca_S, 13), // Loads loc2 (apparel drawing offset)
                    new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(Vector3), "y")), // Gets the address to loc2.y
                    new CodeInstruction(OpCodes.Dup), // Copy it
                    new CodeInstruction(OpCodes.Ldind_R4), // Load the loc2.y value
                    new CodeInstruction(OpCodes.Ldc_R4, offset), // Loads the offset
                    new CodeInstruction(OpCodes.Add), // Subtract offset from loc2.y
                    new CodeInstruction(OpCodes.Stind_R4) // Store new value at address of loc2.y
            };

            //int x = -1;

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (i > 4 && instructionList[i - 4].OperandIs(hatInFront))
                {
                    //x = 30;

                    foreach(CodeInstruction codeInstruction in checkForGoggles)
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

                    foreach(CodeInstruction codeInstruction in revertChange)
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
    }
}
