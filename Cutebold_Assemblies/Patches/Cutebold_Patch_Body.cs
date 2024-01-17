using AlienRace;
using DubsBadHygiene;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using UnityEngine;
using Verse;

namespace Cutebold_Assemblies
{
    /// <summary>
    /// Harmony patches for controlling body part addons.
    /// </summary>
    class Cutebold_Patch_Body
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
        private static readonly MethodBase canDrawAddonRef = AccessTools.Method(typeof(AlienPartGenerator.BodyAddon), nameof(AlienPartGenerator.BodyAddon.CanDrawAddon), new[] {
            typeof(Pawn)
        });

        /// <summary>Our prefix to the CanDrawAddon method.</summary>
        private static readonly HarmonyMethod cuteboldCanDrawAddonPrefixRef = new HarmonyMethod(typeof(Cutebold_Patch_Body), nameof(CuteboldCanDrawAddonPrefix));
        /// <summary>If eye blinking is enabled.</summary>
        private static bool eyeBlink => Cutebold_Assemblies.CuteboldSettings.blinkEyes;

        /// <summary>
        /// Enables/Disables body addons on startup.
        /// </summary>
        /// <param name="harmony">Our instance of harmony to patch with.</param>
        public Cutebold_Patch_Body(Harmony harmony)
        {
            if (!initialized)
            {
                raceAddons = new List<AlienPartGenerator.BodyAddon>(Cutebold_Assemblies.AlienRaceDef.alienRace.generalSettings.alienPartGenerator.bodyAddons);
                harmonyRef = harmony;
                CuteboldAddonModifier(Cutebold_Assemblies.CuteboldSettings);
                harmonyRef.Patch(canDrawAddonRef, prefix: cuteboldCanDrawAddonPrefixRef);
#if RW1_4
                //harmonyRef.Patch(AccessTools.Method(typeof(HarmonyPatches), "DrawAddonsFinalHook"), postfix: new HarmonyMethod(typeof(Cutebold_Patch_BodyAddons), "CuteboldDrawAddonsFinalHookPostfix"));
                
                // Bodytype fixes
                harmony.Patch(AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.CheckBodyType)), postfix: new HarmonyMethod(typeof(Cutebold_Patch_Body), nameof(Cutebold_CheckBodyType_Postfix)));
                harmony.Patch(AccessTools.Method(typeof(PawnGraphicSet), nameof(PawnGraphicSet.ResolveAllGraphics)), postfix: new HarmonyMethod(typeof(Cutebold_Patch_Body), nameof(Cutebold_ResolveAllGraphics_Postfix)));
#endif

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


                if (initialized && Harmony.GetPatchInfo(canDrawAddonMethod).Prefixes.Any(patch => patch.owner == Cutebold_Assemblies.HarmonyID))
                    patched = true;

                glowEyes = settings.glowEyes;
                eyeAdaptation = settings.eyeAdaptation;

                if (glowEyes && eyeAdaptation && initialized)
                {
                    if (!patched)
                        harmonyRef.Patch(canDrawAddonRef, cuteboldCanDrawAddonPrefixRef, null, null, null);

                    foreach (var bodyAddon in raceAddons)
                    {
#if RWPre1_4
                        if ((bodyAddon.bodyPart == "left eye" || bodyAddon.bodyPart == "right eye") && !currentAddons.Contains(bodyAddon))
                            currentAddons.Add(bodyAddon);
#else
                        if (bodyAddon.bodyPart.defName != "Eye" && !currentAddons.Contains(bodyAddon))
                            currentAddons.Add(bodyAddon);
#endif
                    }

                }
                else if (!glowEyes || !eyeAdaptation)
                {
                    if (patched)
                        harmonyRef.Unpatch(canDrawAddonMethod, typeof(Cutebold_Patch_Body).GetMethod("CuteboldCanDrawAddonPrefix"));

                    foreach (var bodyAddon in raceAddons)
                    {
#if RWPre1_4
                        if ((bodyAddon.bodyPart == "left eye" || bodyAddon.bodyPart == "right eye") && currentAddons.Contains(bodyAddon))
                            currentAddons.Remove(bodyAddon);
#else
                        if (bodyAddon.bodyPart.defName != "Eye" && !currentAddons.Contains(bodyAddon))
                            currentAddons.Add(bodyAddon);
#endif
                    }
                }

