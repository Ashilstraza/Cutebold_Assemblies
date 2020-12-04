using RimWorld;
using Verse;

namespace Cutebold_Assemblies
{
    /// <summary>
    /// List of cutebold related defs that are required for the code.
    /// </summary>
    [DefOf]
    public static class Cutebold_DefOf
    {
        /// <summary>
        /// Required to make stuff not explode in a red mess of errors.
        /// </summary>
        static Cutebold_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(Cutebold_DefOf));
        }

        // RaceDef
        public static ThingDef Alien_Cutebold;
        // Opinion ThoughtDefs
        public static ThoughtDef ButcheredCuteboldCorpseOpinion;
        public static ThoughtDef AteRawCuteboldMeat;
        // TaleDefs
        public static TaleDef AteRawCuteboldMeatTale;
        public static TaleDef ButcheredCuteboldCorpseTale;
        // RulePackDefs
        public static RulePackDef NamerPersonCutebold;
        public static RulePackDef NamerPersonCuteboldSlave;
        public static RulePackDef NamerPersonCuteboldOther;
        public static RulePackDef NamerPersonCuteboldOtherFemale;
        public static RulePackDef NamerPersonCuteboldOutsider;
        public static RulePackDef NamerPersonCuteboldOutsiderFemale;
        public static RulePackDef DamageEvent_CuteboldTrapBoulder;
        // HediffDefs
        public static HediffDef CuteboldDarkAdaptation;
        public static HediffDef CuteboldLightSickness;
        // ApparelDefs
        public static ThingDef Cutebold_Goggles;
    }
}
