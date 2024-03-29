﻿#if RW1_5 && DEBUG
using LudeonTK;
#endif

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using static AlienRace.AlienPartGenerator;

namespace Cutebold_Assemblies
{
    /// <summary>
    /// .xml field for the HediffCompProperties_CuteboldDarkAdaptation component. Used for setting the light/dark adjustments.
    /// </summary>
    public class Cutebold_lightDarkAdjustment
    {
        /// <summary>Flat value that the global work speed should be in 0% light.</summary>
#pragma warning disable IDE0044 // Ignore add readonly modifier: these should always be set in the .xml
        private float dark = -1.0f;
        /// <summary>Flat value that the global work sleed should be in 100% light.</summary>
        private float light = -1.0f;
        /// <summary>Multiplier for the difference between the default global work speed values.</summary>
        private float multiplier = float.MaxValue;
#pragma warning restore IDE0044

        /// <summary>Flat value that the global work speed should be in 0% light.</summary>
        public float Dark => dark;
        /// <summary>Flat value that the global work sleed should be in 100% light.</summary>
        public float Light => light;
        /// <summary>Multiplier for the difference between the default global work speed values.</summary>
        public float Multiplier => multiplier;
    }

    /// <summary>
    /// Property handler for configuring the dark adaptation hediff component.
    /// </summary>
    public class HediffCompProperties_CuteboldDarkAdaptation : HediffCompProperties
    {
#pragma warning disable IDE0044 // Ignore add readonly modifier: these should always be set in the .xml
        /// <summary>How much we should gain per day max.</summary>
        private float maxSeverityGainPerDay = 0.1f;
        /// <summary>How much we should lose per day max.</summary>
        private float maxSeverityLossPerDay = -0.25f;
        /// <summary>The maximum light level before losing severity.</summary>
        private float maxLightLevel = 0.5f;
        /// <summary>The minimum light level before gaining severity.</summary>
        private float minLightLevel = 0.3f;
#pragma warning disable CS0649 // Ignore not being assigned to warning: lightDarkAdjustment should always be set in the .xml
        /// <summary>List of how the light level affects global work speed.</summary>
        private List<Cutebold_lightDarkAdjustment> lightDarkAdjustment;
        /// <summary>List of the different eyes that overcome adaptation sickness.</summary>
        private List<string> specialEyes;
#pragma warning restore CS0649
#pragma warning restore IDE0044

        /// <summary>How much we should gain per day max.</summary>
        public float MaxSeverityGainPerDay => maxSeverityGainPerDay;
        /// <summary>How much we should gain per day max.</summary>
        public float MaxSeverityLossPerDay => maxSeverityLossPerDay;
        /// <summary>The maximum light level before losing severity.</summary>
        public float MaxLightLevel => maxLightLevel;
        /// <summary>The minimum light level before gaining severity.</summary>
        public float MinLightLevel => minLightLevel;
        /// <summary>List of how the light level affects global work speed.</summary>
        public List<Cutebold_lightDarkAdjustment> LightDarkAdjustment => lightDarkAdjustment;
        /// <summary>List of the different eyes that overcome adaptation sickness.</summary>
        public List<string> SpecialEyes => specialEyes;

        /// <summary>
        /// Adds a reference to the HediffComp on creation.
        /// </summary>
        public HediffCompProperties_CuteboldDarkAdaptation()
        {
            compClass = typeof(HediffComp_CuteboldDarkAdaptation);
        }

