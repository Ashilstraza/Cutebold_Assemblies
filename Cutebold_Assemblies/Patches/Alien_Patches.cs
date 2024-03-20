#if !RWPre1_4
using AlienRace;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;

namespace Cutebold_Assemblies.Patches
{
    /// <summary>
    /// Set of patches for all Alien races, provided by the Cutebold mod author
    /// </summary>
    public class Alien_Patches
    {
        public static List<string> RaceList { get; private set; } = new List<string>();

        public Alien_Patches(Harmony harmony)
        {
            Type thisClass = typeof(Alien_Patches);

            string alienRaceID = "rimworld.erdelf.alien_race.main";

            StringBuilder stringBuilder = new StringBuilder("Cutebold Mod Provided Alien Patches:");

            if (!Harmony.GetPatchInfo(AccessTools.Method(typeof(IncidentWorker_Disease), "CanAddHediffToAnyPartOfDef"))?.Transpilers?.Any(patch => patch.owner == alienRaceID) ?? true)
            {
                stringBuilder.AppendLine("  Patching IncidentWorker_Disease.CanAddHediffToAnyPartOfDef");
                harmony.Patch(AccessTools.Method(typeof(IncidentWorker_Disease), "CanAddHediffToAnyPartOfDef"), transpiler: new HarmonyMethod(thisClass, nameof(Alien_FixBaby_Transpiler)));
            }

            if (!Harmony.GetPatchInfo(AccessTools.Method(typeof(ITab_Pawn_Gear), "get_IsVisible"))?.Transpilers?.Any(patch => patch.owner == alienRaceID) ?? true)
            {
                stringBuilder.AppendLine("  Patching ITab_Pawn_Gear.get_IsVisible");
                harmony.Patch(AccessTools.Method(typeof(ITab_Pawn_Gear), "get_IsVisible"), transpiler: new HarmonyMethod(thisClass, nameof(Alien_FixBaby_Transpiler)));
            }

            if (!Harmony.GetPatchInfo(AccessTools.Method(typeof(Pawn_IdeoTracker), "get_CertaintyChangeFactor"))?.Transpilers?.Any(patch => !patch.owner.NullOrEmpty()) ?? true)
            {
                stringBuilder.AppendLine("  Patching Pawn_IdeoTracker.get_CertaintyChangeFactor");
                harmony.Patch(AccessTools.Method(typeof(Pawn_IdeoTracker), "get_CertaintyChangeFactor"), transpiler: new HarmonyMethod(thisClass, nameof(Alien_CertaintyChangeFactor_Transpiler)));
            }

            if (!Harmony.GetPatchInfo(AccessTools.Method(typeof(HediffGiver), nameof(HediffGiver.TryApply)))?.Transpilers?.Any(patch => patch.owner == alienRaceID) ?? true)
            {
                stringBuilder.AppendLine("  Patching HediffGiver.TryApply");
                harmony.Patch(AccessTools.Method(typeof(HediffGiver), nameof(HediffGiver.TryApply)), transpiler: new HarmonyMethod(thisClass, nameof(Alien_FixBaby_Transpiler)));
            }

            //harmony.Patch(AccessTools.Method(typeof(AlienPawnRenderNodeWorker_BodyAddon), nameof(AlienPawnRenderNodeWorker_BodyAddon.ScaleFor)), prefix: new HarmonyMethod(thisClass, nameof(Alien_ScaleFor_Prefix)));

#if RW1_4
            // If Dub's Bad Hygene isn't enabled, don't try to patch that which is not there.
            if (ModLister.GetActiveModWithIdentifier("Dubwise.DubsBadHygiene") != null && Cutebold_Assemblies.CuteboldSettings.DBH_Patches)
            {
                stringBuilder.AppendLine("  Patching Dub's Bad Hygine");

                try
                {
                    new DBHPatches(harmony, thisClass);
                }
                catch (Exception e)
                {
                    Log.Error($"{Cutebold_Assemblies.ModName}: Exception when trying to apply DBH Patches. Please notify the author for the cutebold mod with the logs. Thanks!\n{e}");
                }

            }
#endif
            if (!stringBuilder.ToString().Equals("Cutebold Mod Provided Alien Patches:")) Log.Message(stringBuilder.ToString().TrimEndNewlines());

#if DEBUG && RW1_4   // Personal patches
            PersonalPatches(harmony, thisClass);
#endif
        }