                dirty = true;
            }
#if RWPre1_4
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
#else
            var graphicsPaths = Cutebold_Assemblies.AlienRaceDef.alienRace.graphicPaths;

            if (settings.detachableParts != detachableParts)
            {
                detachableParts = settings.detachableParts;

                if (detachableParts && initialized)
                {
                    graphicsPaths.head.path = "Cutebold/Heads/";
                    graphicsPaths.body.path = "Cutebold/Bodies/";

                    foreach (var bodyAddon in raceAddons)
                    {
                        if (bodyAddon.bodyPart.defName != "Eye" && !currentAddons.Contains(bodyAddon))
                            currentAddons.Add(bodyAddon);
                    }
                }
                else if (!detachableParts)
                {
                    graphicsPaths.head.path = "Cutebold/Heads/Simple/";
                    graphicsPaths.body.path = "Cutebold/Bodies/Simple/";

                    foreach (var bodyAddon in raceAddons)
                    {
                        if (bodyAddon.bodyPart.defName != "Eye" && currentAddons.Contains(bodyAddon))
                            currentAddons.Remove(bodyAddon);
                    }
                }

                // Mimics the way AlienRace builds the body and head graphic paths

                foreach (var body in graphicsPaths.body.bodytypeGraphics)
                {
                    body.path = $"{graphicsPaths.body.path}Naked_{body.bodytype}";

                    foreach (var genderedBody in body.genderGraphics)
                    {
                        genderedBody.path = $"{graphicsPaths.body.path}{genderedBody.gender}Naked_{body.bodytype}";
                    }
                    body.paths = new List<string>() { body.path };

                }

                foreach (var head in graphicsPaths.head.headtypeGraphics)
                {
                    var headTypePath = System.IO.Path.GetFileName(head.headType.graphicPath);
                    head.path = graphicsPaths.head.path + headTypePath.Substring(headTypePath.IndexOf('_') + 1);

                    foreach (var generedHead in head.genderGraphics)
                    {
                        generedHead.path = graphicsPaths.head.path + headTypePath;
                    }
                    head.paths = new List<string>() { head.path };
                }

                dirty = true;
            }
#endif
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
#if RWPre1_4
            if (pawn.def.defName != Cutebold_Assemblies.RaceName || (__instance.bodyPart != "left eye" && __instance.bodyPart != "right eye")) return true;
#else
            if (pawn.def.defName != Cutebold_Assemblies.RaceName || __instance.bodyPart.defName != "Eye") return true;
