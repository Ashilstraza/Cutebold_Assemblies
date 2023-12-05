using AlienRace;
using DubsBadHygiene;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Verse;
using Verse.AI;

#if !RWPre1_4
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
            var thisClass = typeof(Alien_Patches);

            var incidentWorker_Disease_CanAddHediffToAnyPartOfDef = AccessTools.Method(typeof(IncidentWorker_Disease), "CanAddHediffToAnyPartOfDef");
            var iTab_Pawn_Gear_IsVisible = AccessTools.Method(typeof(ITab_Pawn_Gear), "get_IsVisible");
            var pawn_IdeoTracker_CertaintyChangeFactor = AccessTools.Method(typeof(Pawn_IdeoTracker), "get_CertaintyChangeFactor");
            var hediffGiver_TryApply = AccessTools.Method(typeof(HediffGiver), "TryApply");

            Alien_RacesToPatch();

            string alienRaceID = "rimworld.erdelf.alien_race.main";
            
            StringBuilder stringBuilder = new StringBuilder("Cutebold Mod Provided Alien Patches:");
            bool patches = false;

            if (!Harmony.GetPatchInfo(AccessTools.Method(typeof(IncidentWorker_Disease), "CanAddHediffToAnyPartOfDef"))?.Prefixes?.Any(patch => patch.owner == alienRaceID) ?? true)
            {
                stringBuilder.AppendLine("  Patching IncidentWorker_Disease.CanAddHediffToAnyPartOfDef");
                harmony.Patch(incidentWorker_Disease_CanAddHediffToAnyPartOfDef, prefix: new HarmonyMethod(thisClass, "Alien_CanAddHediffToAnyPartOfDef_Prefix"));
                patches = true;
            }

            if (!Harmony.GetPatchInfo(AccessTools.Method(typeof(ITab_Pawn_Gear), "get_IsVisible"))?.Prefixes?.Any(patch => patch.owner == alienRaceID) ?? true)
            {
                stringBuilder.AppendLine("  Patching ITab_Pawn_Gear.get_IsVisible");
                harmony.Patch(iTab_Pawn_Gear_IsVisible, prefix: new HarmonyMethod(thisClass, "Alien_get_IsVisible_Prefix"));
                patches = true;
            }

            if (!Harmony.GetPatchInfo(AccessTools.Method(typeof(Pawn_IdeoTracker), "get_CertaintyChangeFactor"))?.Transpilers?.Any(patch => !patch.owner.NullOrEmpty()) ?? true)
            {
                stringBuilder.AppendLine("  Patching Pawn_IdeoTracker.get_CertaintyChangeFactor");
                harmony.Patch(pawn_IdeoTracker_CertaintyChangeFactor, transpiler: new HarmonyMethod(thisClass, "Alien_CertaintyChangeFactor_Transpiler"));
                patches = true;
            }

            if (!Harmony.GetPatchInfo(AccessTools.Method(typeof(HediffGiver), "TryApply"))?.Prefixes?.Any(patch => patch.owner == alienRaceID) ?? true)
            {
                stringBuilder.AppendLine("  Patching HediffGiver.TryApply");
                harmony.Patch(hediffGiver_TryApply, prefix: new HarmonyMethod(thisClass, "Alien_TryApply_Prefix"));
                patches = true;
            }

            if (patches) Log.Message(stringBuilder.ToString().TrimEndNewlines());
            
            // If Dub's Bad Hygene isn't enabled, don't try to patch that which is not there.
            if(ModLister.GetActiveModWithIdentifier("Dubwise.DubsBadHygiene") != null && Cutebold_Assemblies.CuteboldSettings.DBH_Patches)
            {
                harmony.Patch(AccessTools.Method(typeof(NeedsUtil), "ShouldHaveNeed"), transpiler: new HarmonyMethod(thisClass, "Alien_FixBaby_Transpiler"));
                harmony.Patch(AccessTools.Method(typeof(WorkGiver_washPatient), "ShouldBeWashed"), transpiler: new HarmonyMethod(thisClass, "Alien_FixBaby_Transpiler"));
                harmony.Patch(AccessTools.Method(typeof(WorkGiver_washChild), "ShouldBeWashed"), transpiler: new HarmonyMethod(thisClass, "Alien_FixBaby_Transpiler"));
                harmony.Patch(AccessTools.Method(typeof(WorkGiver_washChild), "PotentialWorkThingsGlobal"), transpiler: new HarmonyMethod(thisClass, "Alien_FixBaby_Transpiler"));
                harmony.Patch(AccessTools.Method(typeof(Thing), "Ingested"), postfix: new HarmonyMethod(thisClass, "Alien_DBH_IngestedPostfix"));
                harmony.Patch(AccessTools.Method(typeof(ChildcareUtility), "SuckleFromLactatingPawn"),
                    prefix: new HarmonyMethod(thisClass, "Alien_DBH_SuckleFromLactatingPawn_Prefix"),
                    postfix: new HarmonyMethod(thisClass, "Alien_DBH_SuckleFromLactatingPawn_Postfix"));
            }
        }

        /// <summary>
        /// Replaces instances of Pawn.ageTracker.CurLifeStage == LifeStageDefOf.HumanlikeBaby with Pawn.ageTracker.CurLifeStage.developmentalStage.Baby()
        /// </summary>
        /// <param name="instructions">The instructions we are messing with.</param>
        /// <param name="ilGenerator">The IDGenerator that allows us to create local variables and labels.</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> Alien_FixBaby_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
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
                        if(codeInstruction.opcode == OpCodes.Brtrue)
                        {
                            CodeInstruction tempInstruction = new CodeInstruction(codeInstruction);
                            // Check if the branch instruction is branching when not equal
                            if (instructionList[i+1].opcode == OpCodes.Bne_Un_S)
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
        /// Allow for baby suckling to satisfy their thirst need (unused by DBH currently)
        /// </summary>
        /// <param name="baby">baby being fed</param>
        /// <param name="feeder">pawn feeding baby (unused)</param>
        /// <param name="__state">food level before suckle</param>
        public static void Alien_DBH_SuckleFromLactatingPawn_Prefix(Pawn baby, Pawn feeder, out float __state)
        {
            __state = baby.needs.food.CurLevel;
        }

        /// <summary>
        /// Allow for baby suckling to satisfy their thirst need (unused by DBH currently)
        /// </summary>
        /// <param name="baby">baby being fed</param>
        /// <param name="feeder">pawn feeding baby (unused)</param>
        /// <param name="__state">food level before suckle</param>
        public static void Alien_DBH_SuckleFromLactatingPawn_Postfix(Pawn baby, Pawn feeder, ref float __state)
        {
            var nutritionPercent = baby.needs.food.MaxLevel / (baby.needs.food.CurLevel - __state);
            baby.needs.TryGetNeed<Need_Thirst>()?.Drink(nutritionPercent+.01f);
        }

        /// <summary>
        /// Dirty patch to allow babyfood to satisfy the thirst of a baby without doing an xml patch. (unused by DBH currently)
        /// </summary>
        /// <param name="__instance">Item being fed</param>
        /// <param name="ingester">Pawn being fed</param>
        /// <param name="__result">Nutrition value</param>
        public static void Alien_DBH_IngestedPostfix(Thing __instance, Pawn ingester, ref float __result)
        {
            if(__instance != null && ingester != null)
            {
                if(ingester.ageTracker.CurLifeStage.developmentalStage.Baby() && __instance.def.defName.Equals("BabyFood")){
                    ingester.needs.TryGetNeed<Need_Thirst>()?.Drink(__result);
                }
            }
        }

        /// <summary>
        /// Adds a given alien race to the list of races to allow for the racial life stage patches.
        /// </summary>
        /// <param name="alienRace">String name of the alien race defName</param>
        /// <returns>Returns true if the race was added to the list, false if it was not.</returns>
        public static void Alien_RacesToPatch()
        {
            foreach (var race in DefDatabase<ThingDef>.AllDefs.Where(thingDef => thingDef.race != null && thingDef.race.Humanlike))
            {
                RaceList.Add(race.defName);
            }
        }

        /// <summary>
        /// Patch is for when the "Babies always healthy" Storyteller option is enabled.
        /// </summary>
        public static bool Alien_CanAddHediffToAnyPartOfDef_Prefix(ref bool __result, Pawn pawn)
        {
            if (RaceList.Contains(pawn.def.defName) && pawn.ageTracker.CurLifeStage.developmentalStage.Baby() && Find.Storyteller.difficulty.babiesAreHealthy)
            {
                __result = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Is optional. Human babies don't have the Gear tab, but also it doesn't cause issues if displayed.
        /// </summary>
        public static bool Alien_get_IsVisible_Prefix(ITab_Pawn_Gear __instance, ref bool __result)
        {
            var pawn = Traverse.Create(__instance).Method("get_SelPawnForGear").GetValue<Pawn>();

            if (RaceList.Contains(pawn.def.defName) && pawn.ageTracker.CurLifeStage.developmentalStage.Baby())
            {
                __result = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Destructively replaces the SimpleCurve for the CertaintyChangeFactor
        /// </summary>
        /// <param name="instructions">The instructions we are messing with.</param>
        /// <param name="ilGenerator">The IDGenerator that allows us to create local variables and labels.</param>
        /// <returns>The code that is usable.</returns>
        public static IEnumerable<CodeInstruction> Alien_CertaintyChangeFactor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            MethodInfo alienCertaintyChangeFactor = AccessTools.Method(typeof(Alien_Patches), nameof(Alien_Patches.CertaintyCurve));
            var x = Traverse.Create(typeof(Pawn_IdeoTracker)).Fields()[0];
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
                
                if (i+1<instructionListCount && instructionList[i].opcode == OpCodes.Ldsfld){

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
        private static Dictionary<ThingDef, SimpleCurve> ideoCurves = new Dictionary<ThingDef, SimpleCurve>();

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

                foreach (var lifeStage in pawn.RaceProps.lifeStageAges)
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

        /// <summary>
        /// Patch is for when the "Babies always healthy" Storyteller option is enabled.
        /// </summary>
        public static bool Alien_TryApply_Prefix(HediffGiver __instance, ref bool __result, Pawn pawn)
        {
            if (RaceList.Contains(pawn.def.defName) && pawn.ageTracker.CurLifeStage.developmentalStage.Baby() && Find.Storyteller.difficulty.babiesAreHealthy)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}
#endif