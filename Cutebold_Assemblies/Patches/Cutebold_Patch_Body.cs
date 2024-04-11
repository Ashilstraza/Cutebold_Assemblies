#if RW1_5
using AlienRace.ExtendedGraphics;
#elif RW1_4
using UnityEngine;
#endif

using AlienRace;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Verse;
using static AlienRace.AlienPartGenerator;

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
#if RWPre1_5
        /// <summary>Reference to the CanDrawAddon method.</summary>
        private static readonly MethodBase canDrawAddonRef = AccessTools.Method(typeof(AlienPartGenerator.BodyAddon), nameof(AlienPartGenerator.BodyAddon.CanDrawAddon), new[] {
            typeof(Pawn)
        });
#endif
        /// <summary>Our prefix to the CanDrawAddon method.</summary>
        private static readonly HarmonyMethod cuteboldCanDrawAddonPrefixRef = new HarmonyMethod(typeof(Cutebold_Patch_Body), nameof(CuteboldCanDrawAddonPrefix));

        /// <summary>What kind of modification we want to do to the addons.</summary>
        private enum Modification : byte
        {
            Add,
            Remove
        }

        /// <summary>Dictionary with the various paths.</summary>
        private static readonly Dictionary<String, String> paths = new Dictionary<string, string>(){
            {"headPath",  "Cutebold/Heads/"},
            {"simpleHeadPath", "Cutebold/Heads/Simple/"},
            {"bodyPath", "Cutebold/Bodies/"},
            {"simpleBodyPath", "Cutebold/Bodies/Simple/"}
        };


        /// <summary>If eye blinking is enabled.</summary>
        public static bool EyeBlink => Cutebold_Assemblies.CuteboldSettings.blinkEyes;

        /// <summary>
        /// Enables/Disables body addons on startup.
        /// </summary>
        /// <param name="harmony">Our instance of harmony to patch with.</param>
        public Cutebold_Patch_Body(Harmony harmony)
        {
            if (!initialized)
            {
                raceAddons = new List<BodyAddon>(Cutebold_Assemblies.CuteboldRaceDef.alienRace.generalSettings.alienPartGenerator.bodyAddons);
                harmonyRef = harmony;
                CuteboldAddonModifier(Cutebold_Assemblies.CuteboldSettings);
#if RWPre1_5
                harmonyRef.Patch(canDrawAddonRef, prefix: cuteboldCanDrawAddonPrefixRef);
#endif

#if RW1_4
                // Bodytype fixes
                harmony.Patch(AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.CheckBodyType)), postfix: new HarmonyMethod(typeof(Cutebold_Patch_Body), nameof(Cutebold_CheckBodyType_Postfix)));
                harmony.Patch(AccessTools.Method(typeof(PawnGraphicSet), nameof(PawnGraphicSet.ResolveAllGraphics)), postfix: new HarmonyMethod(typeof(Cutebold_Patch_Body), nameof(Cutebold_ResolveAllGraphics_Postfix)));
