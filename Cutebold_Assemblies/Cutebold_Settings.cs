#if !RWPre1_3
using System.Collections.Generic;
#endif

using System;
using UnityEngine;
using Verse;

namespace Cutebold_Assemblies
{
    /// <summary>
    /// Choice of three different types of handling for the ideology darkness meme.
    /// -Cutebold Default: Uses the glow curve from the hediff.
    /// -Ideology Default: Uses the darkness meme ignore darkness.
    /// -Hybrid: Uses modified glow curve.
    /// </summary>
    public enum Cutebold_DarknessOptions : byte
    {
        CuteboldDefault,
        IdeologyDefault,
        Hybrid
    }

    /// <summary>
    /// Enables converting enum to a string.
    /// </summary>
    public static class Cutebold_DarknessOptionsExtension
    {
        /// <summary>
        /// Human readable string for the enum options.
        /// </summary>
        /// <param name="option">The enum to translate.</param>
        /// <returns>Readable Text</returns>
        public static string ToStringHuman(this Cutebold_DarknessOptions option)
        {
            return option switch
            {
                Cutebold_DarknessOptions.CuteboldDefault => "Cutebold_Settings_Ideology_Darkness_CuteboldMethod".Translate(),
                Cutebold_DarknessOptions.IdeologyDefault => "Cutebold_Settings_Ideology_Darkness_IdeologyMethod".Translate(),
                Cutebold_DarknessOptions.Hybrid => "Cutebold_Settings_Ideology_Darkness_HybridMethod".Translate(),
                _ => throw new NotImplementedException(),
            };
        }

        /// <summary>
        /// Tooltip for the enums.
        /// </summary>
        /// <param name="option">The enum to get the tooltip for.</param>
        /// <returns>The tooltip.</returns>
        public static string GetTooltip(this Cutebold_DarknessOptions option)
        {
            return option switch
            {
                Cutebold_DarknessOptions.CuteboldDefault => "Cutebold_Settings_Ideology_Darkness_CuteboldMethod_ToolTip".Translate(),
                Cutebold_DarknessOptions.IdeologyDefault => "Cutebold_Settings_Ideology_Darkness_IdeologyMethod_ToolTip".Translate(),
                Cutebold_DarknessOptions.Hybrid => "Cutebold_Settings_Ideology_Darkness_HybridMethod_ToolTip".Translate(),
                _ => throw new NotImplementedException(),
            };
        }
    }

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
        /// <summary>If cutebold eyes should blink in the dark.</summary>
        public bool blinkEyes = true;
        /// <summary>If ears/tail can be visually detached.</summary>
        public bool detachableParts = true;
        /// <summary>The current darkness meme method.</summary>
        public Cutebold_DarknessOptions darknessOptions = Cutebold_DarknessOptions.CuteboldDefault;
        /// <summary>If sun sickness should be ignored.</summary>
        public bool ignoreSickness = false;

        /// <summary>
        /// Data to be saved.
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Values.Look(ref extraYield, "extraYield", true, true);
            Scribe_Values.Look(ref altYield, "altYield", false, true);
            Scribe_Values.Look(ref eyeAdaptation, "eyeAdaptation", true, true);
            Scribe_Values.Look(ref glowEyes, "glowEyes", true, true);
            Scribe_Values.Look(ref blinkEyes, "blinkEyes", true, true);
            Scribe_Values.Look(ref detachableParts, "detachableParts", true, true);
            Scribe_Values.Look(ref darknessOptions, "darknessOptions", Cutebold_DarknessOptions.CuteboldDefault, true);
            Scribe_Values.Look(ref ignoreSickness, "ignoreSickness", false, true);
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
        /// <summary>Version number for the assembly</summary>
        private readonly Version versionNumber = typeof(Cutebold_Assemblies).Assembly.GetName().Version;
        /// <summary>True if harvest yield patch mod is enabled.</summary>
        private readonly bool extraYieldDisabled = ModLister.GetActiveModWithIdentifier("syrchalis.harvestyieldpatch") != null;
        /// <summary>Tab string</summary>
        private readonly string tab = "        ";

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
            Cutebold_Listing settingEntries = new();

            settingEntries.Begin(inRect);
            settingEntries.SectionLabel("Cutebold_Settings_GeneralLabel".Translate());
            settingEntries.CheckboxLabeled("Cutebold_Settings_ResourceCheckbox".Translate().ToString() + (extraYieldDisabled ? "Cutebold_Settings_ResourceCheckbox_SYR_HarvestYield".Translate().ToString() : ""), ref settings.extraYield, (extraYieldDisabled ? "Cutebold_Settings_ResourceCheckbox_ToolTip_Disabled".Translate() : "Cutebold_Settings_ResourceCheckbox_ToolTip_Enabled".Translate()), extraYieldDisabled);
            if (Prefs.DevMode) settingEntries.CheckboxLabeled("Cutebold_Settings_AltYield".Translate(tab), ref settings.altYield, "Cutebold_Settings_AltYield_ToolTip".Translate(), !settings.extraYield);
            settingEntries.CheckboxLabeled("Cutebold_Settings_DarkAdaptation".Translate(), ref settings.eyeAdaptation, "Cutebold_Settings_DarkAdaptation_ToolTip".Translate());
            settingEntries.CheckboxLabeled("Cutebold_Settings_DetachableParts".Translate(), ref settings.detachableParts, "Cutebold_Settings_DetachableParts_ToolTip".Translate());

