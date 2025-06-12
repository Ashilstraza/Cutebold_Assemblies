#if !RWPre1_4
using Ashs_Alien_Patches;
#endif
using AlienRace;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using System.Reflection;

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
        private static HashSet<string> butcherRaceList;
        /// <summary>List of all the humanoid leathers.</summary>
        private static HashSet<ThingDef> humanoidLeathers;
        /// <summary>If we already have logged the butcher error.</summary>
        private static bool butcherLogged = false;
        /// <summary>If we already have logged the ingestor error.</summary>
        private static bool ingestedLogged = false;

        /// <summary>Cutebold alien race def.</summary>
        public static readonly ThingDef_AlienRace CuteboldRaceDef = (ThingDef_AlienRace)Cutebold_DefOf.Alien_Cutebold;
        /// <summary>Our mod name, for debug output purposes.</summary>
        public static readonly string ModName = "Cutebold Race Mod";
        /// <summary>Cutebold Harmony ID.</summary>
        public static readonly string HarmonyID = "rimworld.ashilstraza.races.cute.main";
        /// <summary>Reference to harmony.</summary>
        private static readonly Harmony harmony = new(HarmonyID);
        /// <summary>Reference to the settings.</summary>
        public static readonly Cutebold_Settings CuteboldSettings = null;


        /// <summary>
        /// Main constructor for setting up some values and executing harmony patches.
        /// </summary>
        static Cutebold_Assemblies()
        {
            CuteboldSettings = LoadedModManager.GetMod<CuteboldMod>().GetSettings<Cutebold_Settings>();

            BuildLists();

            Type thisClass = typeof(Cutebold_Assemblies);

            new Cutebold_Patch_Names(harmony);

            // Eating Humanoid Meat
            harmony.Patch(AccessTools.Method(typeof(Thing), nameof(Thing.Ingested)), postfix: new HarmonyMethod(thisClass, nameof(CuteboldIngestedPostfix)));
            // Butchering Humanoid Corpses
            harmony.Patch(AccessTools.Method(typeof(Corpse), nameof(Corpse.ButcherProducts)), postfix: new HarmonyMethod(thisClass, nameof(CuteboldButcherProductsPostfix)));
            // Wearing Humanoid Clothing
            harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_HumanLeatherApparel), "CurrentStateInternal"), postfix: new HarmonyMethod(thisClass, nameof(CuteboldCurrentStateInternalPostfix)));

            new Cutebold_Patch_Body(harmony);
            new Cutebold_Patch_Stats(harmony);
            new Cutebold_Patch_HediffRelated(harmony);

#if !RWPre1_4
            try
            {
                new Alien_Patches(harmony);
            }
            catch (Exception e)
            {
                Log.Error(string.Format($"{HarmonyID}: Exception when trying to apply Alien Patches. Ensure that you are using the latest version. If issues persist, please notify the author for the {ModName} with the logs. Thanks!\n{e}"));
            }
#endif

#if !RWPre1_5
            Alien_Patches.Alien_HotReloadAddMethod(BuildLists);
