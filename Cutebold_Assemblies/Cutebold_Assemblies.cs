using AlienRace;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Cutebold_Assemblies
{
    /// <summary>
    /// Main cutebold class that handles modifications that can not be done in XML. Some patches are split off into their own classes for organization, one ones that are in this class are the left overs.
    /// </summary>
    /// <remarks>
    /// <para>Handles interpawn opinions and pawn thoughts.</para>
    /// </remarks>
    /// 
    [StaticConstructorOnStartup]
    public static class Cutebold_Assemblies
    {
        /// <summary>List of all the butcherable races that gives bad thoughts to cutebolds.</summary>
        private static IEnumerable<string> butcherRaceList;
        /// <summary>List of all the humanoid leathers.</summary>
        private static IEnumerable<ThingDef> humanoidLeathers;
        /// <summary>If we already have logged the butcher error.</summary>
        private static bool butcherLogged = false;
        /// <summary>If we already have logged the ingestor error.</summary>
        private static bool ingestedLogged = false;

        /// <summary>Cutebold alien race def.</summary>
        public static readonly ThingDef_AlienRace AlienRaceDef = (ThingDef_AlienRace)Cutebold_DefOf.Alien_Cutebold;
        /// <summary>Our mod name, for debug output purposes.</summary>
        public static readonly string ModName = "Cutebold Race Mod";
        /// <summary>Cutebold race def string.</summary>
        public static readonly string RaceName = "Alien_Cutebold";
        /// <summary>Cutebold Harmony ID.</summary>
        public static readonly string HarmonyID = "rimworld.ashilstraza.races.cute.main";
        /// <summary>Reference to harmony.</summary>
        private static readonly Harmony harmony = new Harmony(HarmonyID);

        public static readonly Cutebold_Settings CuteboldSettings = null;


        /// <summary>
        /// Main constructor for setting up some values and executing harmony patches.
        /// </summary>
        static Cutebold_Assemblies()
        {
            CuteboldSettings = LoadedModManager.GetMod<CuteboldMod>().GetSettings<Cutebold_Settings>();

            try { CreateButcherRaceList(); } // Added because of an update to HAR that changed how referencing other races worked.
            catch (MissingFieldException e)
            {
                Log.Error($"{ModName}: Unable to create butcher race list. Check and see if Humanoid Alien Races has been updated.\n    {e.GetBaseException()}");
            }
            CreateHumanoidLeatherList();

            new Cutebold_Patch_Names(harmony);

            // Eating Humanoid Meat
            harmony.Patch(AccessTools.Method(typeof(Thing), "Ingested"), postfix: new HarmonyMethod(typeof(Cutebold_Assemblies), "CuteboldIngestedPostfix"));
            // Butchering Humanoid Corpses
            harmony.Patch(AccessTools.Method(typeof(Corpse), "ButcherProducts"), postfix: new HarmonyMethod(typeof(Cutebold_Assemblies), "CuteboldButcherProductsPostfix"));
            // Wearing Humanoid Clothing
            harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_HumanLeatherApparel), "CurrentStateInternal"), postfix: new HarmonyMethod(typeof(Cutebold_Assemblies), "CuteboldCurrentStateInternalPostfix"));

            new Cutebold_Patch_BodyAddons(harmony);

            new Cutebold_Patch_Stats(harmony);

            new Cutebold_Patch_HediffRelated(harmony);
        }

        /// <summary>
        /// Creates a list of races that give bad butcher thoughts.
        /// </summary>
        /*private static void CreateButcherRaceList()
        {
            //Log.Message("Create Butcher Race List");
            List<string> butcherList = new List<string>();

            foreach (ThingDef race in AlienRaceDef.alienRace.thoughtSettings.butcherThoughtSpecific.FirstOrDefault().raceList)
            {
                //Log.Message("  Race: " + race.defName);
                butcherList.Add(race.defName);
            }
            butcherRaceList = butcherList;
        }*/
        private static void CreateButcherRaceList()
        {
            Log.Message("Create Butcher Race List");
            List<string> butcherList = new List<string>();

// 1.3      foreach (ThingDef race in AlienRaceDef.alienRace.thoughtSettings.butcherThoughtSpecific.FirstOrDefault().raceList)
            foreach (String race in AlienRaceDef.alienRace.thoughtSettings.butcherThoughtSpecific.FirstOrDefault().raceList)
            {
// 1.3          Log.Message("  Race: " + race.defName);
// 1.3          butcherList.Add(race.defName);

                Log.Message("  Race: " + race);
                butcherList.Add(race);
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
                if (thingDef.race?.leatherDef != null)
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
            if (ingester != null && __instance?.def.ingestible?.sourceDef != null)
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
                        Log.Warning($"{ModName}: Ingested an item that has an issue, only logging once.\n  Thing ingested: {__instance}\n  Ingestor: {ingester}");
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
            if (butcher != null && __instance?.def.ingestible?.sourceDef != null)
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
                        Log.Warning($"{ModName}: Butchered an item that has an issue, only logging once.\n  Corpse Butchered: {__instance}\n  Butcher: {butcher}");
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
        /// Runs a check on all the methods we patch and outputs all the patches for those methods.
        /// </summary>
        public static void CheckPatchedMethods()
        {
            Log.Warning($"{ModName}: Checking Patched Methods...");
            var patchedMethods = harmony.GetPatchedMethods();

            foreach (var method in patchedMethods)
            {
                var patches = Harmony.GetPatchInfo(method);

                Log.Warning($"    {method.Name}");

                if (patches != null)
                {
                    if (patches.Prefixes.Count > 0)
                    {
                        Log.Warning($"        Prefixes:");
                        foreach (var patch in patches.Prefixes)
                        {
                            Log.Warning($"            index={patch.index} owner={patch.owner} patchMethod={patch.PatchMethod} priority={patch.priority} before={patch.before} after={patch.after}");
                        }
                    }
                    if (patches.Postfixes.Count > 0)
                    {
                        Log.Warning($"        Postfixes:");
                        foreach (var patch in patches.Postfixes)
                        {
                            Log.Warning($"            index={patch.index} owner={patch.owner} patchMethod={patch.PatchMethod} priority={patch.priority} before={patch.before} after={patch.after}");
                        }
                    }
                    if (patches.Transpilers.Count > 0)
                    {
                        Log.Warning($"        Transpilers:");
                        foreach (var patch in patches.Transpilers)
                        {
                            Log.Warning($"            index={patch.index} owner={patch.owner} patchMethod={patch.PatchMethod} priority={patch.priority} before={patch.before} after={patch.after}");
                        }
                    }
                    if (patches.Finalizers.Count > 0)
                    {
                        Log.Warning($"        Finalziers:");
                        foreach (var patch in patches.Finalizers)
                        {
                            Log.Warning($"            index={patch.index} owner={patch.owner} patchMethod={patch.PatchMethod} priority={patch.priority} before={patch.before} after={patch.after}");
                        }
                    }
                }
                else
                {
                    Log.Warning($"        Patches is null");
                }
            }

            Log.Warning($"    End of Check");
        }
    }
}