        /// <summary>
        /// Checks the given fields and returns any issues with the configuration.
        /// </summary>
        /// <returns>Any issues found.</returns>
        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (string item in base.ConfigErrors(parentDef))
            {
                yield return item;
            }
            foreach (Cutebold_lightDarkAdjustment adjustment in lightDarkAdjustment)
            {
                if (adjustment.Dark == -1.0f && adjustment.Light == -1.0f && adjustment.Multiplier == float.MaxValue)
                {
                    yield return "A lightDarkAdjustment is empty in .xml.";
                }
                if ((adjustment.Dark == -1.0f && adjustment.Light != -1.0f) || (adjustment.Dark != -1.0f && adjustment.Light == -1.0f))
                {
                    yield return "A lightDarkAdjustment has only a single light or dark value.";
                }
                if ((adjustment.Dark != -1.0f || adjustment.Light != -1.0f) && adjustment.Multiplier != float.MaxValue)
                {
                    yield return "A lightDarkAdjustment has both a light and/or dark value along with a multiplier, the light and/or dark value will be used instead of the multiplier.";
                }
            }
            if (maxSeverityGainPerDay == 0.0f)
            {
                yield return "HediffCompProperties_CuteboldDarkAdaptation maxSeverityGainPerDay is 0.";
            }
            else if (maxSeverityLossPerDay == 0.0f)
            {
                yield return "HediffCompProperties_CuteboldDarkAdaptation maxSeverityLossPerDay is 0.";
            }
            else if (Prefs.DevMode && (maxSeverityGainPerDay != 0.1f || maxSeverityLossPerDay != -0.25f))
            {
                Log.Error("You are either debugging, modifying cutebold adaptation, or forgot to switch max severity back. Don't upload.");
            }
            if (minLightLevel > maxLightLevel)
            {
                yield return "HediffCompProperties_CuteboldDarkAdaptation minLightLevel is greater than maxLightLevel.";
            }
            if (lightDarkAdjustment == null)
            {
                yield return "HediffCompProperties_CuteboldDarkAdaptation lightDarkAdjustment is null.";
            }

        }
    }

    /// <summary>
    /// Hediff component that controls the severity of dark adaptation.
    /// </summary>
    public class HediffComp_CuteboldDarkAdaptation : HediffComp
    {
        /// <summary>Number of ticks between updates.</summary>
        private const int SeverityUpdateInterval = 200;
        /// <summary>Multiplier for severity based on tick interval.</summary>
        private const float SeverityUpdateMultiplier = 1f / (60000f / SeverityUpdateInterval);
        /// <summary>Set of properties for the hediff component.</summary>
        private HediffCompProperties_CuteboldDarkAdaptation Props => (HediffCompProperties_CuteboldDarkAdaptation)props;

        /// <summary>Light level of the pawn at the current location.</summary>
        public float LightLevel = 0f;
        /// <summary>If the pawn can be affected by light.</summary>
        public bool CanSee = true;
        /// <summary>If the pawn should ignore the light level.</summary>
        public bool IgnoreLightLevel = false;
        /// <summary>The maximum light level before adaptation starts to decrease.</summary>
        public float MaxLightLevel => Props.MaxLightLevel;
        /// <summary>The minimum light level before adaptation starts to increase.</summary>
        public float MinLightLevel => Props.MinLightLevel;
        /// <summary>List of the adjustment values for each hediff stage.</summary>
        public List<Cutebold_lightDarkAdjustment> LightDarkAdjustment => Props.LightDarkAdjustment;
        /// <summary>List of the different eyes that overcome adaptation sickness.</summary>
        public List<string> SpecialEyes => Props.SpecialEyes;

        /// <summary>
        /// Sets the severity adjustment amount depending on the pawn's light level.
        /// </summary>
        /// <param name="severityAdjustment">Reference to the severity adjustment amount.</param>
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (CanSee && base.Pawn.IsHashIntervalTick(SeverityUpdateInterval))
            {
                float num = SeverityChangePerDay();
                num *= SeverityUpdateMultiplier;
                severityAdjustment += num;
            }
        }

        /// <summary>
        /// Returns the severity amount per day depending on light level.
        /// </summary>
        /// <returns>The amount per day that we should adjust by.</returns>
        protected virtual float SeverityChangePerDay()
        {
            if (!CanSee) return 0f;
            if (IgnoreLightLevel || LightLevel < MinLightLevel) return Props.MaxSeverityGainPerDay;
            else if (LightLevel > MaxLightLevel) return Props.MaxSeverityLossPerDay;
            else return 0f;
        }

        /// <summary>
        /// Returns debug information about the component.
        /// </summary>
        /// <returns>String with the information.</returns>
        public override string CompDebugString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.CompDebugString());

            if (!base.Pawn.Dead) stringBuilder.AppendLine($"Severity/day in current light level: {SeverityChangePerDay():F3}");

            return stringBuilder.ToString().TrimEndNewlines();
        }

        /// <summary>
        /// Adjusts the returned string name of the class.
        /// </summary>
        /// <returns>String class name</returns>
        public override string ToString()
        {
            return "CuteboldDarkAdaptation";
        }
    }

    /// <summary>
    /// Hediff that controls the glow curve for cutebolds who have dark adaptation.
    /// </summary>
    public class Hediff_CuteboldDarkAdaptation : HediffWithComps
    {
        /// <summary>The adaptation component.</summary>
        private HediffComp_CuteboldDarkAdaptation adaptationComp;

        /// <summary>Light level of the pawn at the current location.</summary>
        private float LightLevel => adaptationComp.LightLevel;
        /// <summary>If the pawn has eyes that overcome adaptation sickness.</summary>
        private bool IgnoreLightLevel => adaptationComp.IgnoreLightLevel;

        /// <summary>Light level of the pawn at the current location on the last update.</summary>
        private float lastLightLevel = 0f;
        /// <summary>The equiped goggles.</summary>
        private Apparel goggles;
        /// <summary>Reference to the lightSickness Hediff</summary>
        private readonly HediffDef lightSickness = Cutebold_DefOf.CuteboldLightSickness;
        /// <summary>Hediff stage index on last update.</summary>
        private int lastIndex = -1;
        /// <summary>Previous blink value in determening eyes being closed or open.</summary>
        private double blinkLastValue = -1;
        /// <summary>If cutebold is asleep.</summary>
        private bool asleep = false;
        /// <summary>If cutebold is unconscious.</summary>
        private bool unconscious = false;
        /// <summary>If the pawn's eyes are missing.</summary>
        private bool eyesMissing = false;
        /// <summary>If the glow curve should be updated.</summary>
        private bool updateGlowCurve = true;
        /// <summary>Check ticks without all the overhead.</summary>
        private int nextTickToCheck = Current.Game.tickManager.TicksGame;
        /// <summary>If we have checked if the pawn is a mime.</summary>
        private bool mimeChecked = false;

        /// <summary>The defualt glow curve.</summary>
        private static readonly SimpleCurve defaultGlowCurve = new SimpleCurve(new List<CurvePoint>()
        {
            new CurvePoint(0.0f,0.8f),
            new CurvePoint(0.3f,1.0f)
        });
        /// <summary>Default rimworld global work speed in 100% light.</summary>
        private static readonly float defaultLightglobalWorkSpeed = 1.0f;
        /// <summary>Default rimworld global work speed in 0% light</summary>
        private static readonly float defaultDarkglobalWorkSpeed = 0.8f;
        /// <summary>Difference between the two global work speed extremes.</summary>
        private static readonly float globalWorkSpeedDifference = defaultLightglobalWorkSpeed - defaultDarkglobalWorkSpeed;
        /// <summary>If eyes should blink.</summary>
        private static bool EyeBlink => Cutebold_Assemblies.CuteboldSettings.blinkEyes;
        /// <summary>If eyes glow.</summary>
        private static bool EyeGlowEnabled => Cutebold_Assemblies.CuteboldSettings.glowEyes;
        /// <summary>If we should ignore sun sickness.</summary>
        private static bool ignoreSickness = Cutebold_Assemblies.CuteboldSettings.ignoreSickness;
        /// <summary>Minimum work speed in bright light.</summary>
        private static readonly float minimumLightWorkSpeed = Cutebold_Assemblies.CuteboldSettings.darknessOptions == Cutebold_DarknessOptions.Hybrid ? 1.0f : 0.5f;
        /// <summary>Minimum work speed in the darkness.</summary>
        private static readonly float minimumDarkWorkSpeed = 0.5f;

        /// <summary>If the current hediff stage should be visible.</summary>
        public override bool Visible => CurStage.becomeVisible;
        /// <summary>Global work speed glow curve depending on hediff.</summary>
        public SimpleCurve GlowCurve { get; private set; } = new SimpleCurve(defaultGlowCurve);
        /// <summary>Maximum global work speed in 0% light.</summary>
        public float MaxDarkGlobalWorkSpeed { get; private set; } = 0f;
        /// <summary>Maximum global work speed in 100% light.</summary>
        public float MaxLightGlobalWorkSpeed { get; private set; } = 0f;

        /// <summary>Returns true if the cutebold is wearing goggles.</summary>
        public bool WearingGoggles
        {
            get
            {
                if (goggles != null) return true;
                return false;
            }
        }

        /// <summary>
        /// Additional tooltip information.
        /// </summary>
        public override string TipStringExtra
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(base.TipStringExtra);
                stringBuilder.AppendLine("Cutebold_Adaptation_String".Translate() + this.Severity.ToStringPercent());
                if (goggles != null) stringBuilder.AppendLine("Cutebold_Adaptation_WearingGoggles".Translate());
                //stringBuilder.AppendLine("------------------");

                return stringBuilder.ToString();
            }
        }


        /// <summary>
        /// Returns the debug information about the hediff.
        /// </summary>
        /// <returns>String with the information.</returns>
        public override string DebugString()
        {
            StringBuilder debugString = new StringBuilder();
            debugString.Append(base.DebugString());

            debugString.AppendLine(($"LightLevel: {LightLevel}\nmaxLightGlobalWorkSpeed: {MaxLightGlobalWorkSpeed}\nmaxDarkGlobalWorkSpeed: {MaxDarkGlobalWorkSpeed}").Indented());
            debugString.AppendLine($"WearingGoggles: {WearingGoggles}");

            return debugString.ToString();
        }

        /// <summary>
        /// Is called at the end of each hediff tick. This updates the saved light variables, component properties, light sickness hediff, and glow curve, and sets the pawn's texture cache to be dirty.
        /// </summary>
        public override void PostTick()
        {
            base.PostTick();

            if (!mimeChecked) CheckdMime();

            lastLightLevel = LightLevel;
            adaptationComp.LightLevel = Cutebold_Patch_HediffRelated.CuteboldGlowHandler(pawn);

            if (((lastLightLevel >= 0.3f) && (LightLevel < 0.3f)) || ((lastLightLevel <= 0.3f) && (LightLevel > 0.3f)) && !asleep && !unconscious)
            {
                Cutebold_Patch_Body.SetDirty(pawn);
            }

            if (EyeGlowEnabled && EyeBlink && LightLevel < 0.3f && !asleep && !unconscious)
            {
                int offsetTicks = Math.Abs(pawn.HashOffsetTicks());
                double blinkValue = Math.Abs((offsetTicks % 182) / 1.8 - Math.Abs(80 * Math.Sin(offsetTicks / 89)));

                if ((blinkValue < 1 && blinkLastValue >= 1) || (blinkValue >= 1 && blinkLastValue < 1))
                {
                    Cutebold_Patch_Body.SetDirty(pawn);
                }

                blinkLastValue = blinkValue;
            }

            if (Current.Game.tickManager.TicksGame > nextTickToCheck) // Used instead of checking the hash
            {
                nextTickToCheck += 60;

                if (((pawn.CurJob != null && pawn.jobs.curDriver.asleep) != asleep) ||
                ((pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) <= 0.1f) == !unconscious))
                {
                    asleep = (pawn.CurJob != null && pawn.jobs.curDriver.asleep);
                    unconscious = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) <= 0.1f;

                    Cutebold_Patch_Body.SetDirty(pawn);
                }

                UpdateCuteboldCompProperties();

                if (updateGlowCurve || (lastIndex != CurStageIndex))
                {
                    lastIndex = CurStageIndex;

                    UpdateGlowCurve();
                }

                if (!ignoreSickness) UpdateLightSickness();
            }
        }

        /// <summary>
        /// Grabs the adaptation hediff comp.
        /// </summary>
        public void CheckHediffComp()
        {
            if (adaptationComp == null)
            {
                foreach (HediffComp comp in comps)
                {
                    if (comp is HediffComp_CuteboldDarkAdaptation adaptationComp)
                    {
                        this.adaptationComp = adaptationComp;
                    }
                }
            }
        }

        /// <summary>
        /// Changes the eye glow to an orange-red color on mimes from Alpha Animals.
        /// </summary>
        private void CheckdMime()
        {
            if (pawn.health.hediffSet.hediffs.Find((Hediff hediff) => hediff.def.defName == "AA_MimeHediff") != null)
            {
                pawn.TryGetComp<AlienComp>().GetChannel("eye").first = new Color(Rand.Range(0.7f, 0.8f), Rand.Range(0.5f, 0.6f), 0f);
                Cutebold_Patch_Body.SetDirty(pawn);
            }
            mimeChecked = true;
        }

        /// <summary>
        /// Updates the dark adaptation component.
        /// </summary>
        private void UpdateCuteboldCompProperties()
        {
            if (WearingGoggles || eyesMissing || pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight) == 0f)
            {
                adaptationComp.CanSee = false;
            }
            else
            {
                adaptationComp.CanSee = true;
            }

            if (eyesMissing)
            {
                this.Severity = 0f;
            }
        }

        /// <summary>
        /// Updates the light sickness hediff.
        /// </summary>
        private void UpdateLightSickness()
        {
            if (adaptationComp.CanSee && !IgnoreLightLevel && LightLevel > adaptationComp.MaxLightLevel && CurStageIndex > 0 && !pawn.health.hediffSet.HasHediff(lightSickness))
            {
                Hediff hediff = HediffMaker.MakeHediff(lightSickness, pawn);
                hediff.Severity = this.Severity;

                if (!pawn.health.WouldDieAfterAddingHediff(hediff)) pawn.health.AddHediff(hediff);
            }
            else if (pawn.IsHashIntervalTick(480) && pawn.health.hediffSet.HasHediff(lightSickness)) // Check to see if we want to adjust lightSickness far less often than adding it.
            {
                Hediff_CuteboldLightSickness hediff = (Hediff_CuteboldLightSickness)pawn.health.hediffSet.GetFirstHediffOfDef(lightSickness);
                if (LightLevel > adaptationComp.MaxLightLevel && CurStageIndex != 0)
                {
                    hediff.Severity = this.Severity;
                    hediff.ResetTicksToDisappear();
                }
            }
        }

        /// <summary>
        /// Updates the glow curve.
        /// </summary>
        private void UpdateGlowCurve()
        {
            Cutebold_lightDarkAdjustment lightDarkAdjustment = adaptationComp.LightDarkAdjustment[CurStageIndex];

            MaxLightGlobalWorkSpeed = lightDarkAdjustment.Light == -1.0f ? defaultLightglobalWorkSpeed - globalWorkSpeedDifference * lightDarkAdjustment.Multiplier : lightDarkAdjustment.Light;
            MaxDarkGlobalWorkSpeed = lightDarkAdjustment.Dark == -1.0f ? defaultDarkglobalWorkSpeed + globalWorkSpeedDifference * lightDarkAdjustment.Multiplier : lightDarkAdjustment.Dark;

            if (MaxLightGlobalWorkSpeed < minimumLightWorkSpeed) MaxLightGlobalWorkSpeed = minimumLightWorkSpeed;
            if (MaxDarkGlobalWorkSpeed < minimumDarkWorkSpeed) MaxDarkGlobalWorkSpeed = minimumDarkWorkSpeed;

            if (WearingGoggles || (MaxLightGlobalWorkSpeed == defaultLightglobalWorkSpeed && MaxDarkGlobalWorkSpeed == defaultDarkglobalWorkSpeed))
            {
                GlowCurve = new SimpleCurve(defaultGlowCurve);
            }
            else if (IgnoreLightLevel)
            {
                GlowCurve.SetPoints(new List<CurvePoint>()
                                {
                                    new CurvePoint(0.0f,MaxDarkGlobalWorkSpeed),
                                    new CurvePoint(1.0f,MaxDarkGlobalWorkSpeed)
                                });
            }
            else
            {
                GlowCurve.SetPoints(new List<CurvePoint>()
                                {
                                    new CurvePoint(adaptationComp.MaxLightLevel,MaxDarkGlobalWorkSpeed),
                                    new CurvePoint(1.0f,MaxLightGlobalWorkSpeed)
                                });
            }
        }

        /// <summary>
        /// Updates the eyesMissing bool.
        /// </summary>
        public void UpdateEyes()
        {
            if (pawn.Dead) return;

            CheckHediffComp();

            bool leftEyeMissing = !pawn.health.hediffSet.GetNotMissingParts().Any(eye => eye.untranslatedCustomLabel == "left eye");
            bool rightEyeMissing = !pawn.health.hediffSet.GetNotMissingParts().Any(eye => eye.untranslatedCustomLabel == "right eye");

            if (leftEyeMissing && rightEyeMissing)
            {
                eyesMissing = true;
                adaptationComp.IgnoreLightLevel = false;
            }
            else
            {
                eyesMissing = false;

                bool leftEyeSpecial = pawn.health.hediffSet.hediffs.Any(hediff => hediff.Part?.untranslatedCustomLabel == "left eye"
                    && adaptationComp.SpecialEyes.Contains(hediff.def.defName));
                bool rightEyeSpecial = pawn.health.hediffSet.hediffs.Any(hediff => hediff.Part?.untranslatedCustomLabel == "right eye"
                    && adaptationComp.SpecialEyes.Contains(hediff.def.defName));

                if ((leftEyeSpecial || leftEyeMissing) && (rightEyeSpecial || rightEyeMissing))
                {
                    adaptationComp.IgnoreLightLevel = true;
                }
                else
                {
                    adaptationComp.IgnoreLightLevel = false;
                }
            }

            updateGlowCurve = true;
        }

        /// <summary>
        /// Updates the reference to the goggles if worn.
        /// </summary>
        public void UpdateGoggles()
        {
            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                if (apparel.def == Cutebold_DefOf.Cutebold_Goggles/* || apparel.def == Cutebold_DefOf.Cutebold_AdvancedGoggles*/)
                {
                    goggles = apparel;
                    updateGlowCurve = true;
                    return;
                }
            }

            goggles = null;
            updateGlowCurve = true;
        }

        /// <summary>
        /// Updates the ignore sun sickness flag.
        /// </summary>
        public static void UpdateIgnoreSickness()
        {
            ignoreSickness = Cutebold_Assemblies.CuteboldSettings.ignoreSickness;
        }

        // Mod Developer: testing adaptation
