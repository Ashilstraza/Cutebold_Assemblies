﻿using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Cutebold_Assemblies
{
    /// <summary>
    /// Harmony patches for the different stats we want to adjust.
    /// </summary>
    class Cutebold_Patch_Stats
    {
        private static bool adaptation = false;

        /// <summary>
        /// Applies harmony patches on startup.
        /// </summary>
        /// <param name="harmony">Our instance of harmony to patch with.</param>
        public Cutebold_Patch_Stats(Harmony harmony)
        {
            var settings = Cutebold_Assemblies.CuteboldSettings;
            if (settings.extraYield && ModLister.GetActiveModWithIdentifier("syrchalis.harvestyieldpatch") == null)
            {

                if (settings.eyeAdaptation)
                {
                    adaptation = true;

                    // Insert Dark Adaptation bonus yield explination
                    harmony.Patch(AccessTools.Method(typeof(StatWorker), "GetExplanationUnfinalized"), postfix: new HarmonyMethod(typeof(Cutebold_Patch_Stats), "CuteboldGetExplanationUnfinalizedPostfix"));
                }

                if (settings.altYield) // Use Postfixes
                {
                    // Tweaks Mining Yield for Cutebolds
                    harmony.Patch(AccessTools.Method(typeof(Mineable), "TrySpawnYield"), postfix: new HarmonyMethod(typeof(Cutebold_Patch_Stats), "CuteboldTrySpawnYieldMiningPostfix"));
                    // Tweaks Harvist Yield for Cutebolds
                    harmony.Patch(AccessTools.Method(typeof(JobDriver_PlantWork), "MakeNewToils"), postfix: new HarmonyMethod(typeof(Cutebold_Patch_Stats), "CuteboldMakeNewToilsPlantWorkPostfix"));
                }
                else // Use Transpilers
                {
                    // Tweaks Mining Yield for Cutebolds
                    harmony.Patch(AccessTools.Method(typeof(Mineable), "TrySpawnYield"), transpiler: new HarmonyMethod(typeof(Cutebold_Patch_Stats), "CuteboldTrySpawnYieldMiningTranspiler"));
                    // Tweaks Harvist Yield for Cutebolds
                    var plantWorkToilMethod = AccessTools.GetDeclaredMethods(typeof(JobDriver_PlantWork).GetNestedTypes(AccessTools.all).First()).ElementAt(1);
                    harmony.Patch(plantWorkToilMethod, transpiler: new HarmonyMethod(typeof(Cutebold_Patch_Stats), "CuteboldMakeNewToilsPlantWorkTranspiler"));
                }

                // Edits the stats in the stat bio window to be the correct value.
                harmony.Patch(AccessTools.Method(typeof(StatsReportUtility), "StatsToDraw", new[] { typeof(Thing) }), postfix: new HarmonyMethod(typeof(Cutebold_Patch_Stats), "CuteboldStatsToDrawPostfix"));

            }
        }

        /// Convered into a transpiler.
        /// <summary>
        /// Adds extra materials to mined rock when the yield is over the default max.
        /// </summary>
        /// <param name="__instance">What was mined</param>
        /// <param name="___yieldPct">The max yield percentage</param>
        /// <param name="map">The map we are on</param>
        /// <param name="yieldChance">The chance to yield something (Ignored)</param>
        /// <param name="moteOnWaste">If we should do a mote on waste (Ignored)</param>
        /// <param name="pawn">The pawn mining</param>
        private static void CuteboldTrySpawnYieldMiningPostfix(Mineable __instance, float ___yieldPct, Map map, float yieldChance, bool moteOnWaste, Pawn pawn)
        {
            //Log.Message("CuteboldTrySapwnYieldMiningPostfix");

            if (pawn?.def.defName != Cutebold_Assemblies.RaceName || __instance == null || __instance.def.building.mineableThing == null || __instance.def.building.mineableDropChance < 1f || !__instance.def.building.mineableYieldWasteable) return;
            //if (pawn == null || pawn.def.defName != Cutebold_Assemblies.RaceName || __instance == null || __instance.def.building.mineableThing == null || __instance.def.building.mineableDropChance < 1f || !__instance.def.building.mineableYieldWasteable) return;

            //Log.Message("  Effective Mineable Yield="+ __instance.def.building.EffectiveMineableYield.ToString());
            //Log.Message("  Yield Percent=" + ___yieldPct.ToString());
            //Log.Message("  Mineable Thing=" + __instance.def.building.mineableThing.ToString());

            float extraPercent = CuteboldCalculateExtraPercent(StatDefOf.MiningYield, StatRequest.For(pawn));

            //Log.Message("  Pawn Additional Mining Percent=" + extraPercent);

            // Based on the RimWorld base code to allow for mining yield over 100% cause cutebolds are just that good at mining.
            if (___yieldPct >= 1f && extraPercent > 0f)
            {
                Thing minedMaterial = ThingMaker.MakeThing(__instance.def.building.mineableThing);
                minedMaterial.stackCount = GenMath.RoundRandom(__instance.def.building.EffectiveMineableYield * extraPercent);
// 1.1          minedMaterial.stackCount = GenMath.RoundRandom(__instance.def.building.mineableYield * extraPercent);
                GenPlace.TryPlaceThing(minedMaterial, __instance.Position, map, ThingPlaceMode.Near, ForbidIfNecessary);
            }

            void ForbidIfNecessary(Thing minedMaterial, int count)
            {
                if ((pawn == null || !pawn.IsColonist) && minedMaterial != null && minedMaterial.def.EverHaulable && !minedMaterial.def.designateHaulable)
                {
                    minedMaterial.SetForbidden(value: true, warnOnFail: false);
                }
            }
        }

        /// <summary>
        /// /// Allows for cutebolds to exceed the max mining yield.
        /// </summary>
        /// <param name="instructions">The instructions we are messing with.</param>
        /// <param name="ilGenerator">The IDGenerator that allows us to create local variables and labels.</param>
        /// <returns>All the code!</returns>
        private static IEnumerable<CodeInstruction> CuteboldTrySpawnYieldMiningTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            FieldInfo miningYield = AccessTools.Field(typeof(StatDefOf), nameof(StatDefOf.MiningYield));
            MethodInfo statRequest = AccessTools.Method(typeof(StatRequest), nameof(StatRequest.For), new[] { typeof(Thing) });
            MethodInfo calculateExtraPercent = AccessTools.Method(typeof(Cutebold_Patch_Stats), nameof(CuteboldCalculateExtraPercent));
            FieldInfo def = AccessTools.Field(typeof(Thing), nameof(Thing.def));
            FieldInfo building = AccessTools.Field(typeof(ThingDef), nameof(ThingDef.building));
            MethodInfo getEffectiveMineableYield = AccessTools.Method(typeof(BuildingProperties), "get_EffectiveMineableYield");
// 1.1      FieldInfo mineableYield = AccessTools.Field(typeof(BuildingProperties), "mineableYield");
            MethodInfo roundRandom = AccessTools.Method(typeof(GenMath), nameof(GenMath.RoundRandom), new[] { typeof(float) });
            FieldInfo stackCount = AccessTools.Field(typeof(Thing), nameof(Thing.stackCount));
            int pawn = 4;
            OpCode getNum = OpCodes.Ldloc_1;
            OpCode storeNum = OpCodes.Stloc_1;
// 1.1      OpCode getNum = OpCodes.Ldloc_0;
// 1.1      OpCode storeNum = OpCodes.Stloc_0;

            List<CodeInstruction> instructionList = instructions.ToList();
            int instructionListCount = instructionList.Count;

            /*
             * See drSpy decompile of PawnRenderer.RenderPawnInternal() for variable references
             *
             * num += GenMath.RoundRandom(
             *          CuteboldCalculateExtraPercent(StatDefOf.MiningYield, StatRequest.For(pawn), true)) *
             *          __instance.def.building.EffectiveMineableYield)
             */
            List<CodeInstruction> extraYield = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldsfld, miningYield), // Load StatDefOf.MiningYield
                new CodeInstruction(OpCodes.Ldarg_S, pawn), // Load pawn
                new CodeInstruction(OpCodes.Call, statRequest), // Calls StatRequest.For on pawn
                new CodeInstruction(OpCodes.Ldc_I4_1), // Load 1 onto the stack
                new CodeInstruction(OpCodes.Call, calculateExtraPercent), // Call CuteboldCalculateExtraPercent(StatDefOf.MiningYield, StatRequest.For(pawn), true)
                new CodeInstruction(OpCodes.Ldarg_0), // Load this
                new CodeInstruction(OpCodes.Ldfld, def), // Load def
                new CodeInstruction(OpCodes.Ldfld, building), // Load building
                new CodeInstruction(OpCodes.Callvirt, getEffectiveMineableYield), // Call virtual get_EffectiveMineableYield()
// 1.1          new CodeInstruction(OpCodes.Ldfld, mineableYield), // Load mineableYield
                new CodeInstruction(OpCodes.Conv_R4), // Converts result of get_EffectiveMineableYield to a float
                new CodeInstruction(OpCodes.Mul), // Multiplies effective yield and extra percent together
                new CodeInstruction(OpCodes.Call, roundRandom), // Call RoundRandom(float)
                new CodeInstruction(getNum), // Load num
                new CodeInstruction(OpCodes.Add), // Add num and the extra yield together
                new CodeInstruction(storeNum), // Stores the new yield in num
            };

            for (int i = 0; i < instructionListCount; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (i - 3 > 0 && instructionList[i - 3].Is(OpCodes.Call, roundRandom))
                {
                    foreach (CodeInstruction codeInstruction in extraYield)
                    {
                        yield return codeInstruction;
                    }
                }

                yield return instruction;
            }
        }

        /// <summary>
        /// Allows for cutebolds to exceed the max harvesting yield.
        /// </summary>
        /// <param name="instructions">The instructions we are messing with.</param>
        /// <param name="ilGenerator">The IDGenerator that allows us to create local variables and labels.</param>
        /// <returns>All the code!</returns>
        private static IEnumerable<CodeInstruction> CuteboldMakeNewToilsPlantWorkTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            MethodInfo yieldNow = AccessTools.Method(typeof(Plant), nameof(Plant.YieldNow));
            FieldInfo plantHarvestYield = AccessTools.Field(typeof(StatDefOf), nameof(StatDefOf.PlantHarvestYield));
            MethodInfo statRequest = AccessTools.Method(typeof(StatRequest), nameof(StatRequest.For), new[] { typeof(Thing) });
            MethodInfo calculateExtraPercent = AccessTools.Method(typeof(Cutebold_Patch_Stats), nameof(CuteboldCalculateExtraPercent));
            MethodInfo roundRandom = AccessTools.Method(typeof(GenMath), nameof(GenMath.RoundRandom), new[] { typeof(float) });

            List<CodeInstruction> instructionList = instructions.ToList();
            int instructionListCount = instructionList.Count;

            /*
             * See drSpy decompile of PawnRenderer.RenderPawnInternal() for variable references
             * 
             * int num2 = GenMath.RoundRandom((float)plant.YieldNow() * (1f + Cutebold_Patch_Stats.CuteboldCalculateExtraPercent(StatDefOf.PlantHarvestYield, StatRequest.For(actor), true)));
             * 
             */
            List<CodeInstruction> extraYield = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Conv_R4), // Convert result of YieldNow to a float
                new CodeInstruction(OpCodes.Ldc_R4, 1f), // Load 1f onto the stack
                new CodeInstruction(OpCodes.Ldsfld, plantHarvestYield), // Load StatDefOf.PlantHarvestYield
                new CodeInstruction(OpCodes.Ldloc_0), // Load pawn
                new CodeInstruction(OpCodes.Call, statRequest), // Calls StatRequest.For on pawn
                new CodeInstruction(OpCodes.Ldc_I4_1), // Load 1 onto the stack
                new CodeInstruction(OpCodes.Call, calculateExtraPercent), // Call CuteboldCalculateExtraPercent(StatDefOf.PlantHarvestYield, StatRequest.For(pawn), true)
                new CodeInstruction(OpCodes.Add), // Add 1f to the result of the extra percent
                new CodeInstruction(OpCodes.Mul), // Multiplies the plant yield with the extra percent
                new CodeInstruction(OpCodes.Call, roundRandom) // Calls RoundRandom on the result of the new yield
            };

            for (int i = 0; i < instructionListCount; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (i > 0 && instructionList[i - 1].Is(OpCodes.Callvirt, yieldNow))
                {
                    foreach (CodeInstruction codeInstruction in extraYield)
                    {
                        yield return codeInstruction;
                    }
                }

                yield return instruction;
            }
        }

        /// <summary>
        /// Changes the value of certain stats when they exceed the vanilla maximum on purpose.
        /// </summary>
        /// <param name="__result">The list of stats being displayed in the stats bio page.</param>
        /// <param name="thing">The pawn or object being inspected.</param>
        /// <returns>Requested stat entry.</returns>
        private static IEnumerable<StatDrawEntry> CuteboldStatsToDrawPostfix(IEnumerable<StatDrawEntry> __result, Thing thing)
        {
            foreach (StatDrawEntry statEntry in __result)
            {
                if (thing.def?.defName == Cutebold_Assemblies.RaceName)
                {
                    var stat = statEntry.stat;
                    if (stat == StatDefOf.MiningYield || stat == StatDefOf.PlantHarvestYield)
                    {
                        var req = StatRequest.For(thing);
                        var extraPercent = CuteboldCalculateExtraPercent(stat, req);
                        if (extraPercent > 0f) yield return new StatDrawEntry(statEntry.category, stat, 1f + extraPercent, req);
                        else yield return statEntry;
                    }
                    else
                    {
                        yield return statEntry;
                    }
                }
                else
                {
                    yield return statEntry;
                }
            }
        }

        /// <summary>
        /// Inserts the extra mining yield multiplier into the mining yield detailed description
        /// </summary>
        /// <param name="__result">The description we are adding onto.</param>
        /// <param name="__instance">The StatWorker</param>
        /// <param name="___stat">The StatDef of the StatWorker</param>
        /// <param name="req">The item requesting the stat.</param>
        /// <param name="numberSense">Unused</param>
        private static void CuteboldGetExplanationUnfinalizedPostfix(ref string __result, StatDef ___stat, StatRequest req, ToStringNumberSense numberSense)
        {
            Pawn pawn = req.Pawn ?? (req.Thing is Pawn ? (Pawn)req.Thing : null);

            if (pawn?.def != Cutebold_Assemblies.AlienRaceDef || ___stat != StatDefOf.MiningYield) return;

            float rawPercent = CuteboldCalculateExtraPercent(___stat, req, false);
            float multiplier = MiningMultiplier(pawn);
            StringBuilder stringBuilder = new StringBuilder(__result);

            stringBuilder.AppendLine("Dark Adaptation");
            stringBuilder.AppendLine($"    Extra Percentage: {rawPercent.ToStringPercent()} (base, 15% max) x {multiplier.ToStringPercent()} (adaptation, 100% max) => +{(rawPercent * multiplier).ToStringPercent()}");

            __result = stringBuilder.ToString();
        }

        /// Changed into a transpiler.
        /// <summary>
        /// Replaces the plant harvest toil of a cutebold to allow them to harvest over 100%.
        /// </summary>
        /// <param name="__result">The previous output from the original toil generator.</param>
        /// <param name="__instance">The plant job.</param>
        /// <returns>A headache. (The new toils)</returns>
        private static IEnumerable<Toil> CuteboldMakeNewToilsPlantWorkPostfix(IEnumerable<Toil> __result, JobDriver_PlantWork __instance)
        {
            //Log.Message("CuteboldMakeNewToilsPlantWorkPostfix");

            foreach (Toil toil in __result)
            {
                if (toil.tickAction != null && __instance.pawn?.def.defName == Cutebold_Assemblies.RaceName)
                {
                    //Log.Message("  Edit Toil");

                    // Shamelessly taken from the base code and modified to allow cutebolds to harvest just that little bit more with their small, delicate hands.
                    // Two Traverses are used to access protected methods that are overwritten by classes that overwrite the defaults.
                    toil.tickAction = delegate ()
                    {
                        Pawn actor = toil.actor;
                        Map map = actor.Map;
                        float xpPerTick = (float)Traverse.Create(__instance).Field("xpPerTick").GetValue();

                        if (actor.skills != null) actor.skills.Learn(SkillDefOf.Plants, xpPerTick);

                        float workSpeed = actor.GetStatValue(StatDefOf.PlantWorkSpeed, true);
                        Plant plant = (Plant)__instance.job.targetA.Thing;

                        workSpeed *= UnityEngine.Mathf.Lerp(3.3f, 1f, plant.Growth);
                        var workDoneVariable = Traverse.Create(__instance).Field("workDone");
                        float workDone = (float)workDoneVariable.GetValue() + workSpeed;
                        workDoneVariable.SetValue(workDone);

                        if ((workDone) >= plant.def.plant.harvestWork)
                        {
                            if (plant.def.plant.harvestedThingDef != null)
                            {
                                StatDef stat = (plant.def.plant.harvestedThingDef.IsDrug ? StatDefOf.DrugHarvestYield : StatDefOf.PlantHarvestYield);
// 1.1                          StatDef stat = StatDefOf.PlantHarvestYield;
                                float yieldMultiplier = (1f + CuteboldCalculateExtraPercent(stat, StatRequest.For(actor)));
                                if (actor.RaceProps.Humanlike && plant.def.plant.harvestFailable && !plant.Blighted && Rand.Value > yieldMultiplier)
                                {
                                    MoteMaker.ThrowText((__instance.pawn.DrawPos + plant.DrawPos) / 2f, map, "TextMote_HarvestFailed".Translate(), 3.65f);
                                }
                                else
                                {
                                    int currentYield = GenMath.RoundRandom(plant.YieldNow() * yieldMultiplier);

                                    //Log.Message("  Pawn Additional Harvest Percent=" + calculateExtraPercent(StatDefOf.PlantHarvestYield, StatRequest.For(actor)));
                                    //Log.Message("  Plant Yield Before=" + plant.YieldNow() + " Plant Yield After=" + currentYield);

                                    if (currentYield > 0)
                                    {
                                        Thing product = ThingMaker.MakeThing(plant.def.plant.harvestedThingDef, null);

                                        product.stackCount = currentYield;

                                        if (actor.Faction != Faction.OfPlayer) product.SetForbidden(true);

                                        Find.QuestManager.Notify_PlantHarvested(actor, product);
                                        GenPlace.TryPlaceThing(product, actor.Position, map, ThingPlaceMode.Near);
                                        actor.records.Increment(RecordDefOf.PlantsHarvested);
                                    }
                                }
                            }
                            plant.def.plant.soundHarvestFinish.PlayOneShot(actor);
                            plant.PlantCollected(__instance.pawn);
// 1.1                      plant.PlantCollected();
                            workDoneVariable.SetValue(0f);
                            __instance.ReadyForNextToil();
                            return;
                        }
                    };
                }
                yield return toil;
            }
        }

        /// <summary>
        /// Calculates the extra yield for a given task and pawn.
        /// </summary>
        /// <param name="stat">The stat to check</param>
        /// <param name="req">The pawn we are checking the stat on</param>
        /// <returns>The extra yield</returns>
        private static float CuteboldCalculateExtraPercent(StatDef stat, StatRequest req, bool useMultiplier = true)
        {
            Pawn pawn = req.Pawn ?? (req.Thing is Pawn ? (Pawn)req.Thing : null);

            if (stat == null || req == null || pawn?.def != Cutebold_Assemblies.AlienRaceDef) return 0f;
            if (stat == StatDefOf.PlantHarvestYield) useMultiplier = false;

            float rawPercent = stat.Worker.GetValueUnfinalized(req, false);
            float pawnBasePercent = StatUtility.GetStatValueFromList(req.StatBases, stat, 1f);
            float defaultMaxPercent = stat.maxValue;
            float multiplier = 1f;

            //Log.Message("adaptation=" + adaptation + "has hediff="+ pawn?.health.hediffSet.HasHediff(Cutebold_DefOf.CuteboldDarkAdaptation).ToString());

            if (adaptation && useMultiplier && pawn?.health.hediffSet.HasHediff(Cutebold_DefOf.CuteboldDarkAdaptation) == true) multiplier = MiningMultiplier(pawn);

            //Log.Message("rawPercent=" + rawPercent + " pawnBasePercent=" + pawnBasePercent + " defaultMaxPercent=" + defaultMaxPercent + "multiplier=" + multiplier);

            float extraPercent = (rawPercent > pawnBasePercent) ? (pawnBasePercent - defaultMaxPercent) * multiplier : ((rawPercent < defaultMaxPercent) ? 0f : (rawPercent - defaultMaxPercent) * multiplier);

            return (extraPercent > 0f) ? extraPercent : 0f;
        }

        /// <summary>
        /// Calculates the multiplier from dark adaptation
        /// </summary>
        /// <param name="pawn">The pawn to check dark adaptation on</param>
        /// <returns>multiplier</returns>
        private static float MiningMultiplier(Pawn pawn)
        {
            Hediff_CuteboldDarkAdaptation darkAdaptation = (Hediff_CuteboldDarkAdaptation)pawn.health.hediffSet.GetFirstHediffOfDef(Cutebold_DefOf.CuteboldDarkAdaptation);
            var severity = darkAdaptation.Severity;
            return darkAdaptation.WearingGoggles ? 0.25f : (severity < 0.25f) ? 0.25f : (severity < 0.5f) ? 0.5f : (severity < 0.75f) ? 0.75f : 1f;
        }
    }
}
