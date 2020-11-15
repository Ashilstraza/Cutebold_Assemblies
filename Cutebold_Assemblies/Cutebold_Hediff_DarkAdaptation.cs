﻿using RimWorld;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace Cutebold_Assemblies
{
    /// <summary>
    /// .xml field for the HediffCompProperties_CuteboldDarkAdaptation component. Used for setting the light/dark adjustments.
    /// </summary>
    public class Cutebold_lightDarkAdjustment
    {
        /// <summary>Flat value that the global work speed should be in 0% light.</summary>
        private float dark = -1.0f;
        /// <summary>Flat value that the global work sleed should be in 100% light.</summary>
        private float light = -1.0f;
        /// <summary>Multiplier for the difference between the default global work speed values.</summary>
        private float multiplier = float.MaxValue;

        /// <summary>Flat value that the global work speed should be in 0% light.</summary>
        public float Dark { get => dark; }
        /// <summary>Flat value that the global work sleed should be in 100% light.</summary>
        public float Light { get => light; }
        /// <summary>Multiplier for the difference between the default global work speed values.</summary>
        public float Multiplier { get => multiplier; }

        /// <summary>
        /// Checks the given fields and returns any issues with the configuration.
        /// </summary>
        /// <returns>Any issues found.</returns>
        public virtual IEnumerable<string> ConfigErrors()
        {
            if (dark == -1.0f && light == -1.0f && multiplier == float.MaxValue)
            {
                yield return "a lightDarkAdjustment is empty in .xml";
            }
            if ((dark == -1.0f && light != -1.0f) || (dark != -1.0f && light == -1.0f))
            {
                yield return "lightDarkAdjustment has only a single light or dark value";
            }
            if ((dark != -1.0f || light != -1.0f) && multiplier != float.MaxValue)
            {
                yield return "lightDarkAdjustment has both a light and/or dark value along with a multiplier, the light and/or dark value will be used instead of the multiplier";
            }
        }
    }

    /// <summary>
    /// Property handler for configuring the dark adaptation hediff component.
    /// </summary>
    public class HediffCompProperties_CuteboldDarkAdaptation : HediffCompProperties
    {
        /// <summary>How much we should gain/lose per day max.</summary>
        private float maxSeverityPerDay;
        /// <summary>The maximum light level before losing severity.</summary>
        private float maxLightLevel;
        /// <summary>The minimum light level before gaining severity.</summary>
        private float minLightLevel;
        /// <summary>List of how the light level affects global work speed.</summary>
        private List<Cutebold_lightDarkAdjustment> lightDarkAdjustment;

        /// <summary>How much we should gain/lose per day max.</summary>
        public float MaxSeverityPerDay { get => maxSeverityPerDay; }
        /// <summary>The maximum light level before losing severity.</summary>
        public float MaxLightLevel { get => maxLightLevel; }
        /// <summary>The minimum light level before gaining severity.</summary>
        public float MinLightLevel { get => minLightLevel; }
        /// <summary>List of how the light level affects global work speed.</summary>
        public List<Cutebold_lightDarkAdjustment> LightDarkAdjustment { get => lightDarkAdjustment; }

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
        public virtual IEnumerable<string> ConfigErrors()
        {
            if (maxSeverityPerDay == 0.0f)
            {
                yield return "HediffCompProperties_CuteboldDarkAdaptation maxSeverityPerDay is 0";
            }
            if (minLightLevel > maxLightLevel)
            {
                yield return "HediffCompProperties_CuteboldDarkAdaptation minLightLevel is greater than maxLightLevel";
            }
            if (lightDarkAdjustment == null)
            {
                yield return "HediffCompProperties_CuteboldDarkAdaptation lightDarkAdjustment is null";
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
        /// <summary>If the pawn can see.</summary>
        public bool CanSee = true;
        /// <summary>The maximum light level before adaptation starts to decrease.</summary>
        public float MaxLightLevel => Props.MaxLightLevel;
        /// <summary>The minimum light level before adaptation starts to increase.</summary>
        public float MinLightLevel => Props.MinLightLevel;
        /// <summary>List of the adjustment values for each hediff stage.</summary>
        public List<Cutebold_lightDarkAdjustment> LightDarkAdjustment => Props.LightDarkAdjustment;

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
            if (LightLevel < MinLightLevel) return Props.MaxSeverityPerDay;
            else if (LightLevel > MaxLightLevel) return -Props.MaxSeverityPerDay * 2;
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

            if (!base.Pawn.Dead) stringBuilder.AppendLine("severity/day in current light level: " + SeverityChangePerDay().ToString("F3"));

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
        /// <summary>Light level of the pawn at the current location.</summary>
        private float lightLevel = 0f;
        /// <summary>Indicates if goggles are equiped.</summary>
        private bool gogglesEquiped = false;
        /// <summary>The equiped goggles.</summary>
        private Apparel goggles;
        /// <summary>The defualt glow curve.</summary>
        private static readonly SimpleCurve defaultGlowCurve = new SimpleCurve(new List<CurvePoint>()
        {
            new CurvePoint(0.0f,0.8f),
            new CurvePoint(0.3f,1.0f)
        });
        /// <summary>Default rimworld global work speed in 100% light.</summary>
        private static float defaultLightglobalWorkSpeed = 1.0f;
        /// <summary>Default rimworld global work speed in 0% light</summary>
        private static float defaultDarkglobalWorkSpeed = 0.8f;
        /// <summary>Difference between the two global work speed extremes.</summary>
        private static float globalWorkSpeedDifference = defaultLightglobalWorkSpeed - defaultDarkglobalWorkSpeed;
        /// <summary>Reference to the lightSickness Hediff</summary>
        private HediffDef lightSickness = Cutebold_DefOf.CuteboldLightSickness;
        /// <summary>Hediff stage index on last update.</summary>
        private int lastIndex = -1;
        /// <summary>The adaptation component.</summary>
        private HediffComp_CuteboldDarkAdaptation adaptationComp;

        /// <summary>If the current hediff stage should be visible.</summary>
        public override bool Visible => CurStage.becomeVisible;
        /// <summary>Global work speed glow curve depending on hediff.</summary>
        public SimpleCurve GlowCurve { get; private set; } = new SimpleCurve(defaultGlowCurve);
        /// <summary>Maximum global work speed in 0% light.</summary>
        public float MaxDarkGlobalWorkSpeed { get; private set; } = 0f;
        /// <summary>Maximum global work speed in 100% light.</summary>
        public float MaxLightGlobalWorkSpeed { get; private set; } = 0f;

        /// <summary>
        /// Returns the debug information about the hediff.
        /// </summary>
        /// <returns>String with the information.</returns>
        public override string DebugString()
        {
            StringBuilder debugString = new StringBuilder();
            debugString.Append(base.DebugString());

            debugString.AppendLine(("lightLevel: " + lightLevel + "\nmaxLightGlobalWorkSpeed: " + MaxLightGlobalWorkSpeed + "\nmaxDarkGlobalWorkSpeed: " + MaxDarkGlobalWorkSpeed).Indented());

            return debugString.ToString();
        }

        /// <summary>
        /// Is called at the end of each hediff tick. This updates the saved light variables, component properties, light sickness hediff, and glow curve.
        /// </summary>
        public override void PostTick()
        {
            base.PostTick();

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

            if (pawn.IsHashIntervalTick(60))
            {
                var updateGlow = false;

                if (pawn.Spawned) this.lightLevel = pawn.Map.glowGrid.GameGlowAt(pawn.Position);
                else this.lightLevel = 0.75f;

                if (goggles != null && goggles.Wearer != pawn)
                {
                    goggles = null;
                    gogglesEquiped = false;
                    updateGlow = true;
                }

                if (goggles == null)
                {
                    foreach(Apparel apparel in pawn.apparel.WornApparel)
                    {
                        if(apparel.def == Cutebold_DefOf.Cutebold_Goggles)
                        {
                            goggles = apparel;
                            gogglesEquiped = true;
                            updateGlow = true;
                            break;
                        }
                    }
                }

                UpdateCuteboldCompProperties();

                if (updateGlow || (lastIndex != CurStageIndex))
                {
                    lastIndex = CurStageIndex;

                    UpdateGlowCurve();
                }

                UpdateLightSickness();
            }
        }

        /// <summary>
        /// Updates the dark adaptation component.
        /// </summary>
        private void UpdateCuteboldCompProperties()
        {
            adaptationComp.LightLevel = this.lightLevel;

            if (gogglesEquiped || (pawn.CurJob != null && pawn.jobs.curDriver.asleep) || pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight) == 0f || pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) <= 0.1f)
            {
                adaptationComp.CanSee = false;
            }
            else adaptationComp.CanSee = true;
        }

        /// <summary>
        /// Updates the light sickness hediff.
        /// </summary>
        private void UpdateLightSickness()
        {
            if (adaptationComp.CanSee && lightLevel > adaptationComp.MaxLightLevel && CurStageIndex > 0 && !pawn.health.hediffSet.HasHediff(lightSickness))
            {
                Hediff hediff = HediffMaker.MakeHediff(lightSickness, pawn);
                hediff.Severity = this.Severity;

                if (!pawn.health.WouldDieAfterAddingHediff(hediff)) pawn.health.AddHediff(hediff);
            }
            else if (pawn.IsHashIntervalTick(480) && pawn.health.hediffSet.HasHediff(lightSickness)) // Check to see if we want to adjust lightSickness far less often than adding it.
            {
                Hediff_CuteboldLightSickness hediff = (Hediff_CuteboldLightSickness)pawn.health.hediffSet.GetFirstHediffOfDef(lightSickness);
                if (lightLevel > adaptationComp.MaxLightLevel && CurStageIndex != 0)
                {
                    hediff.Severity = this.Severity;
                    hediff.resetTicksToDisappear();
                }
            }
        }

        /// <summary>
        /// Updates the glow curve.
        /// </summary>
        private void UpdateGlowCurve()
        {
            var lightDarkAdjustment = adaptationComp.LightDarkAdjustment[CurStageIndex];

            MaxLightGlobalWorkSpeed = lightDarkAdjustment.Light == -1.0f ? defaultLightglobalWorkSpeed - globalWorkSpeedDifference * lightDarkAdjustment.Multiplier : lightDarkAdjustment.Light;
            MaxDarkGlobalWorkSpeed = lightDarkAdjustment.Dark == -1.0f ? defaultDarkglobalWorkSpeed + globalWorkSpeedDifference * lightDarkAdjustment.Multiplier : lightDarkAdjustment.Dark;

            if (gogglesEquiped || (MaxLightGlobalWorkSpeed == defaultLightglobalWorkSpeed && MaxDarkGlobalWorkSpeed == defaultDarkglobalWorkSpeed))
            {
                GlowCurve = new SimpleCurve(defaultGlowCurve);
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
    }
}