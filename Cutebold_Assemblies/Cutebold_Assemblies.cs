﻿using AlienRace;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Cutebold_Assemblies
{
    /// <summary>
    /// Main cutebold class that handles modifications that can not be done in XML.
    /// </summary>
    /// <remarks>
    /// <para>Handles interpawn opinions and pawn thoughts.</para>
    /// </remarks>
    /// 
    [StaticConstructorOnStartup]
    public static class Cutebold_Assemblies
    {
        /// <summary>Cutebold alien race def.</summary>
        private static ThingDef_AlienRace alienRaceDef;
        /// <summary>List of all the butcherable races that gives bad thoughts to cutebolds.</summary>
        private static IEnumerable<string> butcherRaceList;
        /// <summary>List of all the humanoid leathers.</summary>
        private static IEnumerable<ThingDef> humanoidLeathers;
        /// <summary>If we already have logged the butcher error</summary>
        private static bool butcherLogged = false;
        /// <summary>If we already have logged the ingestor error</summary>
        private static bool ingestedLogged = false;

        /// <summary>Our mod name, for debug output purposes.</summary>
        public static string ModName { get; } = "Cutebold Race Mod";
        /// <summary>Cutebold race def string.</summary>
        public static string RaceName { get; } = "Alien_Cutebold";

        /// <summary>
        /// Main constructor for setting up some values and executing harmony patches.
        /// </summary>
        static Cutebold_Assemblies()
        {
            var settings = LoadedModManager.GetMod<CuteboldMod>().GetSettings<Cutebold_Settings>();
            var harmony = new Harmony("rimworld.ashilstraza.races.cute.main");

            alienRaceDef = (ThingDef_AlienRace)DefDatabase<ThingDef>.GetNamed(RaceName);

            try { CreateButcherRaceList(); }
            catch (MissingFieldException e)
            {
                Log.Error(string.Format("{0}: Unable to create butcher race list. Check and see if Humanoid Alien Races has been updated.\n    {1}", new object[]{
                    ModName,
                    e.GetBaseException().ToString()
                }));
            }
            CreateHumanoidLeatherList();

            new Cutebold_Patch_Names(harmony);

            // Eating Humanoid Meat
            harmony.Patch(AccessTools.Method(typeof(Thing), "Ingested", null, null), null, new HarmonyMethod(typeof(Cutebold_Assemblies), "CuteboldIngestedPostfix", null), null, null);
            // Butchering Humanoid Corpses
            harmony.Patch(AccessTools.Method(typeof(Corpse), "ButcherProducts", null, null), null, new HarmonyMethod(typeof(Cutebold_Assemblies), "CuteboldButcherProductsPostfix", null), null, null);
            // Wearing Humanoid Clothing
            harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_HumanLeatherApparel), "CurrentStateInternal", null, null), null, new HarmonyMethod(typeof(Cutebold_Assemblies), "CuteboldCurrentStateInternalPostfix", null), null, null);
            // Disable eye glow when sleeping/in light
            harmony.Patch(AccessTools.Method(typeof(AlienPartGenerator.BodyAddon), "CanDrawAddon", null, null), null, new HarmonyMethod(typeof(Cutebold_Assemblies), "CuteboldCanDrawAddonPostfix", null), null, null);

            // Optional Patches
            new Cutebold_Patch_Stats(harmony, settings);
        }

        /// <summary>
        /// Creates a list of races that give bad butcher thoughts.
        /// </summary>
        private static void CreateButcherRaceList()
        {
            //Log.Message("Create Butcher Race List");
            List<string> butcherList = new List<string>();

            foreach (ThingDef race in alienRaceDef.alienRace.thoughtSettings.butcherThoughtSpecific.FirstOrDefault().raceList)
            {
                //Log.Message("  Race: " + race.defName);
                butcherList.Add(race.defName);
            }
            butcherRaceList = butcherList;
        }

        /// <summary>
        /// Creates a list of humanoid leathers.
        /// </summary>
        private static void CreateHumanoidLeatherList()
        {
            //Log.Message("Create Humanoid Leather List");
            var aliens = new List<ThingDef>();
            var animals = new List<ThingDef>();

            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef.race != null && thingDef.race.leatherDef != null)
                {
                    //Log.Message("thingDef: " + thingDef.ToString()+" thingDef.race: " + thingDef.race.ToString() + " thingDef.race.leatherDef: " + thingDef.race.leatherDef.ToString(), true);
                    if (thingDef.race.Humanlike)
                    {
                        //Log.Message("    Humanlike");
                        aliens.Add(thingDef.race.leatherDef);
                    }
                    else
                    {
                        animals.Add(thingDef.race.leatherDef);
                    }
                }
            }

            humanoidLeathers = aliens.Except(animals).AsEnumerable<ThingDef>(); // We only want the list of leathers unique to humanoids.
            //Log.Message("Humanoid Leather:");
            //foreach (ThingDef leathers in humanoidLeathers) Log.Message(leathers.ToString());
        }

        /// <summary>
        /// Patch that checks to see if a cutebold has eaten something that others will have a bad opinion of.
        /// </summary>
        /// <param name="__result">The returned float of the Ingested method.</param>
        /// <param name="__instance">What was ingested.</param>
        /// <param name="ingester">Who ingested the thing.</param>
        /// <returns>Passes through the original float without modifying it.</returns>
        private static void CuteboldIngestedPostfix(Thing __instance, Pawn ingester)
        {
            //Log.Message("Ingested Postfix");
            //Log.Message("Instance: " + __instance.ToString() + " Instance Source: " + ((__instance.def.ingestible != null && __instance.def.ingestible.sourceDef != null) ? __instance.def.ingestible.sourceDef.defName.ToString() : "Null") + " ingester: " + ingester.ToString());
            if (__instance != null && ingester != null && __instance.def.ingestible != null && __instance.def.ingestible.sourceDef != null)
            {
                try
                {
                    if (butcherRaceList.Contains(__instance.def.ingestible.sourceDef.defName))
                    {
                        TaleRecorder.RecordTale(Cutebold_DefOf.AteRawCuteboldMeatTale, new object[]
                        {
                        ingester
                        });
                    }
                }
                catch (NullReferenceException)
                {
                    if (Prefs.DevMode && !ingestedLogged)
                    {
                        Log.Warning(string.Format("{0}: Ingested an item that has an issue, only logging once.\n  Thing ingested: {1}\n  Ingestor: {2}", new object[]{
                            ModName,
                            __instance.ToString(),
                            ingester.ToString()
                    }));
                        ingestedLogged = true;
                    }
                }
            }
        }

        /// <summary>
        /// Patch that checks to see if a cutebold has butchered something that others will have a bad opinion of.
        /// </summary>
        /// <param name="__result">The returned IEnumerable of the ButcherProducts method.</param>
        /// <param name="__instance">What was butchered.</param>
        /// <param name="butcher">Who butchered the thing.</param>
        /// <returns>Passes through the original IEnumerable.</returns>
        private static void CuteboldButcherProductsPostfix(Corpse __instance, Pawn butcher)
        {
            //Log.Message("Butcher Products Postfix");
            //Log.Message("Instance: " +  __instance.ToString() + " Instance Source: " + ((__instance.def.ingestible != null && __instance.def.ingestible.sourceDef != null) ? __instance.def.ingestible.sourceDef.defName.ToString() : "Null") + " butcher: " + butcher.ToString());
            if (__instance != null && butcher != null && __instance.def.ingestible != null && __instance.def.ingestible.sourceDef != null)
            {
                try
                {
                    if (butcherRaceList.Contains(__instance.def.ingestible.sourceDef.defName))
                    {
                        TaleRecorder.RecordTale(Cutebold_DefOf.ButcheredCuteboldCorpseTale, new object[]
                        {
                    butcher
                        });
                    }
                }
                catch (NullReferenceException)
                {
                    if (Prefs.DevMode && !butcherLogged)
                    {
                        Log.Warning(string.Format("{0}: Butchered an item that has an issue, only logging once.\n  Corpse Butchered: {1}\n  Butcher: {2}", new object[]{
                            ModName,
                            __instance.ToString(),
                            butcher.ToString()
                    }));
                        butcherLogged = true;
                    }
                }
            }
        }

        /// <summary>
        /// Patch that checks what cutebolds are wearing and if they should have a mood change depending if they are wearing humanoid leather instead of just human leather.
        /// </summary>
        /// <param name="__result">How much of an effect the clothing is having on the pawn. Max is 4.</param>
        /// <param name="p">The pawn to check.</param>
        /// <returns>The modified effect of the clothing. Max is 4.</returns>
        private static void CuteboldCurrentStateInternalPostfix(ThoughtState __result, Pawn p)
        {
            //Log.Message("Current State Internal");
            if (p == null || p.def.defName != RaceName) return; // We only want to apply to cutebolds (currently).

            // Pretty much copied from the regular code.
            string text = null;
            int num = 0;
            ThoughtState newThoughtState;
            foreach (var apparel in p.apparel.WornApparel)
            {
                //Log.Message("apparel stuff: "+wornApparel[i].Stuff.ToString());
                if (humanoidLeathers.Contains(apparel.Stuff))
                {
                    if (text == null)
                    {
                        text = apparel.def.label;
                    }
                    num++;
                }
            }

            if (num == 0) newThoughtState = ThoughtState.Inactive;
            else if (num >= 5) newThoughtState = ThoughtState.ActiveAtStage(4, text);
            else newThoughtState = ThoughtState.ActiveAtStage(num - 1, text);

            //Log.Message("num: " + num.ToString() + " Result Stage Index: " + __result.StageIndex.ToString() + "new Thought State Stage Index: " + newThoughtState.StageIndex.ToString());

            if (__result.StageIndex <= newThoughtState.StageIndex) __result = newThoughtState;
        }

        /// <summary>
        /// Patch that hides the eye glow on cutebolds depending on a handfull of situations
        /// </summary>
        /// <param name="__instance">The body addon to check</param>
        /// <param name="__result">If the body addon is hidden</param>
        /// <param name="pawn">The pawn the body addon belongs to</param>
        public static void CuteboldCanDrawAddonPostfix(AlienPartGenerator.BodyAddon __instance, ref bool __result, Pawn pawn)
        {
            if(__result && pawn.def.defName == RaceName && __instance.ColorChannel == "eye")
            {
                if (pawn.Dead || pawn.Map.glowGrid.GameGlowAt(pawn.Position) >= 0.3f || (pawn.CurJob != null && pawn.jobs.curDriver.asleep) || 
                    pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight) == 0f || pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) <=0.1f)
                {
                    __result = false;
                }
            }
        }
    }
}
