using Verse;

namespace Cutebold_Assemblies
{
    /// <summary>
    /// Light sickness hediff handler. Allows for reseting the ticks to disappear.
    /// </summary>
    public class Hediff_CuteboldLightSickness : HediffWithComps
    {
        /// <summary>The initial ticks when this hediff is created.</summary>
        private int maxTicksToDisappear = 0;

        /// <summary>
        /// Resets the number of ticks until the hediff disappears.
        /// </summary>
        public void resetTicksToDisappear()
        {
            foreach (HediffComp comp in comps)
            {
                if (comp is HediffComp_Disappears disappearComp)
                {
                    disappearComp.ticksToDisappear = maxTicksToDisappear;
                }
            }
        }

        /// <summary>
        /// Method called after the hediff is made, saves the initial number of ticks to disappear.
        /// </summary>
        public override void PostMake()
        {
            base.PostMake();
            foreach (HediffComp comp in comps)
            {
                if (comp is HediffComp_Disappears disappearComp)
                {
                    maxTicksToDisappear = disappearComp.ticksToDisappear;
                }
            }
        }
    }
}
