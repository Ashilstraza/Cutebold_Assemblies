using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Grammar;

namespace Cutebold_Assemblies
{
    /// <summary>
    /// Harmony patches for the different name related methods.
    /// </summary>
    class Cutebold_Patch_Names
    {
        /// <summary>If we have created the different backstory lists.</summary>
        private readonly bool createdLists = false;

#if RWPre1_4
        /// <summary>List of Regular Cutebold Child Backstories</summary>
        public static List<Backstory> CuteboldRegularChildBackstories { get; private set; }
        /// <summary>List of Slave Cutebold Child Backstories</summary>
        public static List<Backstory> CuteboldSlaveChildBackstories { get; private set; }
        /// <summary>List of Underground Cutebold Child Backstories</summary>
        public static List<Backstory> CuteboldUndergroundChildBackstories { get; private set; }
        /// <summary>List of Regular Cutebold Adult Backstories</summary>
        public static List<Backstory> CuteboldRegularAdultBackstories { get; private set; }
        /// <summary>List of Regular Cutebold Adult Backstories, Unused.</summary>
        public static List<Backstory> CuteboldSlaveAdultBackstories { get; private set; }
        /// <summary>List of Servant Cutebold Adult Backstories</summary>
        public static List<Backstory> CuteboldServantAdultBackstories { get; private set; }
        /// <summary>List of Underground Cutebold Adult Backstories</summary>
        public static List<Backstory> CuteboldUndergroundAdultBackstories { get; private set; }
#else
        /// <summary>List of Regular Cutebold Child Backstories</summary>
        public static List<BackstoryDef> CuteboldRegularChildBackstories { get; private set; }
        /// <summary>List of Slave Cutebold Child Backstories</summary>
        public static List<BackstoryDef> CuteboldSlaveChildBackstories { get; private set; }
        /// <summary>List of Underground Cutebold Child Backstories</summary>
        public static List<BackstoryDef> CuteboldUndergroundChildBackstories { get; private set; }
        /// <summary>List of Regular Cutebold Adult Backstories</summary>
        public static List<BackstoryDef> CuteboldRegularAdultBackstories { get; private set; }
        /// <summary>List of Regular Cutebold Adult Backstories, Unused.</summary>
        public static List<BackstoryDef> CuteboldSlaveAdultBackstories { get; private set; }
        /// <summary>List of Servant Cutebold Adult Backstories</summary>
        public static List<BackstoryDef> CuteboldServantAdultBackstories { get; private set; }
        /// <summary>List of Underground Cutebold Adult Backstories</summary>
        public static List<BackstoryDef> CuteboldUndergroundAdultBackstories { get; private set; }
#endif

        /// <summary>
        /// Applies harmony patches on startup.
        /// </summary>
        /// <param name="harmony">Our instance of harmony to patch with.</param>
        public Cutebold_Patch_Names(Harmony harmony)
        {
            if (!createdLists)
            {
                CreateBackstoryLists();
                createdLists = true;
                Type thisClass = typeof(Cutebold_Patch_Names);

                // Disable Cutebold Name Validation
                harmony.Patch(AccessTools.Method(typeof(NameGenerator), nameof(NameGenerator.GenerateName), [
                    typeof(GrammarRequest),
                    typeof(Predicate<string>),
                    typeof(bool),
                    typeof(string),
                    typeof(string)
                ]), prefix: new HarmonyMethod(thisClass, nameof(CuteboldGenerateNamePrefix)));
                // Generate Cutebold Names
                harmony.Patch(AccessTools.Method(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.GeneratePawnName)), prefix: new HarmonyMethod(thisClass, nameof(CuteboldGeneratePawnNamePrefix)));
                // Allow for renaming pawns that lack a first and/or last name
                harmony.Patch(AccessTools.PropertyGetter(typeof(NameTriple), "IsValid"), postfix: new HarmonyMethod(thisClass, nameof(Cutebold_NameTriple_IsValidPostfix)));
            }
        }