        /*public static bool Alien_ScaleFor_Prefix(AlienPawnRenderNodeWorker_BodyAddon __instance, PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
        {
            AlienPawnRenderNodeProperties_BodyAddon props = AlienPawnRenderNodeWorker_BodyAddon.PropsFromNode(node);
            AlienPartGenerator.BodyAddon ba = props.addon;
            
            Vector2 scale = (parms.Portrait && ba.drawSizePortrait != Vector2.zero ? ba.drawSizePortrait : ba.drawSize) *
                            (ba.scaleWithPawnDrawsize ?
                                 (ba.alignWithHead ?
                                      parms.Portrait ?
                                          props.alienComp.customPortraitHeadDrawSize :
                                          props.alienComp.customHeadDrawSize :
                                      parms.Portrait ?
                                          props.alienComp.customPortraitDrawSize :
                                          props.alienComp.customDrawSize) *
                                 (ModsConfig.BiotechActive ? parms.pawn.ageTracker.CurLifeStage.bodyWidth ?? 1.5f : 1.5f) :
                                 Vector2.one * 1.5f);
            
            Vector2 addonDrawSize;
            Vector2 pawnBodyDrawSize;
            Vector2 pawnHeadDrawSize;

            if (parms.Portrait)
            {
                addonDrawSize = ba.drawSizePortrait != Vector2.zero ? ba.drawSizePortrait : ba.drawSize;
                pawnBodyDrawSize = props.alienComp.customPortraitDrawSize;
                pawnHeadDrawSize = props.alienComp.customPortraitHeadDrawSize;
            }
            else
            {
                addonDrawSize = ba.drawSize;
                pawnBodyDrawSize = props.alienComp.customDrawSize;
                pawnHeadDrawSize = props.alienComp.customHeadDrawSize;
            }
            if (ba.scaleWithPawnDrawsize)
            {
                if (ModsConfig.BiotechActive)
                {
                    pawnBodyDrawSize *= parms.pawn.ageTracker.CurLifeStage.bodyWidth ?? 1.5f;
                    pawnHeadDrawSize *= Vector2.one * 0.76f;
                }
                else
                {
                    pawnHeadDrawSize *= 1.5f;
                    pawnBodyDrawSize *= 1.5f;
                }
                addonDrawSize *= ba.alignWithHead ? pawnHeadDrawSize : pawnBodyDrawSize;
            }
            else
            {
                addonDrawSize *= Vector2.one * 1.5f;
            }

            __result = new Vector3(addonDrawSize.x, 1f, addonDrawSize.y);
            return false;
        }*/

        /// <summary>
        /// Replaces instances of Pawn.ageTracker.CurLifeStage == LifeStageDefOf.HumanlikeBaby with Pawn.ageTracker.CurLifeStage.developmentalStage.Baby()
        /// </summary>
        /// <param name="instructions">The instructions we are messing with.</param>
        /// <param name="ilGenerator">The IDGenerator that allows us to create local variables and labels.</param>
        /// <returns>The fixed code.</returns>
        public static IEnumerable<CodeInstruction> Alien_FixBaby_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo humanLikeBaby = AccessTools.Field(typeof(LifeStageDefOf), "HumanlikeBaby");
            FieldInfo developmentalStage = AccessTools.Field(typeof(LifeStageDef), "developmentalStage");
            MethodInfo baby = AccessTools.Method(typeof(DevelopmentalStageExtensions), "Baby");

            List<CodeInstruction> instructionList = instructions.ToList();
            int instructionListCount = instructionList.Count;


