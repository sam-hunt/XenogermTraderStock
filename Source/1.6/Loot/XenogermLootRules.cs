using System;
using RimWorld;

namespace XenogermTraderStock
{
    // Which settings row a ThingSetMaker_Xenogerms answers to. Set in the XML
    // patch that adds the maker to a pool.
    public enum XenogermLootChannel
    {
        QuestReward,
        WorldLoot,
    }

    // The loot set maker's decisions as pure functions, so the suite can cover
    // them headless.
    public static class XenogermLootRules
    {
        // giverTech is null when no faction stands behind the reward.
        public static bool GiverAllowed(TechLevel? giverTech, TechLevel floor, bool allowFactionless)
        {
            return giverTech.HasValue ? giverTech.Value >= floor : allowFactionless;
        }

        // The price range a germ must fall in to be offered. A budgeted pool
        // (quest rewards) bounds it by the remaining budget and vanilla's
        // "no trivially cheap items" share of it; cap is the settings ceiling,
        // passed only where it applies.
        public static void PriceBand(float? budgetMax, float? minItemShare, float? maxThingValue, float? cap,
            out float min, out float max)
        {
            min = 0f;
            max = float.MaxValue;
            if (budgetMax.HasValue)
            {
                max = budgetMax.Value;
                min = budgetMax.Value * (minItemShare ?? 0f);
            }
            if (maxThingValue.HasValue)
            {
                max = Math.Min(max, maxThingValue.Value);
            }
            if (cap.HasValue)
            {
                max = Math.Min(max, cap.Value);
            }
        }
    }
}
