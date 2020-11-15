using AlienRace;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using static AlienRace.AlienPartGenerator;

namespace Cutebold_Assemblies
{
    /// <summary>
    /// Harmony patches for the different stats we want to adjust.
    /// </summary>
    class Cutebold_Patch_Stats
    {
        /// <summary>
        /// Applies harmony patches on startup.
        /// </summary>
        /// <param name="harmony">Our instance of harmony to patch with.</param>
        /// <param name="settings">Our list of saved settings.</param>
        public Cutebold_Patch_Stats(Harmony harmony, Cutebold_Settings settings)
        {
            if (settings.extraYield)
            {
                // Tweaks Mining Yield for Cutebolds
                harmony.Patch(AccessTools.Method(typeof(Mineable), "TrySpawnYield", null, null), null, new HarmonyMethod(typeof(Cutebold_Patch_Stats), "CuteboldTrySpawnYieldMiningPostfix", null), null, null);
                // Tweaks Harvist Yield for Cutebolds
                harmony.Patch(AccessTools.Method(typeof(JobDriver_PlantWork), "MakeNewToils", null, null), null, new HarmonyMethod(typeof(Cutebold_Patch_Stats), "CuteboldMakeNewToilsPlantWorkPostfix", null), null, null);
                // Edits the stats in the stat bio window to be the correct value.
                harmony.Patch(AccessTools.Method(typeof(StatsReportUtility), "StatsToDraw", new Type[] { typeof(Thing) }, null), null, new HarmonyMethod(typeof(Cutebold_Patch_Stats), "CuteboldStatsToDrawPostfix", null), null, null);
            }

            if (settings.eyeAdaptation)
            {
                // Allows for dark adaptation, obviously not cave adaptation since that is a different game with cute kobolds.
                harmony.Patch(AccessTools.Method(typeof(StatPart_Glow), "FactorFromGlow", null, null), null, new HarmonyMethod(typeof(Cutebold_Patch_Stats), "CuteboldFactorFromGlowPostfix", null), null, null);
                // Applies dark adaptation to all cutebolds as they spawn.               
                harmony.Patch(AccessTools.Method(typeof(Pawn), "SpawnSetup", null, null), null, new HarmonyMethod(typeof(Cutebold_Patch_Stats), "CuteboldAdaptationSpawnSetupPostfix", null), null, null);
            }
            else
            {
                // Removes dark adaptation to all cutebolds as they spawn in.
                harmony.Patch(AccessTools.Method(typeof(Pawn), "SpawnSetup", null, null), null, new HarmonyMethod(typeof(Cutebold_Patch_Stats), "CuteboldNoAdaptationSpawnSetupPostfix", null), null, null);
            }
        }

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

            if (pawn == null || pawn.def.defName != Cutebold_Assemblies.RaceName || __instance == null || __instance.def.building.mineableThing == null || __instance.def.building.mineableDropChance < 1f || !__instance.def.building.mineableYieldWasteable) return;

            //Log.Message("  Effective Mineable Yield="+ __instance.def.building.EffectiveMineableYield.ToString());
            //Log.Message("  Yield Percent=" + ___yieldPct.ToString());
            //Log.Message("  Mineable Thing=" + __instance.def.building.mineableThing.ToString());

            float extraPercent = calculateExtraPercent(StatDefOf.MiningYield, StatRequest.For(pawn));

            //Log.Message("  Pawn Additional Mining Percent=" + extraPercent);