#endif
            __result = true;

            if (pawn.Dead ||
                ((pawn.ParentHolder as Map) != null ? pawn.Map.glowGrid.GameGlowAt(pawn.Position) : (pawn.CarriedBy != null ? pawn.CarriedBy.Map.glowGrid.GameGlowAt(pawn.CarriedBy.Position) : 0.5f)) >= 0.3f ||
                (pawn.CurJob != null && pawn.jobs.curDriver.asleep) ||
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight) == 0f ||
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) <= 0.1f)
            {
                __result = false;
            }
            else if (eyeBlink)
            {
                // Blink Fucntion; somewhat regular blinking, but not exactly even nor completely random.
                var offsetTicks = Math.Abs(pawn.HashOffsetTicks());
                if (Math.Abs((offsetTicks % 182) / 1.8 - Math.Abs(80 * Math.Sin(offsetTicks / 89))) < 1) __result = false;
            }

            return __result;
        }

        public static void Cutebold_CheckBodyType_Postfix(Pawn pawn, ref BodyTypeDef __result)
        {
            if (pawn.def.defName != Cutebold_Assemblies.RaceName || __result == BodyTypeDefOf.Child || __result == BodyTypeDefOf.Baby) return;

            BodyTypeDef replacementBodyType = __result;

            // Cutebold body types can normally be either thin or feminine
            if (pawn.gender == Gender.Male && __result != BodyTypeDefOf.Female && pawn.story.Childhood.defName.StartsWith("Cutebold")) replacementBodyType = BodyTypeDefOf.Thin;
            if (pawn.gender == Gender.Female && __result != BodyTypeDefOf.Thin && pawn.story.Childhood.defName.StartsWith("Cutebold")) replacementBodyType = BodyTypeDefOf.Female;

            List<Gene> genesListForReading = pawn.genes.GenesListForReading;
            HashSet<BodyTypeDef> geneBodyTypes = new HashSet<BodyTypeDef>();

            for (int index = 0; index < genesListForReading.Count; ++index)
            {
                if (genesListForReading[index].def.bodyType.HasValue)
                {
                    var bodyTypeTemp = genesListForReading[index].def.bodyType.Value.ToBodyType(pawn);
                    switch (bodyTypeTemp.defName)
                    {
                        case "Male":
                            geneBodyTypes.Add(pawn.story.Childhood.defName.StartsWith("Cutebold") ? BodyTypeDefOf.Thin : bodyTypeTemp);
                            break;
                        default: geneBodyTypes.Add(bodyTypeTemp); break;


                    }
                }
            }

            BodyTypeDef randomGeneBody;
            if (geneBodyTypes.TryRandomElement<BodyTypeDef>(out randomGeneBody))
                replacementBodyType = randomGeneBody;

            __result = replacementBodyType;
        }

        public static Vector2 Cutebold_MaleDrawSize_Adjust = new Vector2(-0.2f, 0.0f);
        public static Vector2 Cutebold_FatDrawSize_Adjust = new Vector2(-0.2f, 0.0f);
        public static Vector2 Cutebold_HulkDrawSize_Adjust = new Vector2(-0.2f, -0.1f);

        public static void Cutebold_ResolveAllGraphics_Postfix(PawnGraphicSet __instance)
        {
            if (__instance.pawn.def.defName != Cutebold_Assemblies.RaceName) return;
            AlienPartGenerator.AlienComp alienComp = __instance.pawn.GetComp<AlienPartGenerator.AlienComp>();
            switch (__instance.pawn.story.bodyType.defName)
            {
                case "Male":
                    alienComp.customDrawSize += Cutebold_MaleDrawSize_Adjust;
                    alienComp.customPortraitDrawSize += Cutebold_MaleDrawSize_Adjust;
                    return;
                case "Fat":
                    alienComp.customDrawSize += Cutebold_FatDrawSize_Adjust;
                    alienComp.customPortraitDrawSize += Cutebold_FatDrawSize_Adjust;
                    return;
                case "Hulk":
                    alienComp.customDrawSize += Cutebold_HulkDrawSize_Adjust;
                    alienComp.customPortraitDrawSize += Cutebold_HulkDrawSize_Adjust;
                    return;
                default: return;
            }
        }
#if RW1_4
        /// <summary>
        /// Custom fixes for body addons
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="ba"></param>
        /// <param name="rot"></param>
        /// <param name="addonGraphic"></param>
        /// <param name="offsetVector"></param>
        /// <param name="angle"></param>
        /// <param name="mat"></param>
        private static void CuteboldDrawAddonsFinalHookPostfix(Pawn pawn, AlienPartGenerator.BodyAddon addon, Rot4 rot, ref Graphic graphic, ref Vector3 offsetVector, ref float angle, ref Material mat)
        {
        }
#endif
    }
}
