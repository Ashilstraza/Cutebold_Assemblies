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
        public static readonly ThingDef Alien_Cutebold;
        // Opinion ThoughtDefs
        public static readonly ThoughtDef ButcheredCuteboldCorpseOpinion;
        public static readonly ThoughtDef AteRawCuteboldMeat;
        // TaleDefs
        public static readonly TaleDef AteRawCuteboldMeatTale;
        public static readonly TaleDef ButcheredCuteboldCorpseTale;
        // RulePackDefs
        public static readonly RulePackDef NamerPersonCutebold;
        public static readonly RulePackDef NamerPersonCuteboldSlave;
        public static readonly RulePackDef NamerPersonCuteboldOther;
        public static readonly RulePackDef NamerPersonCuteboldOtherFemale;
        public static readonly RulePackDef NamerPersonCuteboldOutsider;
        public static readonly RulePackDef NamerPersonCuteboldOutsiderFemale;
        // HediffDefs
        public static readonly HediffDef CuteboldDarkAdaptation;
        public static readonly HediffDef CuteboldLightSickness;
        // ApparelDefs
        public static readonly ThingDef Cutebold_Goggles;
        //public static readonly ThingDef Cutebold_AdvancedGoggles;
    }
}
