using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace XenogermTraderStock
{
    public class StockGenerator_Xenogerms : StockGenerator
    {
        public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
        {
            // The consumer (ThingSetMaker_TraderStock) discards anything whose
            // def traders cannot sell; another mod may have made Xenogerm such
            // a def since startup (see XenogermTradeability).
            XenogermTradeability.EnsureTraderCanSell();

            // The commonality strategy is pool-relative (the bell curve centres
            // on the pool's median price), so weights are computed in one pass
            // over everything sellable rather than per option.
            List<XenogermPool.Entry> pool = XenogermPool.Sellable();
            if (pool.Count == 0)
            {
                yield break;
            }

            float[] weights = XenogermCommonality.Weights(XenogermPool.Prices(pool),
                XenogermTraderStockMod.Settings.selectionStrategy);

            // The settings override is read live, keyed by the owning trader
            // kind (StockGenerator.trader, set by ResolveReferences), with the
            // XML countRange as the shipped default. Nothing is ever written
            // back onto the def, so a settings change needs no restart.
            IntRange range = XenogermTraderStockMod.Settings.GetTraderCountRange(trader?.defName) ?? countRange;
            int count = range.RandomInRange;
            for (int i = 0; i < count; i++)
            {
                yield return XenogermPool.MakeRandom(pool, weights);
            }
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            return thingDef == ThingDefOf.Xenogerm;
        }

        // Every trader kind carrying this generator, with the generator's XML
        // countRange as that trader's shipped default. Feeds the "Stock
        // quantity" settings rows, so a trader patched by another mod (or the
        // Gene Trader def, present only while that mod is loaded) gets its row
        // without this mod knowing its name.
        public static IEnumerable<(TraderKindDef trader, IntRange xmlCountRange)> PatchedTraders()
        {
            foreach (TraderKindDef trader in DefDatabase<TraderKindDef>.AllDefsListForReading)
            {
                foreach (StockGenerator generator in trader.stockGenerators)
                {
                    if (generator is StockGenerator_Xenogerms xenogerms)
                    {
                        yield return (trader, xenogerms.countRange);
                    }
                }
            }
        }
    }
}
