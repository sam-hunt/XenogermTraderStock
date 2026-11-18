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
    // The option is as likely as a genepack at 100% commonality. Its XML weight
    // is a placeholder; ExtraSelectionWeightFactor supplies the weight a
    // genepack has in the same pool (XenogermLootRules.GenepackShare over the
    // sibling options) times the commonality setting. So the rate follows the
    // DLC mix and any mod that reworks the pool, and a pool that offers no
    // genepack offers no germ.
    //
    // That weight is state on a def instance, so it is resolved on first use
    // rather than at startup: a play-data reload builds new makers.
    //
    // Nothing is written onto the def. CanGenerateSub drops the option out of
    // the roll when the channel is off, the giver is refused or no sellable
    // xenotype fits the price band. The band keeps the germ inside the budget
    // the caller passes. A quest reward is then measured at the germ's real
    // market value, and RewardsGenerator makes up a shortfall with filler
    // items or the social half of the reward.
    //
    // CanGenerateSub runs outside vanilla's exception guard, so it must not
    // throw.
    //
    // Reward_ItemsStandard is also rolled for peace talks, refugee rewards and
    // item stashes. The first two pass their faction and meet the tech floor
    // like a quest giver; a stash passes none and is always allowed. A caller
    // that passes no budget, as every world loot pool does, gets no ceiling:
    // vanilla sets none on the items those pools already hold.
    public class ThingSetMaker_Xenogerms : ThingSetMaker
    {
        public XenogermLootChannel channel;

        private float? genepackWeight;

        private static XenogermTraderStockSettings Settings => XenogermTraderStockMod.Settings;

        private float SelectionWeight
        {
            get
            {
                if (!genepackWeight.HasValue)
                {
                    ResolveGenepackWeights();
                    // Still unset means no random-option pool holds this maker.
                    genepackWeight = genepackWeight ?? 0f;
                }
                return genepackWeight.Value * Settings.LootCommonalityFactor(channel);
            }
        }

        public override float ExtraSelectionWeightFactor(ThingSetMakerParams parms)
        {
            return SelectionWeight;
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

            if (Settings.LootCommonalityFactor(channel) <= 0f || SelectionWeight <= 0f)
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

            if (channel == XenogermLootChannel.QuestReward && !XenogermLootRules.GiverAllowed(GiverTechLevel(parms)))
            {
                return false;
            }

            XenogermLootRules.PriceBand(parms.totalMarketValueRange?.max, parms.minSingleItemMarketValuePct,
                parms.maxThingMarketValue, out float min, out float max);

            pool = XenogermPool.Sellable();
            weights = XenogermCommonality.WeightsWithin(XenogermPool.Prices(pool),
                Settings.selectionStrategy, min, max);
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

        // Walks every set maker def for the pools that hold one of these makers
        // and gives each its genepack weight. One pass serves every maker, so a
        // load pays for it once.
        private static void ResolveGenepackWeights()
        {
            foreach (ThingSetMakerDef def in DefDatabase<ThingSetMakerDef>.AllDefsListForReading)
            {
                Visit(def.root, def);
            }
        }

        private static void Visit(ThingSetMaker maker, ThingSetMakerDef def)
        {
            if (maker is ThingSetMaker_RandomOption pool && pool.options != null)
            {
                foreach (ThingSetMaker_RandomOption.Option option in pool.options)
                {
                    if (option.thingSetMaker is ThingSetMaker_Xenogerms germs)
                    {
                        germs.genepackWeight = GenepackWeightIn(pool);
                        if (germs.genepackWeight <= 0f)
                        {
                            Log.Message("[Xenogerm Trader Stock] " + def.defName
                                + " offers no genepack, so no xenogerm will appear in it.");
                        }
                    }
                    else
                    {
                        Visit(option.thingSetMaker, def);
                    }
                }
            }
            else if (maker is ThingSetMaker_Sum sum && sum.options != null)
            {
                foreach (ThingSetMaker_Sum.Option option in sum.options)
                {
                    Visit(option.thingSetMaker, def);
                }
            }
            else if (maker is ThingSetMaker_Conditional conditional)
            {
                Visit(conditional.thingSetMaker, def);
            }
        }

        // Reads only each sibling's own filter, which every vanilla maker that
        // picks a genepack carries, so nothing here depends on a running game.
        private static float GenepackWeightIn(ThingSetMaker_RandomOption pool)
        {
            ThingDef genepack = ThingDefOf.Genepack;
            float total = 0f;
            foreach (ThingSetMaker_RandomOption.Option option in pool.options)
            {
                ThingFilter filter = option.thingSetMaker?.fixedParams.filter;
                if (filter != null && genepack != null)
                {
                    total += XenogermLootRules.GenepackShare(option.weight, filter.AllowedDefCount,
                        filter.Allows(genepack));
                }
            }
            return total;
        }
    }
}