            if (settings.eyeAdaptation)
            {
                settingEntries.Gap(36);
                settingEntries.SectionLabel("Cutebold_Settings_DarkAdaptation_Label".Translate());
                settingEntries.CheckboxLabeled("Cutebold_Settings_SunSickness".Translate(), ref settings.ignoreSickness, "Cutebold_Settings_SunSickness_Tooltip".Translate());
                settingEntries.CheckboxLabeled("Cutebold_Settings_EyeGlow".Translate(), ref settings.glowEyes, "Cutebold_Settings_EyeGlow_ToolTip".Translate());
                settingEntries.CheckboxLabeled("Cutebold_Settings_EyeGlowBlink".Translate(tab), ref settings.blinkEyes, "Cutebold_Settings_EyeGlowBlink_ToolTip".Translate(), !settings.glowEyes);
#if !RWPre1_3
                if (settingEntries.AltButtonTextLabeled("Cutebold_Settings_Ideology_Darkness".Translate(), settings.darknessOptions.ToStringHuman(), tooltip: (ModLister.IdeologyInstalled ? "Cutebold_Settings_Ideology_Darkness_ToolTip".Translate() : "Cutebold_Settings_Ideology_Darkness_ToolTip_NoIdeology".Translate()), disabled: !ModLister.IdeologyInstalled))
                {
                    List<FloatMenuOption> options = [];
                    foreach (Cutebold_DarknessOptions option in Enum.GetValues(typeof(Cutebold_DarknessOptions)))
                    {
                        FloatMenuOption floatOption = new(option.ToStringHuman(), delegate
                        {
                            settings.darknessOptions = option;
                        })
                        {
                            tooltip = option.GetTooltip()
                        };

                        options.Add(floatOption);
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
#endif
            }

            //settingEntries.SectionLabel("Cutebold_Settings_ModSpecific".Translate());

            if (Prefs.DevMode)
            {
                settingEntries.Gap(36);
                settingEntries.SectionLabel("Cutebold_Settings_DebugLabel".Translate());
                settingEntries.Label("Cutebold_Settings_AssemblyVersion".Translate() + versionNumber);
                if (settingEntries.AltButtonTextLabeled("Cutebold_Settings_CheckPatches".Translate(), "Cutebold_Settings_CheckPatches_Button".Translate(), tooltip: "Cutebold_Settings_CheckPatches_ToolTip".Translate()))
                {
                    Cutebold_Assemblies.CheckPatchedMethods();
                }
                if (settingEntries.AltButtonTextLabeled("Cutebold_Settings_CheckAllPatches".Translate(), "Cutebold_Settings_CheckPatches_Button".Translate(), tooltip: "Cutebold_Settings_CheckAllPatches_ToolTip".Translate()))
                {
                    Cutebold_Assemblies.CheckPatchedMethods(true);
                }
            }
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
            Cutebold_Patch_Body.CuteboldAddonModifier(settings);
            Hediff_CuteboldDarkAdaptation.UpdateIgnoreSickness();
            base.WriteSettings();
        }

        /// <summary>
        /// Name of the settings window.
        /// </summary>
        /// <returns>Our mod name.</returns>
        public override string SettingsCategory()
        {
            return "Cutebold_RaceName".Translate();
        }
    }

    /// <summary>
    /// Enables extra functionality.
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

        /// <summary>
        /// Creates a button with text to the left and the given paramaters.
        /// </summary>
        /// <param name="label">Text to the left of the button.</param>
        /// <param name="buttonLabel">What text should be on the button.</param>
        /// <param name="buttonWidthPercent">Width of the button compared to the entire widget.</param>
        /// <param name="tooltip">What the tooltio should be when hovered.</param>
        /// <param name="disabled">If the button should be disabled.</param>
        /// <returns>If the button has been released.</returns>
        public bool AltButtonTextLabeled(string label, string buttonLabel, float buttonWidthPercent = 0.25f, float height = 30f, string tooltip = null, bool disabled = false)
        {
            Rect rect = GetRect(height);
            Widgets.Label(rect.LeftPart(1f - buttonWidthPercent), label);
            Color color = GUI.color;
            if (disabled) GUI.color = Color.gray;
            bool result = Widgets.ButtonText(rect.RightPart(buttonWidthPercent), buttonLabel, active: !disabled);
            GUI.color = color;

            if (tooltip != null)
            {
                TooltipHandler.TipRegion(rect.RightPart(buttonWidthPercent), tooltip);
            }

            Gap(verticalSpacing);
            return result;
        }

        /// <summary>
        /// Creates a label with medium text and a divider line.
        /// </summary>
        /// <param name="label">What the text should be.</param>
        /// <param name="maxHeight">The max height of the label should be.</param>
        /// <param name="tooltip">What the tooltio should be when hovered.</param>
        /// <returns>The size of the widget as a Rect.</returns>
        public Rect SectionLabel(string label, float maxHeight = -1f, string tooltip = null)
        {
            Text.Font = GameFont.Medium;
#if RWPre1_6
            Rect rect = Label(label, maxHeight, tooltip);
#else
            Rect rect = Label(label, maxHeight, (TipSignal?)(TipSignal)tooltip);
#endif
            Text.Font = GameFont.Small;
            GapLine(1f);
            rect.height += 1f;
            Gap(11f);
            rect.height += 11f;
            return rect;
        }
    }
}