            /*
             *  
             * Replaces == LifeStageDefOf.HumanlikeBaby with .developmentalStage.Baby()
             * 
             */
            List<CodeInstruction> babyFix = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldfld, developmentalStage), // Load developmentStage
                new CodeInstruction(OpCodes.Call, baby), // Call Baby() on the loaded development stage
                new CodeInstruction(OpCodes.Brtrue, null) // Branches if true, Operand label to be replaced on runtime
            };

            for (int i = 0; i < instructionListCount; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (i < instructionListCount && instructionList[i].Is(OpCodes.Ldsfld, humanLikeBaby))
                {
                    foreach (CodeInstruction codeInstruction in babyFix)
                    {
                        if (codeInstruction.opcode == OpCodes.Brtrue)
                        {
                            CodeInstruction tempInstruction = new CodeInstruction(codeInstruction);
                            // Check if the branch instruction is branching when not equal
                            if (instructionList[i + 1].opcode == OpCodes.Bne_Un_S)
                            {
                                tempInstruction.opcode = OpCodes.Brfalse;
                            }

                            tempInstruction.operand = instructionList[i + 1].operand; // Grab branch label

                            yield return tempInstruction;
                        }
                        else yield return codeInstruction;
                    }

                    i += 2;
                    instruction = instructionList[i];
                }

                yield return instruction;
            }
        }

        /// <summary>
        /// Destructively replaces the SimpleCurve for the CertaintyChangeFactor
        /// </summary>
        /// <param name="instructions">The instructions we are messing with.</param>
        /// <param name="ilGenerator">The IDGenerator that allows us to create local variables and labels.</param>
        /// <returns>The code that is usable.</returns>
        public static IEnumerable<CodeInstruction> Alien_CertaintyChangeFactor_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo alienCertaintyChangeFactor = AccessTools.Method(typeof(Alien_Patches), nameof(Alien_Patches.CertaintyCurve));
            string x = Traverse.Create(typeof(Pawn_IdeoTracker)).Fields()[0];
            FieldInfo pawn = AccessTools.Field(typeof(Pawn_IdeoTracker), "pawn");

            List<CodeInstruction> instructionList = instructions.ToList();
            int instructionListCount = instructionList.Count;

            /*
             * See drSpy decompile of PawnIdeo_Tracker.get_CertaintyChangeFactor() for variable references 
             */
            List<CodeInstruction> racialCertainty = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_0), // Load this
                new CodeInstruction(OpCodes.Ldfld, pawn), // Load this.pawn
                new CodeInstruction(OpCodes.Call, alienCertaintyChangeFactor), // Call Alien_CertaintyChangeFactor(pawn)
                new CodeInstruction(OpCodes.Ret), // Returns the adjusted float from the previous call
            };

            for (int i = 0; i < instructionListCount; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (i + 1 < instructionListCount && instructionList[i].opcode == OpCodes.Ldsfld)
                {

                    racialCertainty[0] = racialCertainty[0].MoveLabelsFrom(instructionList[i]);
                    foreach (CodeInstruction codeInstruction in racialCertainty)
                    {
                        yield return codeInstruction;
                    }
                    break;
                }

                yield return instruction;
            }
        }

        /// <summary>
        /// Racial ideology certainty curves
        /// </summary>
        private static readonly Dictionary<ThingDef, SimpleCurve> ideoCurves = new Dictionary<ThingDef, SimpleCurve>();

        /// <summary>
        /// Creates a certainty curve for each race. Ideology does not matter, just the pawn's child and adult ages.
        /// </summary>
        /// <param name="pawn">Pawn to check the ideology certainty curve of.</param>
        /// <returns>Float between the values of 2 and 1 depending on how close to an adult the pawn is.</returns>
        public static float CertaintyCurve(Pawn pawn)
        {
            if (pawn == null) return 1f;

            if (!ideoCurves.ContainsKey(pawn.def))
            {
                float child = -1;
                float adult = -1;

                foreach (LifeStageAge lifeStage in pawn.RaceProps.lifeStageAges)
                {
                    if (child == -1 && lifeStage.def.developmentalStage.Child()) child = lifeStage.minAge; //Just want the first lifeStage where a pawn is a child
                    if (lifeStage.def.developmentalStage.Adult()) adult = lifeStage.minAge; //Want last life stage to match how Humans work. Does not take into account a life stage like "elder" if a custom race would have that.
                }

                //If for some reason a stage is not found, set the ages to be extremely low.
                if (child == -1) child = 0;
                if (adult < child) adult = child + 0.001f; //Add 0.001 to the child to prevent an inverted curve if the adult age is less than the child age.

                ideoCurves.Add(pawn.def, new SimpleCurve
                    {
                        new CurvePoint(child, 2f),
                        new CurvePoint(adult, 1f)
                    });
            }

            return ideoCurves[pawn.def].Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
        }

        #region Personal Patches
#if DEBUG && RW1_4
        /// <summary>
        /// Personal Patches for various annoyances/bugs
        /// </summary>
        private static void PersonalPatches(Harmony harmony, Type thisClass)
        {
            if (ModLister.GetActiveModWithIdentifier("Mlie.SomeThingsFloat") != null)
            {
                new SomeThingsFloat(harmony);
            }

            if (ModLister.GetActiveModWithIdentifier("Mlie.RFRumorHasIt") != null)
            {
                new RFRumorHasIt(harmony);
            }

            if (ModLister.GetActiveModWithIdentifier("babylettuce.growingzone") != null)
            {
                new GZP(harmony);
            }

            // faster baby feeding, maybe new mod?
            harmony.Patch(AccessTools.GetDeclaredMethods(typeof(JobDriver_BottleFeedBaby)).ElementAt(13), transpiler: new HarmonyMethod(thisClass, nameof(FasterFeeding_Transpiler)));
            harmony.Patch(AccessTools.Method(typeof(ChildcareUtility), "SuckleFromLactatingPawn"), transpiler: new HarmonyMethod(thisClass, nameof(FasterFeeding_Transpiler)));
        }

   // Personal patches
        public static IEnumerable<CodeInstruction> FasterFeeding_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            int instructionListCount = instructionList.Count;

            for (int i = 0; i < instructionListCount; i++)
            {


                if (instructionList[i].Is(OpCodes.Ldc_R4, 5000f))
                {
                    instructionList[i].operand = 1250f;
                }
                yield return instructionList[i];
            }
        }
#endif
        #endregion
    }
}
#endif