#endif
        }
        public static void BuildLists(bool hotReload = false)
        {
            CreateButcherRaceList();
            CreateHumanoidLeatherList();

            if (hotReload)
            {
                Cutebold_Patch_Body.HotReload();
            }
        }

        /// <summary>
        /// Creates a list of races that give bad butcher thoughts.
        /// </summary>
        private static void CreateButcherRaceList()
        {
            HashSet<string> butcherList = [];

#if RW1_1
            foreach (String race in CuteboldRaceDef.alienRace.thoughtSettings.butcherThoughtSpecific.FirstOrDefault().raceList)
            {
                butcherList.Add(race);
            }
#else
            foreach (ThingDef race in CuteboldRaceDef.alienRace.thoughtSettings.butcherThoughtSpecific.FirstOrDefault().raceList)
            {
                butcherList.Add(race.defName);
            }
#endif   

            butcherRaceList = butcherList;
        }

        /// <summary>
        /// Creates a list of humanoid leathers.
        /// </summary>
        private static void CreateHumanoidLeatherList()
        {
            HashSet<ThingDef> aliens = [];
            HashSet<ThingDef> animals = [];

            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs.Where(thingDef => thingDef.race?.leatherDef != null))
            {
                if (thingDef.race.Humanlike)
                {
                    aliens.Add(thingDef.race.leatherDef);
                }
                else
                {
                    // Ignore animals that have human leather as their type
                    if (thingDef.race.leatherDef.defName != "Leather_Human") animals.Add(thingDef.race.leatherDef);
                }
            }

            humanoidLeathers = [.. aliens.Except(animals).AsEnumerable()]; // We only want the list of leathers unique to humanoids.
        }

        /// <summary>
        /// Patch that checks to see if a cutebold has eaten something that others will have a bad opinion of.
        /// </summary>
        /// <param name="__result">The returned float of the Ingested method.</param>
        /// <param name="__instance">What was ingested.</param>
        /// <param name="ingester">Who ingested the thing.</param>
        /// <returns>Passes through the original float without modifying it.</returns>
        public static void CuteboldIngestedPostfix(ref Thing __instance, Pawn ingester)
        {
            //Log.Message("Ingested Postfix");
            //Log.Message("Instance: " + __instance.ToString() + " Instance Source: " + ((__instance.def.ingestible != null && __instance.def.ingestible.sourceDef != null) ? __instance.def.ingestible.sourceDef.defName.ToString() : "Null") + " ingester: " + ingester.ToString());
            if (ingester != null && __instance?.def.ingestible?.sourceDef != null)
            {
                try
                {
                    if (butcherRaceList.Contains(__instance.def.ingestible.sourceDef.defName))
                    {
                        TaleRecorder.RecordTale(Cutebold_DefOf.AteRawCuteboldMeatTale,
                        [
                        ingester
                        ]);
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
        public static void CuteboldButcherProductsPostfix(ref Corpse __instance, Pawn butcher)
        {
            //Log.Message("Butcher Products Postfix");
            //Log.Message("Instance: " +  __instance.ToString() + " Instance Source: " + ((__instance.def.ingestible != null && __instance.def.ingestible.sourceDef != null) ? __instance.def.ingestible.sourceDef.defName.ToString() : "Null") + " butcher: " + butcher.ToString());
            if (butcher != null && __instance?.def.ingestible?.sourceDef != null)
            {
                try
                {
                    if (butcherRaceList.Contains(__instance.def.ingestible.sourceDef.defName))
                    {
                        TaleRecorder.RecordTale(Cutebold_DefOf.ButcheredCuteboldCorpseTale,
                        [
                    butcher
                        ]);
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
        public static void CuteboldCurrentStateInternalPostfix(ref ThoughtState __result, Pawn p)
        {
#if RWPre1_3
            if (p?.def != CuteboldRaceDef) return; // We only want to apply to cutebolds.
#else
            if (p?.def != CuteboldRaceDef || p?.Ideo == null) return; // We only want to apply to cutebolds.
#endif

            // Pretty much copied from the regular code.
            string text = null;
            int num = 0;
            ThoughtState newThoughtState;
            foreach (Apparel apparel in p.apparel.WornApparel)
            {
                if (humanoidLeathers.Contains(apparel.Stuff))
                {
                    text ??= apparel.def.label;
                    num++;
                }
            }

            if (num == 0) newThoughtState = ThoughtState.Inactive;
            else if (num >= 5) newThoughtState = ThoughtState.ActiveAtStage(4, text);
            else newThoughtState = ThoughtState.ActiveAtStage(num - 1, text);

            __result = newThoughtState;
        }

        /// <summary>
        /// Runs a check on all the methods we patch and outputs all the patches for those methods regardless of which mod it came from.
        /// </summary>
        public static void CheckPatchedMethods(bool allMethods = false)
        {
            StringBuilder stringBuilder = new();

            IEnumerable<MethodBase> patchedMethods = allMethods ? Harmony.GetAllPatchedMethods() : harmony.GetPatchedMethods();

            writeLine($"{ModName}: Checking Patched Methods...\n", ref stringBuilder);

            foreach (MethodBase method in patchedMethods)
            {
                HarmonyLib.Patches patches = Harmony.GetPatchInfo(method);

                writeLine($"    {method.Name}", ref stringBuilder);

                if (patches != null)
                {
                    if (patches.Prefixes.Count > 0)
                    {
                        writeLine("        Prefixes:".Colorize(Color.blue), ref stringBuilder);

                        foreach (Patch patch in patches.Prefixes)
                            writeLine(patchOutput(patch, $"index={patch.index} owner={patch.owner} patchMethod={patch.PatchMethod} priority={patch.priority} before={patch.before} after={patch.after}").Colorize(patch.owner == HarmonyID ? Color.green : Color.white), ref stringBuilder);
                    }
                    if (patches.Postfixes.Count > 0)
                    {
                        writeLine("        Postfixes:".Colorize(Color.cyan), ref stringBuilder);

                        foreach (Patch patch in patches.Postfixes)
                            writeLine(patchOutput(patch, $"index={patch.index} owner={patch.owner} patchMethod={patch.PatchMethod} priority={patch.priority} before={patch.before} after={patch.after}").Colorize(patch.owner == HarmonyID ? Color.green : Color.white), ref stringBuilder);
                    }
                    if (patches.Transpilers.Count > 0)
                    {
                        writeLine("        Transpilers:".Colorize(Color.magenta), ref stringBuilder);

                        foreach (Patch patch in patches.Transpilers)
                            writeLine(patchOutput(patch, $"index={patch.index} owner={patch.owner} patchMethod={patch.PatchMethod} priority={patch.priority} before={patch.before} after={patch.after}").Colorize(patch.owner == HarmonyID ? Color.green : Color.white), ref stringBuilder);
                    }
                    if (patches.Finalizers.Count > 0)
                    {
                        writeLine("        Finalziers:".Colorize(new Color(1f, 0.6f, 0f, 1f)), ref stringBuilder);

                        foreach (Patch patch in patches.Finalizers)
                            writeLine(patchOutput(patch, $"index={patch.index} owner={patch.owner} patchMethod={patch.PatchMethod} priority={patch.priority} before={patch.before} after={patch.after}").Colorize(patch.owner == HarmonyID ? Color.green : Color.white), ref stringBuilder);
                    }
                }
                else
                {
                    writeLine("Error: Patches is null".Colorize(Color.red), ref stringBuilder);
                }
            }

            writeLine("    End of Check", ref stringBuilder);

            Log.Message(stringBuilder.ToString().TrimEndNewlines());

            static string patchOutput(Patch patch, string patchText)
            {
                if (patch.owner == HarmonyID) return "Cb        " + patchText;
                else return "            " + patchText;
            }

            void writeLine(string text, ref StringBuilder stringBuilder)
            {
                if (!allMethods) Log.Message(text);
                stringBuilder.AppendLine(text.StripTags());
            }
        }
    }
}
