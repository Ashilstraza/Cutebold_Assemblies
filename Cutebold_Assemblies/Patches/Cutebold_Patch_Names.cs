using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
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

        private static readonly List<RulePackDef> cuteboldNamers = new List<RulePackDef>()
        {
            Cutebold_DefOf.NamerPersonCutebold,
            Cutebold_DefOf.NamerPersonCuteboldOther,
            Cutebold_DefOf.NamerPersonCuteboldOtherFemale,
            Cutebold_DefOf.NamerPersonCuteboldOutsider,
            Cutebold_DefOf.NamerPersonCuteboldOutsiderFemale,
            Cutebold_DefOf.NamerPersonCuteboldSlave
        };
        public static List<RulePackDef> CuteboldNamers { get; }


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

                // Disable Cutebold Name Validation
                harmony.Patch(AccessTools.Method(typeof(NameGenerator), "GenerateName", new Type[] {
                    typeof(GrammarRequest),
                    typeof(Predicate<string>),
                    typeof(bool),
                    typeof(string),
                    typeof(string)
                }), prefix: new HarmonyMethod(typeof(Cutebold_Patch_Names), "CuteboldGenerateNamePrefix"));
                // Generate Cutebold Names
                harmony.Patch(AccessTools.Method(typeof(PawnBioAndNameGenerator), "GeneratePawnName"), prefix: new HarmonyMethod(typeof(Cutebold_Patch_Names), "CuteboldGeneratePawnNamePrefix"));
                // Ignores Validation for Player's Cutebold Names on World Gen
                harmony.Patch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), "CanDoNext"), postfix: new HarmonyMethod(typeof(Cutebold_Patch_Names), "CuteboldCanDoNextPostfix"));
            }
        }

        /// <summary>
        /// Creates lists of the different types of cutebold backstories from specific categories.
        /// </summary>
        private void CreateBackstoryLists()
        {
            //Log.Message("Create Backstory Lists");

            CuteboldRegularChildBackstories = BackstoryDatabase.ShuffleableBackstoryList(BackstorySlot.Childhood, new BackstoryCategoryFilter { categories = new List<string> { "CuteboldRegularChildBackstories" } });
            CuteboldSlaveChildBackstories = BackstoryDatabase.ShuffleableBackstoryList(BackstorySlot.Childhood, new BackstoryCategoryFilter { categories = new List<string> { "CuteboldSlaveChildBackstories" } });
            CuteboldUndergroundChildBackstories = BackstoryDatabase.ShuffleableBackstoryList(BackstorySlot.Childhood, new BackstoryCategoryFilter { categories = new List<string> { "CuteboldUndergroundChildBackstories" } });
            CuteboldRegularAdultBackstories = BackstoryDatabase.ShuffleableBackstoryList(BackstorySlot.Adulthood, new BackstoryCategoryFilter { categories = new List<string> { "CuteboldRegularAdultBackstories" } });
            CuteboldSlaveAdultBackstories = BackstoryDatabase.ShuffleableBackstoryList(BackstorySlot.Adulthood, new BackstoryCategoryFilter { categories = new List<string> { "CuteboldSlaveAdultBackstories" } });
            CuteboldServantAdultBackstories = BackstoryDatabase.ShuffleableBackstoryList(BackstorySlot.Adulthood, new BackstoryCategoryFilter { categories = new List<string> { "CuteboldServantAdultBackstories" } });
            CuteboldUndergroundAdultBackstories = BackstoryDatabase.ShuffleableBackstoryList(BackstorySlot.Adulthood, new BackstoryCategoryFilter { categories = new List<string> { "CuteboldUndergroundAdultBackstories" } });

            /*
            Log.Message("Regular Child:");
            foreach (Backstory b in CuteboldRegularChildBackstories)
            {
                Log.Message("  " + b);
            }
            Log.Message("Slave Child:");
            foreach (Backstory b in CuteboldSlaveChildBackstories)
            {
                Log.Message("  " + b);
            }
            Log.Message("Regular Adult:");
            foreach (Backstory b in CuteboldRegularAdultBackstories)
            {
                Log.Message("  " + b);
            }
            Log.Message("Slave Adult:");
            foreach (Backstory b in CuteboldSlaveAdultBackstories)
            {
                Log.Message("  " + b);
            }
            Log.Message("Servant Adult:");
            foreach (Backstory b in CuteboldServantAdultBackstories)
            {
                Log.Message("  " + b);
            }*/
        }

        /// <summary>
        /// Catches when a cutebold name is about to be generated and nulls out the validator, otherwise errors occur. Cutebold names are very simple and will almost always get rejected.
        /// </summary>
        /// <param name="request">The name generator request.</param>
        /// <param name="validator">The validator to use when naming the pawn.</param>
        /// <param name="appendNumberIfNameUsed">Ignored</param>
        /// <param name="rootKeyword">Ignored</param>
        /// <param name="untranslatedRootKeyword">Ignored</param>
        private static void CuteboldGenerateNamePrefix(GrammarRequest request, ref Predicate<string> validator, bool appendNumberIfNameUsed, string rootKeyword, string untranslatedRootKeyword)
        {

            //Log.Message("Generate Name Prefix");

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
        private static bool CuteboldGeneratePawnNamePrefix(Pawn pawn, ref Name __result, NameStyle style = NameStyle.Full, string forcedLastName = null)
        {

            //Log.Message("Generate Pawn Name Prefix");
            //Log.Message("  pawn def=" + pawn.def.ToString() + " style=" + style.ToString());

            if (pawn.def?.defName != Cutebold_Assemblies.RaceName || style != NameStyle.Full) return true;

            //Log.Message("  pawn faction=" + pawn.Faction.ToString() + "  faction name maker="+((pawn.Faction != null && pawn.Faction.def.pawnNameMaker != null) ? pawn.Faction.def.pawnNameMaker.ToString() : ""));
            RulePackDef rulePack = null;

            if (pawn.Faction != null && cuteboldNamers.Contains(pawn.Faction.def.pawnNameMaker))
            {
                rulePack = pawn.Faction.def.pawnNameMaker;
            }

            // Cutebolds with no faction name maker
            if (pawn.Faction?.def.pawnNameMaker == null)
            {
                //Log.Message("  Faction null or pawnNameMaker null");
                if (pawn.story.childhood == null || (pawn.story.adulthood != null && CuteboldRegularChildBackstories.Contains(pawn.story.childhood) && CuteboldRegularAdultBackstories.Contains(pawn.story.adulthood))) // Cutebold somehow does not have a childhood or is from a cutebold tribe
                {
                    //Log.Message("    Regular Cutebold");
                    rulePack = Cutebold_DefOf.NamerPersonCutebold;
                }
                else if (CuteboldSlaveChildBackstories.Contains(pawn.story.childhood)) // Cutebold was a cutebold child slave
                {
                    //Log.Message("    Slave Child Cutebold");
                    rulePack = Cutebold_DefOf.NamerPersonCuteboldSlave;
                }
                else if (CuteboldRegularChildBackstories.Contains(pawn.story.childhood) || (pawn.story.adulthood != null && CuteboldRegularAdultBackstories.Contains(pawn.story.adulthood))) // Cutebold either joined or left a cutebold tribe during their lifetime
                {
                    //Log.Message("    Other Cutebold");
                    rulePack = pawn.gender == Verse.Gender.Female ? Cutebold_DefOf.NamerPersonCuteboldOtherFemale : Cutebold_DefOf.NamerPersonCuteboldOther;
                }
                else // Cutebold is an outsider
                {
                    //Log.Message("    Outsider Cutebold");
                    rulePack = pawn.gender == Verse.Gender.Female ? Cutebold_DefOf.NamerPersonCuteboldOutsiderFemale : Cutebold_DefOf.NamerPersonCuteboldOutsider;
                }

                // We want servants to have a full first and last name.
                if (pawn.story.adulthood != null && CuteboldServantAdultBackstories.Contains(pawn.story.adulthood))
                {
                    //Log.Message("    Servant Cutebold");
                    rulePack = pawn.gender == Verse.Gender.Female ? Cutebold_DefOf.NamerPersonCuteboldOutsiderFemale : Cutebold_DefOf.NamerPersonCuteboldOutsider;
                }
            }
            // Cutebold slaves/servants
            else if (pawn.story.adulthood != null && CuteboldServantAdultBackstories.Contains(pawn.story.adulthood)) // Cutebold servents get a full name
            {
                //Log.Message("  Cutebold with faction and pawnNameMaker");
                //Log.Message("    Servant Cutebold");
                rulePack = pawn.gender == Verse.Gender.Female ? Cutebold_DefOf.NamerPersonCuteboldOutsiderFemale : Cutebold_DefOf.NamerPersonCuteboldOutsider;
            }
            else if (pawn.story.childhood != null && CuteboldSlaveChildBackstories.Contains(pawn.story.childhood)) // Cutebold child slaves get a simple name
            {
                //Log.Message("  Cutebold with faction and pawnNameMaker");
                //Log.Message("    Slave Child Cutebold");
                rulePack = Cutebold_DefOf.NamerPersonCuteboldSlave;
            }

            if (rulePack != null)
            {
                __result = CuteboldNameGenerator(rulePack, forcedLastName);

                return false;
            }
            else
            {
                //Log.Message("RulePack still null, using regular name generator.");
                return true;
            }
        }

        /// <summary>
        /// Generates a name for a cutebold. May generate a name that has been used if unable to create a unique one.
        /// </summary>
        /// <param name="rulePack">The given name rules.</param>
        /// <param name="forcedLastName">The forced last name.</param>
        /// <returns>Returns a new cutebold name.</returns>
        private static Name CuteboldNameGenerator(RulePackDef rulePack, string forcedLastName)
        {
            var name = CuteboldNameResolver(rulePack, forcedLastName);

            for (int i = 0; i < 100; i++)
            {
                if (!CuteboldNameChecker(name))
                {
                    //Log.Message("  Generated name: "+__result.ToStringFull+" after "+i+" tries.");
                    return name;
                }
                name = CuteboldNameResolver(rulePack, forcedLastName);
            }

            Log.Warning(string.Format("{0}: Failed at creating a unique name, using {1}.", new object[] {
                    Cutebold_Assemblies.ModName,
                    name.ToStringFull
                }));
            return name;
        }

        /// <summary>
        /// Creates a cutebold name.
        /// </summary>
        /// <param name="nameMaker">The given name rules.</param>
        /// <param name="forcedLastName">The forced last name.</param>
        /// <returns>Returns a new cutebold name.</returns>
        private static Name CuteboldNameResolver(RulePackDef nameMaker, string forcedLastName)
        {
            NameTriple name = NameTriple.FromString(NameGenerator.GenerateName(nameMaker, null, false, null, null));
            name.CapitalizeNick();
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
            NameTriple nameTriple = name as NameTriple;

            foreach (NameTriple otherName in NameUseChecker.AllPawnsNamesEverUsed)
            {
                if (otherName != null)
                {
                    if (!otherName.Nick.NullOrEmpty() && !nameTriple.Nick.NullOrEmpty() && otherName.Nick == nameTriple.Nick)
                    {
                        //Log.Message("  Nick already in use.");
                        return true;
                    }
                    if (!otherName.First.NullOrEmpty() && !nameTriple.First.NullOrEmpty() && otherName.First == nameTriple.First
                        && otherName.Last == nameTriple.Last)
                    {
                        //Log.Message("  First and last already in use.");
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Catches the result of the CanDoNext() method when starting a new game and it validates the pawn names.
        /// </summary>
        /// <param name="__result">True if everyone's name is valid.</param>
        [HarmonyPriority(Priority.Low)]
        private static void CuteboldCanDoNextPostfix(ref bool __result)
        {
            if (__result) return;

            if (Find.GameInitData.startingAndOptionalPawns.Any(pawn => !pawn.Name.IsValid && pawn.def.defName == Cutebold_Assemblies.RaceName))
                __result = true;
        }
    }
}
