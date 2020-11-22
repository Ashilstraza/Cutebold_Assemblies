using AlienRace;
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

        /// <summary>
        /// Enables/Disables body addons on startup.
        /// </summary>
        /// <param name="settings">Our list of saved settings.</param>
        public Cutebold_Patch_BodyAddons(Cutebold_Settings settings)
        {
            if (!initialized)
            {
                raceAddons = new List<AlienPartGenerator.BodyAddon>(Cutebold_Assemblies.AlienRaceDef.alienRace.generalSettings.alienPartGenerator.bodyAddons);
                CuteboldAddonModifier(settings);
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
                glowEyes = settings.glowEyes;
                eyeAdaptation = settings.eyeAdaptation;

                if (glowEyes && eyeAdaptation && initialized)
                {
                    foreach (var bodyAddon in raceAddons)
                    {
                        if ((bodyAddon.bodyPart == "left eye" || bodyAddon.bodyPart == "right eye") && !currentAddons.Contains(bodyAddon))
                            currentAddons.Add(bodyAddon);
                    }

                }
                else if (!glowEyes || !eyeAdaptation)
                {
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

            if(dirty) UpdatePawns();
        }

        /// <summary>
        /// Iterates over all the pawns and updates the hediff eye glow references and sets the graphics as dirty so they regenerate.
        /// </summary>
        private static void UpdatePawns()
        {
            foreach (Pawn pawn in PawnsFinder.All_AliveOrDead)
            {
                if (pawn.def.defName == Cutebold_Assemblies.RaceName)
                {
                    Hediff_CuteboldDarkAdaptation hediff = (Hediff_CuteboldDarkAdaptation)pawn.health.hediffSet.GetFirstHediffOfDef(Cutebold_DefOf.CuteboldDarkAdaptation);

                    if (eyeAdaptation && hediff != null)
                    {
                        foreach (var bodyAddon in raceAddons)
                        {
                            if (bodyAddon.bodyPart == "left eye" && bodyAddon.ColorChannel == "eye") hediff.leftEyeGlow = glowEyes ? bodyAddon : null;
                            if (bodyAddon.bodyPart == "right eye" && bodyAddon.ColorChannel == "eye") hediff.rightEyeGlow = glowEyes ? bodyAddon : null;
                        }
                    }

                    pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
                }
            }
        }
    }
}
