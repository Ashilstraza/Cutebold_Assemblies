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
        /// <summary>Use an alternative yield patching.</summary>
        public bool altYield = false;
        /// <summary>If the player wants to have cutebolds become 'dark adapted'.</summary>
        public bool eyeAdaptation = true;
        /// <summary>If pawn eyes should glow when 'dark adapted'.</summary>
        public bool glowEyes = true;
        /// <summary>If ears/tail can be visually detached.</summary>
        public bool detachableParts = true;

        /// <summary>
        /// Data to be saved.
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Values.Look(ref extraYield, "extraYield", true, true);
            Scribe_Values.Look(ref altYield, "altYield", false, true);
            Scribe_Values.Look(ref eyeAdaptation, "eyeAdaptation", true, true);
            Scribe_Values.Look(ref glowEyes, "glowEyes", true, true);
            Scribe_Values.Look(ref detachableParts, "detachableParts", true, true);
            base.ExposeData();
        }
    }

    /// <summary>
    /// Overrides the Mod class to enable usage of settings.
    /// </summary>
    public class CuteboldMod : Mod
    {
        /// <summary>List of saved settings</summary>
        private readonly Cutebold_Settings settings;

        private readonly System.Version versionNumber = typeof(Cutebold_Assemblies).Assembly.GetName().Version;

        private readonly bool extraYieldDisabled = ModLister.GetActiveModWithIdentifier("syrchalis.harvestyieldpatch") != null;

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
            Cutebold_Listing settingEntries = new Cutebold_Listing();

            settingEntries.Begin(inRect);
            if (Prefs.DevMode) settingEntries.Label($"Cutebold Assembly Version: {versionNumber}");

            settingEntries.CheckboxLabeled($"Cutebolds are able to extract extra resources (requires restart):{(extraYieldDisabled ? " [SYR] Harvest Yield enabled, using that instead." : "")}", ref settings.extraYield, (extraYieldDisabled ? "Humans mis... didn't miss a spot?" : "Human missed a spot!"), extraYieldDisabled);
            
            if (Prefs.DevMode)
            {
                settingEntries.CheckboxLabeled("    Use alternative yield patching method (requires restart):", ref settings.altYield, "Eeeeeek!", !settings.extraYield);

                if (settingEntries.ButtonTextLabeled("Check for all patches to methods that this mod patches (used for debugging):","Check"))
                {
                    Cutebold_Assemblies.CheckPatchedMethods();
                }
            };
            settingEntries.CheckboxLabeled("Cutebolds can adapt to see in the dark (requires restart):", ref settings.eyeAdaptation, "The sun, it burns!");
            settingEntries.CheckboxLabeled("    Cutebolds eyes glow when dark adapted:", ref settings.glowEyes, "Shiny eyes!", !settings.eyeAdaptation);
            settingEntries.CheckboxLabeled("Cutebold ears and tail will be visually gone when the body part becomes lost:", ref settings.detachableParts, "No ears or tail makes a sadbold.");
            settingEntries.End();

            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// By default, writes settings on window close. Also handles enabling/disabling of certain settings that were changed.
        /// 
        /// We could remove the postfixes for yield and dark adaptation, however that may interfere with gameplay and would get ugly.
        /// </summary>
        public override void WriteSettings()
        {
            //Cutebold_Patch_BodyAddons.CuteboldAddonSettingsUpdate(settings);
            Cutebold_Patch_BodyAddons.CuteboldAddonModifier(settings);
            base.WriteSettings();
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

    /// <summary>
    /// Enables the ability to disable a checkbox.
    /// </summary>
    public class Cutebold_Listing : Listing_Standard
    {
        /// <summary>
        /// Creates a checkbox with the given paramaters.
        /// </summary>
        /// <param name="label">What the checkbox should be labeled.</param>
        /// <param name="checkOn">If the checkbox is checked.</param>
        /// <param name="tooltip">What the tooltio should be when hovered.</param>
        /// <param name="disabled">If the checkbox should be disabled.</param>
        public void CheckboxLabeled(string label, ref bool checkOn, string tooltip = null, bool disabled = false)
        {
            float lineHeight = Text.LineHeight;
            Rect rect = GetRect(lineHeight);
            if (!tooltip.NullOrEmpty())
            {
                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                }
                TooltipHandler.TipRegion(rect, tooltip);
            }
            Widgets.CheckboxLabeled(rect, label, ref checkOn, disabled);
            Gap(verticalSpacing);
        }
    }
}
