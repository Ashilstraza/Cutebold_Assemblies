using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

#if !RWPre1_4
namespace Cutebold_Assemblies.Patches
{
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

            Alien_AddRaceToPatch(Cutebold_Assemblies.RaceName);

            string alienRaceID = "rimworld.erdelf.alien_race.main";
            //StringBuilder stringBuilder = new StringBuilder("Temporary patches to allow for custom racial life stages, provided by the Cutebold Mod.\nIf you want your race to use these patches, call Cutebold_Assemblies.Patches.Alien_Patches.Alien_AddRaceToPatch() with the string of your race def to add them!\n\n(This may disappear when HAR adds this ability.)");
            StringBuilder stringBuilder = new StringBuilder();
            bool patches = false;

            if (!Harmony.GetPatchInfo(AccessTools.Method(typeof(IncidentWorker_Disease), "CanAddHediffToAnyPartOfDef"))?.Prefixes?.Any(patch => patch.owner == alienRaceID) ?? true)
            {
                stringBuilder.AppendLine("  Patching IncidentWorker_Disease.CanAddHediffToAnyPartOfDef");
                harmony.Patch(incidentWorker_Disease_CanAddHediffToAnyPartOfDef, prefix: new HarmonyMethod(thisClass, "Alien_CanAddHediffToAnyPartOfDef_Prefix"));
                patches = true;
            }

            //Optional
            if (!Harmony.GetPatchInfo(AccessTools.Method(typeof(ITab_Pawn_Gear), "get_IsVisible"))?.Prefixes?.Any(patch => patch.owner == alienRaceID) ?? true)
            {
                stringBuilder.AppendLine("  Patching ITab_Pawn_Gear.get_IsVisible");
                harmony.Patch(iTab_Pawn_Gear_IsVisible, prefix: new HarmonyMethod(thisClass, "Alien_get_IsVisible_Prefix"));
                patches = true;
            }

            if (!Harmony.GetPatchInfo(AccessTools.Method(typeof(Pawn_IdeoTracker), "get_CertaintyChangeFactor"))?.Prefixes?.Any(patch => patch.owner == alienRaceID) ?? true)
            {
                stringBuilder.AppendLine("  Patching Pawn_IdeoTracker.get_CertaintyChangeFactor");
                harmony.Patch(pawn_IdeoTracker_CertaintyChangeFactor, prefix: new HarmonyMethod(thisClass, "Alien_CertaintyChangeFactor_Prefix"));
                patches = true;
            }

            if (!Harmony.GetPatchInfo(AccessTools.Method(typeof(HediffGiver), "TryApply"))?.Prefixes?.Any(patch => patch.owner == alienRaceID) ?? true)
            {
                stringBuilder.AppendLine("  Patching HediffGiver.TryApply");
                harmony.Patch(hediffGiver_TryApply, prefix: new HarmonyMethod(thisClass, "Alien_TryApply_Prefix"));
                patches = true;
            }

            if (patches) Log.Message(stringBuilder.ToString().TrimEndNewlines());
        }

        /// <summary>
        /// Adds a given alien race to the list of races to allow for the racial life stage patches.
        /// </summary>
        /// <param name="alienRace">String name of the alien race defName</param>
        /// <returns>Returns true if the race was added to the list, false if it was not.</returns>
        public static bool Alien_AddRaceToPatch(string alienRace)
        {
            if (!RaceList.Contains(alienRace) && DefDatabase<ThingDef>.AllDefs.Where(thingDef => thingDef.race != null && thingDef.race.Humanlike && thingDef.defName == alienRace).Count() > 0)
            {
                RaceList.Add(alienRace);
                return true;
            }

            return false;
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
        /// Sets the Ideology certainty factor curve for children.
        /// </summary>
        public static bool Alien_CertaintyChangeFactor_Prefix(Pawn_IdeoTracker __instance, ref float __result)
        {
            var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();

            if (RaceList.Contains(pawn.def.defName))
            {
                __result = CertaintyCurve(pawn);
                return false;
            }
            return true;
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
        private static float CertaintyCurve(Pawn pawn)
        {
            if (!ModsConfig.BiotechActive || pawn == null) return 1f;

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