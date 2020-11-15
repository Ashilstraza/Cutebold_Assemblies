using UnityEngine;
using Verse;

namespace Cutebold_Assemblies
{
    /// <summary>
    /// Overrides the virtual ModSettings class to enable saving of settings.
    /// </summary>
    public class Cutebold_Settings : ModSettings
    {
        /// <summary>If the player wants to allow cutebolds to harvest more than the default max.</summary>
        public bool extraYield = true;
        /// <summary>If the player wants to have cutebolds become 'cave adapted'.</summary>
        public bool eyeAdaptation = true;

        /// <summary>
        /// Data to be saved.
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Values.Look(ref extraYield, "extraYield", true, true);
            Scribe_Values.Look(ref eyeAdaptation, "eyeAdaptation", true, true);
            base.ExposeData();
        }
    }

    /// <summary>
    /// Overrides the Mod class to enable usage of settings.
    /// </summary>
    public class CuteboldMod : Mod
    {
        /// <summary>List of saved settings</summary>
        private Cutebold_Settings settings;

        /// <summary>
        /// Required constructor to allow for the rest of the mod to be able to use the settings.
        /// </summary>
        /// <param name="content">Something</param>
        public CuteboldMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<Cutebold_Settings>();
        }

        /// <summary>
        /// Creates the settings window.
        /// </summary>
        /// <param name="inRect"></param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard settingEntries = new Listing_Standard();

            settingEntries.Begin(inRect);
            settingEntries.CheckboxLabeled("Cutebolds are able to extract extra resources (requires restart):", ref settings.extraYield, "Human missed a spot!");
            settingEntries.CheckboxLabeled("Cutebolds can adapt to see in the dark (requires restart):", ref settings.eyeAdaptation, "The sun, it burns!");
            settingEntries.End();

            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// Name of the settings window.
        /// </summary>
        /// <returns>Our mod name.</returns>
        public override string SettingsCategory()
        {
            return "Cutebold Race";
        }
    }
}