#endif
                initialized = true;
            }
        }

        public static void HotReload()
        {
            raceAddons = new List<BodyAddon>(Cutebold_Assemblies.CuteboldRaceDef.alienRace.generalSettings.alienPartGenerator.bodyAddons);
            CuteboldAddonModifier(Cutebold_Assemblies.CuteboldSettings);
        }

        /// <summary>
        /// Handles disabling and enabling of the various body addons.
        /// </summary>
        /// <param name="settings">The settings to reference.</param>
        public static void CuteboldAddonModifier(Cutebold_Settings settings)
        {
            List<BodyAddon> currentAddons = Cutebold_Assemblies.CuteboldRaceDef.alienRace.generalSettings.alienPartGenerator.bodyAddons;

            if (UpdateEyes(settings, ref currentAddons) || UpdateBodiesAndHeads(settings, ref currentAddons))
            {
                foreach (Pawn pawn in PawnsFinder.All_AliveOrDead)
                {
                    if (pawn.def == Cutebold_Assemblies.CuteboldRaceDef)
                    {
                        SetDirty(pawn);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the given pawn's graphics as dirty to have it redrawn.
        /// </summary>
        /// <param name="pawn">The pawn to dirty, leave empty to set all pawns dirty.</param>
        public static void SetDirty(Pawn pawn = null)
        {
            if(pawn == null)
            {
                foreach (Pawn p in PawnsFinder.All_AliveOrDead)
                {
                    SetDirty(p);
                }
            }
#if RWPre1_3
            pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
#elif RWPre1_5
            pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
            GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);
#else
            pawn.Drawer.renderer.renderTree.SetDirty();
            GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);
#endif
        }

        /// <summary>
        /// Updates eye addons.
        /// </summary>
        /// <param name="settings">The current settings</param>
        /// <param name="currentAddons"> reference to the list of current addons.</param>
        /// <returns>True if we need to redraw the cache for cutebolds.</returns>
        private static bool UpdateEyes(Cutebold_Settings settings, ref List<BodyAddon> currentAddons)
        {
            if (settings.glowEyes != glowEyes || settings.eyeAdaptation != eyeAdaptation)
            {
                MethodInfo canDrawAddonMethod = typeof(BodyAddon).GetMethod("CanDrawAddon");
                bool patched = false;

                glowEyes = settings.glowEyes;
                eyeAdaptation = settings.eyeAdaptation;

                if (initialized && Harmony.GetPatchInfo(canDrawAddonMethod).Prefixes.Any(patch => patch.owner == Cutebold_Assemblies.HarmonyID))
                    patched = true;

                if (glowEyes && eyeAdaptation && initialized)
                {
                    if (!patched)
#if RWPre1_5
                        harmonyRef.Patch(canDrawAddonRef, prefix: cuteboldCanDrawAddonPrefixRef);
#endif

                    foreach (BodyAddon bodyAddon in raceAddons)
                    {
                        CheckModifyPart(true, ref currentAddons, bodyAddon, Modification.Add);
                    }

                }
                else if (!glowEyes || !eyeAdaptation)
                {
                    if (patched)
#if RWPre1_5
                        harmonyRef.Unpatch(canDrawAddonRef, cuteboldCanDrawAddonPrefixRef.method);
#endif

                    foreach (BodyAddon bodyAddon in raceAddons)
                    {
                        CheckModifyPart(true, ref currentAddons, bodyAddon, Modification.Remove);
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates body and head addons that are not eyes.
        /// </summary>
        /// <param name="settings">The current settings</param>
        /// <param name="currentAddons"> reference to the list of current addons.</param>
        /// <returns>True if we need to redraw the cache for cutebolds.</returns>
        private static bool UpdateBodiesAndHeads(Cutebold_Settings settings, ref List<BodyAddon> currentAddons)
        {
            if (settings.detachableParts != detachableParts)
            {
                detachableParts = settings.detachableParts;

                SetGraphicPaths(detachableParts);

                if (detachableParts && initialized)
                {
                    foreach (BodyAddon bodyAddon in raceAddons)
                    {
                        CheckModifyPart(false, ref currentAddons, bodyAddon, Modification.Add);
                    }
                }
                else if (!detachableParts)
                {
                    foreach (BodyAddon bodyAddon in raceAddons)
                    {
                        CheckModifyPart(false, ref currentAddons, bodyAddon, Modification.Remove);
                    }
                }

                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Updates the graphic paths depending on version
        /// </summary>
        /// <param name="detachableParts">If we want parts to be visually detached</param>
        private static void SetGraphicPaths(bool detachableParts = true)
        {
            GraphicPaths graphicsPaths;
#if RWPre1_4
            graphicsPaths = Cutebold_Assemblies.CuteboldRaceDef.alienRace.graphicPaths.First();
            graphicsPaths.head = detachableParts ? paths["headPath"] : paths["simpleHeadPath"];
            graphicsPaths.body = detachableParts ? paths["bodyPath"] : paths["simpleBodyPath"];
#else
            graphicsPaths = Cutebold_Assemblies.CuteboldRaceDef.alienRace.graphicPaths;
            graphicsPaths.head.path = detachableParts ? paths["headPath"] : paths["simpleHeadPath"];
            graphicsPaths.body.path = detachableParts ? paths["bodyPath"] : paths["simpleBodyPath"];

            //Handle body and head variations
            UpdateBodyGraphics(ref graphicsPaths);
            UpdateHeadGraphics(ref graphicsPaths);
#endif
        }

        /// <summary>
        /// Handles version dependant modification of body addons
        /// </summary>
        /// <param name="forEyes">If we are messing with the eyes</param>
        /// <param name="currentAddons">List of the current body addons</param>
        /// <param name="bodyAddon">Current body addon we want to modify</param>
        /// <param name="modify">If we should add or remove body parts</param>
        private static void CheckModifyPart(bool forEyes, ref List<BodyAddon> currentAddons, BodyAddon bodyAddon, Modification modify)
        {
            bool shouldModify;

            if (forEyes)
            {
#if RWPre1_4
                shouldModify = bodyAddon.bodyPart == "left eye" || bodyAddon.bodyPart == "right eye";
#elif RW1_4
                    shouldModify = bodyAddon.bodyPart.defName == "Eye";
#else
                    shouldModify = ((ConditionBodyPart)bodyAddon.conditions.Find(condition => condition is ConditionBodyPart))?.bodyPart.defName == "Eye";
#endif
            }
            else
            {
#if RWPre1_4
                shouldModify = bodyAddon.bodyPart != "left eye" && bodyAddon.bodyPart != "right eye";
#elif RW1_4
                    shouldModify = bodyAddon.bodyPart.defName != "Eye";
#else
                    shouldModify = ((ConditionBodyPart)bodyAddon.conditions.Find(condition => condition is ConditionBodyPart))?.bodyPart.defName != "Eye";
#endif
            }
            if (modify == Modification.Add)
            {
                if (shouldModify && !currentAddons.Contains(bodyAddon))
                    currentAddons.Add(bodyAddon);
            }
            else if (modify == Modification.Remove)
            {
                if (shouldModify && currentAddons.Contains(bodyAddon))
                    currentAddons.Remove(bodyAddon);
            }
        }

        /// <summary>
        /// Used Pre 1.5
        /// Checks a body addon if it should be drawn. This checks if the pawn is:
        /// - Dead
        /// - In a lit room
        /// - Asleep
        /// - Severely incompacitated.
        /// </summary>
        /// <param name="__result">If we should draw this addon.</param>
        /// <param name="pawn">The pawn the addon belongs to.</param>
        /// <returns>Returns true if we want to continue checking the addon in the regular method, false if we don't want to draw this addon.</returns>
        private static bool CuteboldCanDrawAddonPrefix(Pawn pawn, ref bool __result, BodyAddon __instance)
        {
#if RWPre1_4
            if (pawn.def != Cutebold_Assemblies.CuteboldRaceDef || (__instance.bodyPart != "left eye" && __instance.bodyPart != "right eye")) return true;
#elif RW1_4
            if (pawn.def != Cutebold_Assemblies.CuteboldRaceDef || __instance.bodyPart.defName != "Eye") return true;
#else
            return __result;
#endif
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CS0162 // Unreachable code detected
            __result = true;

#pragma warning restore CS0162 // Unreachable code detected
#pragma warning restore IDE0079 // Remove unnecessary suppression

            if (pawn.Dead ||
                Cutebold_Patch_HediffRelated.CuteboldGlowHandler(pawn) >= 0.3f ||
                (pawn.CurJob != null && pawn.jobs.curDriver.asleep) ||
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight) == 0f ||
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) <= 0.1f)
            {
                __result = false;
            }
            else if (EyeBlink)
            {
                // Blink Fucntion; somewhat regular blinking, but not exactly even nor completely random.
                int offsetTicks = Math.Abs(pawn.HashOffsetTicks());
                if (Math.Abs((offsetTicks % 182) / 1.8 - Math.Abs(80 * Math.Sin(offsetTicks / 89))) < 1) __result = false;
            }


            return __result;
        }
#if RWPre1_4
    } // Close Cutebold_Patch_Body Class
#endif

        #region 1.4 Only Code
#if RW1_4
        /// <summary>
        /// Updates the paths for the body graphic variations
        /// </summary>
        /// <param name="graphicsPaths">Reference to the graphic paths.</param>
        private static void UpdateBodyGraphics(ref GraphicPaths graphicsPaths)
        {
            foreach (var body in graphicsPaths.body.bodytypeGraphics)
            {
                body.path = $"{graphicsPaths.body.path}Naked_{body.bodytype}";

                foreach (var genderedBody in body.genderGraphics)
                {
                    genderedBody.path = $"{graphicsPaths.body.path}{genderedBody.gender}Naked_{body.bodytype}";
                }
                body.paths = new List<string>() { body.path };
            }
        }

        /// <summary>
        /// Updates the paths for the head graphic variations
        /// </summary>
        /// <param name="graphicsPaths">Reference to the graphic paths.</param>
        private static void UpdateHeadGraphics(ref GraphicPaths graphicsPaths)
        {
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
        }

        public static void Cutebold_CheckBodyType_Postfix(Pawn pawn, ref BodyTypeDef __result)
        {
            if (pawn.def != Cutebold_Assemblies.CuteboldRaceDef || __result == BodyTypeDefOf.Child || __result == BodyTypeDefOf.Baby) return;

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
            if (__instance.pawn.def != Cutebold_Assemblies.CuteboldRaceDef) return;
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
    } // Close Cutebold_Patch_Body Class
#endif
        #endregion

        #region 1.5 Only Code
#if RW1_5
        /// <summary>
        /// Updates the paths for the body graphic variations
        /// </summary>
        /// <param name="graphicsPaths">Reference to the graphic paths.</param>
        private static void UpdateBodyGraphics(ref GraphicPaths graphicsPaths)
        {
            foreach (ExtendedConditionGraphic bodyType in graphicsPaths.body.extendedGraphics.Cast<ExtendedConditionGraphic>())
            {
                ConditionBodyType bodyTypeCondition = (ConditionBodyType)bodyType.conditions.Find(body => body is ConditionBodyType);
                bodyType.path = $"{graphicsPaths.body.path}Naked_{(bodyTypeCondition.bodyType == BodyTypeDefOf.Baby ? BodyTypeDefOf.Child : bodyTypeCondition.bodyType)}";

                foreach (ExtendedConditionGraphic genderedBody in bodyType.extendedGraphics.Cast<ExtendedConditionGraphic>())
                {
                    ConditionGender genderCondition = (ConditionGender)genderedBody.conditions.Find(gender => gender is ConditionGender);
                    genderedBody.path = $"{graphicsPaths.body.path}{genderCondition.gender}_Naked_{(bodyTypeCondition.bodyType == BodyTypeDefOf.Baby ? BodyTypeDefOf.Child : bodyTypeCondition.bodyType)}";
                }
                bodyType.paths = new List<string>() { bodyType.path }; //TODO Fix this maybe?

            }
        }

        /// <summary>
        /// Updates the paths for the head graphic variations
        /// </summary>
        /// <param name="graphicsPaths">Reference to the graphic paths.</param>
        private static void UpdateHeadGraphics(ref GraphicPaths graphicsPaths)
        {
            foreach (ExtendedConditionGraphic headType in graphicsPaths.head.extendedGraphics.Cast<ExtendedConditionGraphic>())
            {
                headType.path = graphicsPaths.head.path + System.IO.Path.GetFileName(headType.path);

                foreach (ExtendedConditionGraphic generedHead in headType.extendedGraphics.Cast<ExtendedConditionGraphic>())
                {
                    generedHead.path = graphicsPaths.head.path + System.IO.Path.GetFileName(generedHead.path);
                }
                headType.paths = new List<string>() { headType.path }; //TODO Fix this maybe?
            }
        }
    } // Close Cutebold_Patch_Body Class
    public class CuteboldEyeBlink : Condition
    {
        public new const string XmlNameParseKey = "CuteboldBlink";

        public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
        {
            Pawn p = pawn.WrappedPawn;
            if (p.Dead ||
                Cutebold_Patch_HediffRelated.CuteboldGlowHandler(p) >= 0.3f ||
                (pawn.CurJob != null && p.jobs.curDriver.asleep) ||
                p.health.capacities.GetLevel(PawnCapacityDefOf.Sight) == 0f ||
                p.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) <= 0.1f)
            {
                return false;
            }
            else if (Cutebold_Patch_Body.EyeBlink)
            {
                // Blink Fucntion; somewhat regular blinking, but not exactly even nor completely random.
                int offsetTicks = Math.Abs(p.HashOffsetTicks());
                if (Math.Abs((offsetTicks % 182) / 1.8 - Math.Abs(80 * Math.Sin(offsetTicks / 89))) < 1) return false;
            }

            return true;
        } 
    }
#endif
#endregion
}
