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
        // The lowest tech level a faction needs before its quests offer germs.
        public const TechLevel MinGiverTechLevel = TechLevel.Industrial;

        // The most a germ may be worth where the caller passes no budget, which
        // is every world loot pool.
        public const float MaxUnbudgetedValue = 2500f;

        // giverTech is null when no faction stands behind the reward, as with
        // an item stash; those are always allowed.
        public static bool GiverAllowed(TechLevel? giverTech)
        {
            return !giverTech.HasValue || giverTech.Value >= MinGiverTechLevel;
        }

        // One pool option's contribution to a genepack's weight in that pool:
        // the option's weight split evenly across the defs it picks from. Exact
        // for the count makers, which pick uniformly, and an estimate for the
        // value-budgeted makers behind quest rewards.
        public static float GenepackShare(float optionWeight, int defCount, bool offersGenepack)
        {
            return offersGenepack && defCount > 0 && optionWeight > 0f ? optionWeight / defCount : 0f;
        }

        // The price range a germ must fall in to be offered. A budgeted pool
        // (quest rewards) bounds it by the remaining budget and vanilla's
        // "no trivially cheap items" share of it; cap is MaxUnbudgetedValue,
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