            // Based on the RimWorld base code to allow for mining yield over 100% cause cutebolds are just that good at mining.
            if (___yieldPct >= 1f && extraPercent > 0f)
            {
                Thing minedMaterial = ThingMaker.MakeThing(__instance.def.building.mineableThing, null);
                minedMaterial.stackCount = GenMath.RoundRandom(__instance.def.building.EffectiveMineableYield * extraPercent);
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
        /// Changes the value of certain stats when they exceed the vanilla maximum on purpose.
        /// </summary>
        /// <param name="__result">The list of stats being displayed in the stats bio page.</param>
        /// <param name="thing">The pawn or object being inspected.</param>
        /// <returns></returns>
        private static IEnumerable<StatDrawEntry> CuteboldStatsToDrawPostfix(IEnumerable<StatDrawEntry> __result, Thing thing)
        {
            foreach (StatDrawEntry statEntry in __result)
            {
                if (thing.def != null && thing.def.defName == Cutebold_Assemblies.RaceName)
                {
                    var stat = statEntry.stat;
                    if (stat == StatDefOf.MiningYield || stat == StatDefOf.PlantHarvestYield)
                    {
                        var req = StatRequest.For(thing);
                        var extraPercent = calculateExtraPercent(stat, req);
                        if (extraPercent > 0f) yield return new StatDrawEntry(statEntry.category, stat, 1f + extraPercent, req);
                        else yield return statEntry;
                    }
                    else yield return statEntry;
                }
                else yield return statEntry;
            }
        }

        /// <summary>
        /// Replaces the plant harvest toil of a cutebold to allow them to harvest over 100%.
        /// </summary>
        /// <param name="__result">The previous output from the original toil generator.</param>
        /// <param name="__instance">The plant job.</param>
        /// <returns>A headache. (The new toils)</returns>
        private static IEnumerable<Toil> CuteboldMakeNewToilsPlantWorkPostfix(IEnumerable<Toil> __result, JobDriver_PlantWork __instance)
        {
            //Log.Message("CuteboldTrySapwnYieldMiningPostfix");

            foreach (Toil toil in __result)
            {
                if (toil.tickAction != null && __instance.pawn != null && __instance.pawn.def.defName == Cutebold_Assemblies.RaceName)
                {
                    //Log.Message("  Edit Toil");

                    // Shamelessly taken from the base code and modified to allow cutebolds to harvest just that little bit more with their small, delicate hands.
                    // Two Traverses are used to access protected methods that are overwritten by classes that overwrite the defaults.
                    toil.tickAction = delegate ()
                    {
                        Pawn actor = toil.actor;
                        Map map = actor.Map;
                        float xpPerTick = (float)Traverse.Create(__instance).Field("xpPerTick").GetValue();

                        if (actor.skills != null) actor.skills.Learn(SkillDefOf.Plants, xpPerTick, false);

                        float workSpeed = actor.GetStatValue(StatDefOf.PlantWorkSpeed, true);
                        Plant plant = (Plant)__instance.job.targetA.Thing;

                        workSpeed *= Mathf.Lerp(3.3f, 1f, plant.Growth);
                        var workDoneVariable = Traverse.Create(__instance).Field("workDone");
                        float workDone = (float)workDoneVariable.GetValue() + workSpeed;
                        workDoneVariable.SetValue(workDone);

                        if ((workDone) >= plant.def.plant.harvestWork)
                        {
                            if (plant.def.plant.harvestedThingDef != null)
                            {
                                if (actor.RaceProps.Humanlike && plant.def.plant.harvestFailable && !plant.Blighted && Rand.Value > actor.GetStatValue(StatDefOf.PlantHarvestYield, true))
                                {
                                    MoteMaker.ThrowText((__instance.pawn.DrawPos + plant.DrawPos) / 2f, map, "TextMote_HarvestFailed".Translate(), 3.65f);
                                }
                                else
                                {
                                    int currentYield = GenMath.RoundRandom(plant.YieldNow() * (1f + calculateExtraPercent(StatDefOf.PlantHarvestYield, StatRequest.For(actor))));

                                    //Log.Message("  Pawn Additional Harvest Percent=" + calculateExtraPercent(StatDefOf.PlantHarvestYield, StatRequest.For(actor)));
                                    //Log.Message("  Plant Yield Before=" + plant.YieldNow() + " Plant Yield After=" + currentYield);

                                    if (currentYield > 0)
                                    {
                                        Thing product = ThingMaker.MakeThing(plant.def.plant.harvestedThingDef, null);

                                        product.stackCount = currentYield;

                                        if (actor.Faction != Faction.OfPlayer) product.SetForbidden(true, true);

                                        Find.QuestManager.Notify_PlantHarvested(actor, product);
                                        GenPlace.TryPlaceThing(product, actor.Position, map, ThingPlaceMode.Near, null, null, default(Rot4));
                                        actor.records.Increment(RecordDefOf.PlantsHarvested);
                                    }
                                }
                            }
                            plant.def.plant.soundHarvestFinish.PlayOneShot(actor);
                            plant.PlantCollected();
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
        private static float calculateExtraPercent(StatDef stat, StatRequest req)
        {
            if (stat == null || req == null) return 0f;

            float rawPercent = stat.Worker.GetValueUnfinalized(req, false);
            float basePercent = StatUtility.GetStatValueFromList(req.StatBases, stat, 1f);
            float defaultMaxPercent = stat.maxValue;

            //Log.Message("rawPercent=" + rawPercent + " basePercent=" + basePercent + " defaultMaxPercent=" + defaultMaxPercent);

            return (rawPercent > basePercent) ? (basePercent - defaultMaxPercent) : (rawPercent < defaultMaxPercent) ? 0f : rawPercent - defaultMaxPercent;
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
            if (__instance.Dead || __instance.def == null || __instance.def.defName != Cutebold_Assemblies.RaceName || __instance.kindDef == null) return;

            Hediff hediff = __instance.health.hediffSet.GetFirstHediffOfDef(Cutebold_DefOf.CuteboldDarkAdaptation);

            if (hediff != null)
            {
                __instance.health.RemoveHediff(hediff);
            }
        }
    }
}
