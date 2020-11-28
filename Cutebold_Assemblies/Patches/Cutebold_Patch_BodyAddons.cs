using AlienRace;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Cutebold_Assemblies
{
    /// <summary>
    /// Harmony patches for controlling body part addons.
    /// </summary>
    class Cutebold_Patch_BodyAddons
    {
        /// <summary>If we already initialized the body addon patches.</summary>
        private static bool initialized = false;
        /// <summary>Copy of the body addons.</summary>
        private static List<AlienPartGenerator.BodyAddon> raceAddons;
        /// <summary>If dark adaptation are enabled.</summary>
        private static bool eyeAdaptation = true;
        /// <summary>If eye glow are enabled.</summary>
        private static bool glowEyes = true;
        /// <summary>If detachable parts are enabled.</summary>
        private static bool detachableParts = true;
        /// <summary>Reference to our harmony instance.</summary>
        private static Harmony harmonyRef;
        /// <summary>Reference to the CanDrawAddon method.</summary>
        private static System.Reflection.MethodBase canDrawAddonRef = AccessTools.Method(typeof(AlienPartGenerator.BodyAddon), "CanDrawAddon", null, null);
        /// <summary>Our prefix to the CanDrawAddon method.</summary>
        private static HarmonyMethod cuteboldCanDrawAddonPrefixRef = new HarmonyMethod(typeof(Cutebold_Patch_BodyAddons), "CuteboldCanDrawAddonPrefix", null);

        /// <summary>
        /// Enables/Disables body addons on startup.
        /// </summary>
        /// <param name="harmony">Our instance of harmony to patch with.</param>
        /// <param name="settings">Our list of saved settings.</param>
        public Cutebold_Patch_BodyAddons(Harmony harmony, Cutebold_Settings settings)
        {
            if (!initialized)
            {
                raceAddons = new List<AlienPartGenerator.BodyAddon>(Cutebold_Assemblies.AlienRaceDef.alienRace.generalSettings.alienPartGenerator.bodyAddons);
                harmonyRef = harmony;
                CuteboldAddonModifier(settings);
                harmonyRef.Patch(canDrawAddonRef, cuteboldCanDrawAddonPrefixRef, null, null, null);
                initialized = true;
            }
        }

        /// <summary>
        /// Handles disabling and enabling of the various body addons.
        /// </summary>
        /// <param name="settings">The settings to reference.</param>
        public static void CuteboldAddonModifier(Cutebold_Settings settings)
        {
            bool dirty = false;
            var currentAddons = Cutebold_Assemblies.AlienRaceDef.alienRace.generalSettings.alienPartGenerator.bodyAddons;

            if (settings.glowEyes != glowEyes || settings.eyeAdaptation != eyeAdaptation)
            {
                var canDrawAddonMethod = typeof(AlienPartGenerator.BodyAddon).GetMethod("CanDrawAddon");
                bool patched = false;

                if(Harmony.GetPatchInfo(canDrawAddonMethod).Prefixes.Any(patch => patch.owner == Cutebold_Assemblies.HarmonyID))
                    patched = true;

                glowEyes = settings.glowEyes;
                eyeAdaptation = settings.eyeAdaptation;

                if (glowEyes && eyeAdaptation && initialized)
                {
                    if (!patched)
                        harmonyRef.Patch(canDrawAddonRef, cuteboldCanDrawAddonPrefixRef, null, null, null);

                    foreach (var bodyAddon in raceAddons)
                    {
                        if ((bodyAddon.bodyPart == "left eye" || bodyAddon.bodyPart == "right eye") && !currentAddons.Contains(bodyAddon))
                            currentAddons.Add(bodyAddon);
                    }

                }
                else if (!glowEyes || !eyeAdaptation)
                {
                    if (patched)
                        harmonyRef.Unpatch(canDrawAddonMethod, typeof(Cutebold_Patch_BodyAddons).GetMethod("CuteboldCanDrawAddonPrefix"));

                    foreach (var bodyAddon in raceAddons)
                    {
                        if ((bodyAddon.bodyPart == "left eye" || bodyAddon.bodyPart == "right eye") && currentAddons.Contains(bodyAddon))
                            currentAddons.Remove(bodyAddon);
                    }
                }

                dirty = true;
            }

            if (settings.detachableParts != detachableParts)
            {
                detachableParts = settings.detachableParts;

                var graphicsPaths = Cutebold_Assemblies.AlienRaceDef.alienRace.graphicPaths.First();

                if (detachableParts && initialized)
                {
                    graphicsPaths.head = "Cutebold/Heads/";
                    graphicsPaths.body = "Cutebold/Bodies/";

                    foreach (var bodyAddon in raceAddons)
                    {
                        if (bodyAddon.bodyPart != "left eye" && bodyAddon.bodyPart != "right eye" && !currentAddons.Contains(bodyAddon))
                            currentAddons.Add(bodyAddon);
                    }
                }
                else if (!detachableParts)
                {
                    graphicsPaths.head = "Cutebold/Heads/Simple/";
                    graphicsPaths.body = "Cutebold/Bodies/Simple/";

                    foreach (var bodyAddon in raceAddons)
                    {
                        if (bodyAddon.bodyPart != "left eye" && bodyAddon.bodyPart != "right eye" && currentAddons.Contains(bodyAddon))
                            currentAddons.Remove(bodyAddon);
                    }
                }

                dirty = true;
            }

            if (dirty)
            {
                foreach (Pawn pawn in PawnsFinder.All_AliveOrDead)
                {
                    if (pawn.def.defName == Cutebold_Assemblies.RaceName)
                    {

                        pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
                    }
                }
            }
        }

        /// <summary>
        /// Checks a body addon if it should be drawn. This checks if the pawn is:
        /// - Dead
        /// - In a lit room
        /// - Asleep
        /// - Severely incompacitated.
        /// </summary>
        /// <param name="__result">If we should draw this addon.</param>
        /// <param name="pawn">The pawn the addon belongs to.</param>
        /// <returns>Returns true if we want to continue checking the addon in the regular method, false if we don't want to draw this addon.</returns>
        private static bool CuteboldCanDrawAddonPrefix(Pawn pawn, ref bool __result, AlienPartGenerator.BodyAddon __instance)
        {
            if (pawn.def.defName != Cutebold_Assemblies.RaceName || (__instance.bodyPart != "left eye" && __instance.bodyPart != "right eye")) return true;

            __result = true;
            
            if (pawn.Dead || 
                (pawn.CarriedBy == null ? pawn.Map.glowGrid.GameGlowAt(pawn.Position) : pawn.CarriedBy.Map.glowGrid.GameGlowAt(pawn.CarriedBy.Position)) >= 0.3f || 
                (pawn.CurJob != null && pawn.jobs.curDriver.asleep) || 
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight) == 0f || 
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) <= 0.1f)
            {
                __result = false;
            }

            return __result;
        }
    }
}