        /// <summary>
        /// Creates lists of the different types of cutebold backstories from specific categories.
        /// </summary>
        private void CreateBackstoryLists()
        {
#if RWPre1_4
            CuteboldRegularChildBackstories = BackstoryDatabase.ShuffleableBackstoryList(BackstorySlot.Childhood, new BackstoryCategoryFilter { categories = ["CuteboldRegularChildBackstories"] });
            CuteboldSlaveChildBackstories = BackstoryDatabase.ShuffleableBackstoryList(BackstorySlot.Childhood, new BackstoryCategoryFilter { categories = ["CuteboldSlaveChildBackstories"] });
            CuteboldUndergroundChildBackstories = BackstoryDatabase.ShuffleableBackstoryList(BackstorySlot.Childhood, new BackstoryCategoryFilter { categories = ["CuteboldUndergroundChildBackstories"] });
            CuteboldRegularAdultBackstories = BackstoryDatabase.ShuffleableBackstoryList(BackstorySlot.Adulthood, new BackstoryCategoryFilter { categories = ["CuteboldRegularAdultBackstories"] });
            CuteboldSlaveAdultBackstories = BackstoryDatabase.ShuffleableBackstoryList(BackstorySlot.Adulthood, new BackstoryCategoryFilter { categories = ["CuteboldSlaveAdultBackstories"] });
            CuteboldServantAdultBackstories = BackstoryDatabase.ShuffleableBackstoryList(BackstorySlot.Adulthood, new BackstoryCategoryFilter { categories = ["CuteboldServantAdultBackstories"] });
            CuteboldUndergroundAdultBackstories = BackstoryDatabase.ShuffleableBackstoryList(BackstorySlot.Adulthood, new BackstoryCategoryFilter { categories = ["CuteboldUndergroundAdultBackstories"] });
#else
            BackstorySlot slot = BackstorySlot.Childhood;

            CuteboldRegularChildBackstories = DefDatabase<BackstoryDef>.AllDefs.Where((BackstoryDef bs) => bs.shuffleable && bs.slot == slot && bs.spawnCategories.Contains("CuteboldRegularChildBackstories")).ToList();
            CuteboldSlaveChildBackstories = DefDatabase<BackstoryDef>.AllDefs.Where((BackstoryDef bs) => bs.shuffleable && bs.slot == slot && bs.spawnCategories.Contains("CuteboldSlaveChildBackstories")).ToList();
            CuteboldUndergroundChildBackstories = DefDatabase<BackstoryDef>.AllDefs.Where((BackstoryDef bs) => bs.shuffleable && bs.slot == slot && bs.spawnCategories.Contains("CuteboldUndergroundChildBackstories")).ToList();

            slot = BackstorySlot.Adulthood;

            CuteboldRegularAdultBackstories = DefDatabase<BackstoryDef>.AllDefs.Where((BackstoryDef bs) => bs.shuffleable && bs.slot == slot && bs.spawnCategories.Contains("CuteboldRegularAdultBackstories")).ToList();
            CuteboldSlaveAdultBackstories = DefDatabase<BackstoryDef>.AllDefs.Where((BackstoryDef bs) => bs.shuffleable && bs.slot == slot && bs.spawnCategories.Contains("CuteboldSlaveAdultBackstories")).ToList();
            CuteboldServantAdultBackstories = DefDatabase<BackstoryDef>.AllDefs.Where((BackstoryDef bs) => bs.shuffleable && bs.slot == slot && bs.spawnCategories.Contains("CuteboldServantAdultBackstories")).ToList();
            CuteboldUndergroundAdultBackstories = DefDatabase<BackstoryDef>.AllDefs.Where((BackstoryDef bs) => bs.shuffleable && bs.slot == slot && bs.spawnCategories.Contains("CuteboldUndergroundAdultBackstories")).ToList();
#endif
        }