#if DEBUG
#pragma warning disable IDE0051 // Remove unused private members

        /// <summary>
        /// Decreases dark adaptation on a pawn.
        /// </summary>
        /// <param name="p">Pawn to decrease the adaptation of.</param>
#if RWPre1_4
        [DebugAction(category: "Cutebold Mod", name: "Increase Adaptation by 20%", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
#else
        [DebugAction(category: "Cutebold Mod", name: "Increase Adaptation by 20%", hideInSubMenu: true, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
#endif
        private static void IncreaseAdaptation(Pawn p)
        {
            if (p.health.hediffSet.GetFirstHediffOfDef(Cutebold_DefOf.CuteboldDarkAdaptation) is Hediff_CuteboldDarkAdaptation hediff)
            {
                hediff.Severity += 0.2f;
                DebugActionsUtility.DustPuffFrom(p);
            }
        }


        /// <summary>
        /// Decreases dark adaptation on a pawn.
        /// </summary>
        /// <param name="p">Pawn to increase the adaptation of.</param>
#if RWPre1_4
        [DebugAction(category: "Cutebold Mod", name: "Decrease Adaptation by 20%", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
#else
        [DebugAction(category: "Cutebold Mod", name: "Decrease Adaptation by 20%", hideInSubMenu: true, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
#endif
        private static void DecreaseAdaptation(Pawn p)
        {
            if (p.health.hediffSet.GetFirstHediffOfDef(Cutebold_DefOf.CuteboldDarkAdaptation) is Hediff_CuteboldDarkAdaptation hediff)
            {
                hediff.Severity -= 0.2f;
                DebugActionsUtility.DustPuffFrom(p);
            }
        }
#pragma warning restore IDE0051 // Remove unused private members
#endif
    }
}
