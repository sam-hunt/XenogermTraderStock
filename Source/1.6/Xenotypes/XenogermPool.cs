using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace XenogermTraderStock
{
    // The sellable pool as one list: every preset and scenario xenotype
    // XenotypeEligibility allows, each with the price the commonality weights
    // are computed from. Shared by trader stock and the loot set maker so both
    // draw from the same xenotypes at the same relative rarity.
    public static class XenogermPool
    {
        public readonly struct Entry
        {
            public readonly XenotypeDef Preset;
            public readonly CustomXenotype Custom;
            public readonly float Price;

            public Entry(XenotypeDef preset, CustomXenotype custom, float price)
            {
                Preset = preset;
                Custom = custom;
                Price = price;
            }
        }

        public static List<Entry> Sellable()
        {
            // The custom pool is per-game, so the startup seeding pass cannot
            // have covered it.
            XenotypeEligibility.SeedUnseen();

            var entries = new List<Entry>();
            foreach (XenotypeDef xenotype in DefDatabase<XenotypeDef>.AllDefsListForReading)
            {
                if (XenotypeEligibility.IsSellable(xenotype))
                {
                    entries.Add(new Entry(xenotype, null, XenogermPricing.EstimateMarketValue(xenotype.genes)));
                }
            }

            List<CustomXenotype> customs = Current.Game?.customXenotypeDatabase?.customXenotypes;
            if (customs != null)
            {
                foreach (CustomXenotype custom in customs)
                {
                    if (XenotypeEligibility.IsSellable(custom))
                    {
                        entries.Add(new Entry(null, custom, XenogermPricing.EstimateMarketValue(custom.genes)));
                    }
                }
            }

            return entries;
        }

        public static float[] Prices(List<Entry> entries)
        {
            var prices = new float[entries.Count];
            for (int i = 0; i < prices.Length; i++)
            {
                prices[i] = entries[i].Price;
            }
            return prices;
        }

        // Weights are parallel to entries and must have a positive total.
        public static Xenogerm MakeRandom(List<Entry> entries, float[] weights)
        {
            int index = Enumerable.Range(0, entries.Count).RandomElementByWeight(i => weights[i]);
            Entry entry = entries[index];
            return entry.Custom != null
                ? XenogermFactory.CreateForCustomXenotype(entry.Custom)
                : XenogermFactory.CreateForXenotype(entry.Preset);
        }
    }
}