        /// <summary>
        /// Catches when a cutebold name is about to be generated and nulls out the validator, otherwise errors occur. Cutebold names are very simple and will almost always get rejected.
        /// </summary>
        /// <param name="request">The name generator request.</param>
        /// <param name="validator">The validator to use when naming the pawn.</param>
        public static void CuteboldGenerateNamePrefix(GrammarRequest request, ref Predicate<string> validator)
        {
            if (request.Includes.Any(included => included == Cutebold_DefOf.NamerPersonCutebold || included == Cutebold_DefOf.NamerPersonCuteboldSlave))
                validator = null;
        }

        /// <summary>
        /// Catches cutebolds and names them if they are either slaves or lack a name maker.
        /// </summary>
        /// <param name="pawn">The pawn to be named.</param>
        /// <param name="__result">The name of the pawn.</param>
        /// <param name="style">If the pawn is numbered like an animal. Unused.</param>
        /// <param name="forcedLastName">Force the use of a given last name.</param>
        /// <returns>Two return types, method returns true when want to use the regular GeneratePawnName method and returns false when we use our custom one. The second return type is __result which is the generated name that we want to use.</returns>
        [HarmonyPriority(Priority.Low)]
        public static bool CuteboldGeneratePawnNamePrefix(Pawn pawn, ref Name __result, NameStyle style = NameStyle.Full, string forcedLastName = null)
        {
            if (pawn.def != Cutebold_Assemblies.CuteboldRaceDef || style != NameStyle.Full) return true;

#if RWPre1_3
            RulePackDef rulePack = pawn.Faction?.def.pawnNameMaker;
            Backstory childhood = pawn.story.childhood;
            Backstory adulthood = pawn.story.adulthood;
#else
            RulePackDef rulePack = pawn.Faction?.ideos?.PrimaryCulture.pawnNameMaker;
#if RW1_3
            Backstory childhood = pawn.story.childhood;
            Backstory adulthood = pawn.story.adulthood;
    #else
            BackstoryDef childhood = pawn.story.Childhood;
            BackstoryDef adulthood = pawn.story.Adulthood;
    #endif
#endif
            // Cutebolds with no faction pawn name maker
            if (rulePack == null)
            {
                if (childhood == null || (adulthood != null && CuteboldRegularChildBackstories.Contains(childhood) && CuteboldRegularAdultBackstories.Contains(adulthood))) // Cutebold somehow does not have a childhood or is from a cutebold tribe
                {
                    rulePack = Cutebold_DefOf.NamerPersonCutebold;
                }
                else if (CuteboldSlaveChildBackstories.Contains(childhood)) // Cutebold was a cutebold child slave
                {
                    rulePack = Cutebold_DefOf.NamerPersonCuteboldSlave;
                }
                else if (CuteboldRegularChildBackstories.Contains(childhood) || (adulthood != null && CuteboldRegularAdultBackstories.Contains(adulthood))) // Cutebold either joined or left a cutebold tribe during their lifetime
                {
                    rulePack = pawn.gender == Verse.Gender.Female ? Cutebold_DefOf.NamerPersonCuteboldOtherFemale : Cutebold_DefOf.NamerPersonCuteboldOther;
                }
                else // Cutebold is an outsider
                {
                    rulePack = pawn.gender == Verse.Gender.Female ? Cutebold_DefOf.NamerPersonCuteboldOutsiderFemale : Cutebold_DefOf.NamerPersonCuteboldOutsider;
                }

                // We want servants to have a full first and last name.
                if (adulthood != null && CuteboldServantAdultBackstories.Contains(adulthood))
                {
                    rulePack = pawn.gender == Verse.Gender.Female ? Cutebold_DefOf.NamerPersonCuteboldOutsiderFemale : Cutebold_DefOf.NamerPersonCuteboldOutsider;
                }
            }
            // Cutebold slaves/servants
            else if (adulthood != null && CuteboldServantAdultBackstories.Contains(adulthood)) // Cutebold servents get a full name
            {
                rulePack = pawn.gender == Gender.Female ? Cutebold_DefOf.NamerPersonCuteboldOutsiderFemale : Cutebold_DefOf.NamerPersonCuteboldOutsider;
            }
            else if (childhood != null && CuteboldSlaveChildBackstories.Contains(childhood)) // Cutebold child slaves get a simple name
            {
                rulePack = Cutebold_DefOf.NamerPersonCuteboldSlave;
            }
            else if (childhood != null && !childhood.identifier.ToString().StartsWith("Cutebold")) // Non-Cutebold backstory, sometimes generate with a cutebold outsider name
            {
                if (rulePack == Cutebold_DefOf.NamerPersonCutebold || Rand.Range(1, 100) <= 50)
                {
                    rulePack = pawn.gender == Verse.Gender.Female ? Cutebold_DefOf.NamerPersonCuteboldOutsiderFemale : Cutebold_DefOf.NamerPersonCuteboldOutsider;
                }
            }

            if (rulePack != null)
            {
                NameTriple tempName = CuteboldNameGenerator(rulePack, forcedLastName);

                if (tempName.Nick.EndsWith("za") && pawn.gender != Gender.Female)
                {
                    __result = (NameTriple)(new NameTriple(tempName.First, tempName.Nick.TrimEnd('a'), tempName.Last));
                }
                else __result = tempName;

                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Generates a name for a cutebold. May generate a name that has been used if unable to create a unique one.
        /// </summary>
        /// <param name="rulePack">The given name rules.</param>
        /// <param name="forcedLastName">The forced last name.</param>
        /// <returns>Returns a new cutebold name.</returns>
        private static NameTriple CuteboldNameGenerator(RulePackDef rulePack, string forcedLastName)
        {
            NameTriple name = CuteboldNameResolver(rulePack, forcedLastName);

            for (int i = 0; i < 100; i++)
            {
                if (!CuteboldNameChecker(name))
                {
                    //Log.Message("  Generated name: "+__result.ToStringFull+" after "+i+" tries.");
                    return name;
                }
                name = CuteboldNameResolver(rulePack, forcedLastName);
            }

            Log.Warning(string.Format("{0}: Failed at creating a unique name, using {1}.", [
                    Cutebold_Assemblies.ModName,
                    name.ToStringFull
                ]));
            return name;
        }

        /// <summary>
        /// Creates a cutebold name.
        /// </summary>
        /// <param name="nameMaker">The given name rules.</param>
        /// <param name="forcedLastName">The forced last name.</param>
        /// <returns>Returns a new cutebold name.</returns>
        private static NameTriple CuteboldNameResolver(RulePackDef nameMaker, string forcedLastName)
        {
            NameTriple name = (NameTriple)NameTriple.FromString(NameGenerator.GenerateName(nameMaker, null, false, null, null));
            name.Nick.CapitalizeFirst();
            name.ResolveMissingPieces(forcedLastName);

            return name;
        }

        /// <summary>
        /// Checks if a cutebold name has been already used.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>If the name has been used.</returns>
        private static bool CuteboldNameChecker(Name name)
        {
            //Log.Message("Cutebold Name Checker name=" + name.ToString());
            NameTriple cutebold_Name = name as NameTriple;

            foreach (Name otherName in NameUseChecker.AllPawnsNamesEverUsed)
            {
                cutebold_Name.ConfusinglySimilarTo(otherName);
            }
            return false;
        }

        /// <summary>
        /// Allows for names that are only nicknames when naming pawns. This affects all pawns and not just Cutebolds
        /// </summary>
        /// <param name="__result">If the name is valid.</param>
        /// <param name="__instance">New name.</param>
        public static void Cutebold_NameTriple_IsValidPostfix(ref bool __result, NameTriple __instance)
        {
            if (__result) return;
            if (!__instance.Nick.NullOrEmpty())
            {
                __result = true;
            }
        }
    }
}
