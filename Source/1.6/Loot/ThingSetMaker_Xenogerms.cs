using System.Collections.Generic;
using RimWorld;
using Verse;

namespace XenogermTraderStock
{
    // One trader-grade xenogerm as an option in a vanilla reward or loot pool,
    // added by Patches_Loot.xml. Vanilla's own makers cannot carry it: they
    // plan on the def's value (20 silver) and build the item with a bare
    // MakeThing, which leaves a xenogerm without genes.
    //
    // Everything is read live from settings, nothing is written onto the def:
    // the commonality slider scales the option's XML weight through
    // ExtraSelectionWeightFactor, and CanGenerateSub drops the option out of
    // the roll when the channel is off, the giver is refused or no sellable
    // xenotype fits the price band. The band keeps the germ inside the budget
    // the caller passes, and the parent measures the finished germ at its real
    // market value, so the rest of a quest reward is balanced around it.
    //
    // CanGenerateSub runs outside vanilla's exception guard, so it must not
    // throw.
    //
    // Reward_ItemsStandard is also rolled for peace talks, item stashes and
    // refugee rewards. Those pass no giver, so the factionless toggle decides
    // them, and where a caller passes no budget the loot value ceiling applies.
    public class ThingSetMaker_Xenogerms : ThingSetMaker
    {
        public XenogermLootChannel channel;

        private static XenogermTraderStockSettings Settings => XenogermTraderStockMod.Settings;

        public override float ExtraSelectionWeightFactor(ThingSetMakerParams parms)
        {
            return Settings.LootCommonalityFactor(channel);
        }

        protected override bool CanGenerateSub(ThingSetMakerParams parms)
        {
            return TryWeighPool(parms, out _, out _);
        }

        protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
        {
            if (TryWeighPool(parms, out List<XenogermPool.Entry> pool, out float[] weights))
            {
                outThings.Add(XenogermPool.MakeRandom(pool, weights));
            }
        }

        protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
        {
            yield return ThingDefOf.Xenogerm;
        }

        private bool TryWeighPool(ThingSetMakerParams parms, out List<XenogermPool.Entry> pool, out float[] weights)
        {
            pool = null;
            weights = null;

            XenogermTraderStockSettings settings = Settings;
            if (settings.LootCommonalityFactor(channel) <= 0f)
            {
                return false;
            }

            ThingDef xenogerm = ThingDefOf.Xenogerm;
            if (xenogerm == null
                || (parms.countRange.HasValue && parms.countRange.Value.max <= 0)
                || (parms.validator != null && !parms.validator(xenogerm))
                || (parms.filter != null && !parms.filter.Allows(xenogerm)))
            {
                return false;
            }

            if (channel == XenogermLootChannel.QuestReward
                && !XenogermLootRules.GiverAllowed(GiverTechLevel(parms),
                    settings.questRewardMinTechLevel, settings.questRewardAllowFactionless))
            {
                return false;
            }

            float? budgetMax = parms.totalMarketValueRange?.max;
            bool capped = channel == XenogermLootChannel.WorldLoot || !budgetMax.HasValue;
            XenogermLootRules.PriceBand(budgetMax, parms.minSingleItemMarketValuePct, parms.maxThingMarketValue,
                capped ? settings.lootMaxValue : (float?)null, out float min, out float max);

            pool = XenogermPool.Sellable();
            weights = XenogermCommonality.WeightsWithin(XenogermPool.Prices(pool),
                settings.selectionStrategy, min, max);
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] > 0f)
                {
                    return true;
                }
            }
            return false;
        }

        // A faction whose def declares no tech level counts as no giver.
        private static TechLevel? GiverTechLevel(ThingSetMakerParams parms)
        {
            TechLevel? level = parms.techLevel ?? parms.makingFaction?.def?.techLevel;
            return level == TechLevel.Undefined ? null : level;
        }
    }
}
