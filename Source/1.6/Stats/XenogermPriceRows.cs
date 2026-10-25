using System.Collections.Generic;
using Verse;

namespace XenogermTraderStock
{
    // The pricing formula as display rows: one per component, each a Keyed
    // label (key plus placeholders, translated only when Label() is called)
    // and the silver it adds. The settings grid tooltip and the info card's
    // StatPart_XenogermValue explanation both read their rows from here, so
    // the two hovers word the formula identically and cannot drift. Pure
    // until Label(): row selection and arithmetic are unit-tested headless.
    public static class XenogermPriceRows
    {
        public readonly struct Row
        {
            public readonly string Key;
            public readonly object[] Args;
            public readonly float Silver;

            public Row(string key, float silver, params object[] args)
            {
                Key = key;
                Silver = silver;
                Args = args;
            }

            // Translated label. ToString()d on purpose (RawText, tags kept):
            // a bare TaggedString riding a + chain converts via StripTags.
            public string Label()
            {
                var args = new NamedArgument[Args.Length];
                for (int i = 0; i < Args.Length; i++)
                {
                    args[i] = new NamedArgument(Args[i], null);
                }
                return Key.Translate(args).ToString();
            }
        }

        // Vanilla's base xenogerm value, the one row every germ has.
        public static Row Base => new Row("XTS_PriceBase", XenogermPricing.BaseXenogermValue);

        // The flat preset premium (the key is the preset wording; the info card
        // swaps in XTS_PriceScenario for a scenario-template germ).
        public static Row Preset(XenogermTraderStockSettings settings)
        {
            return new Row("XTS_PricePreset", settings.basePresetValue);
        }

        // The three per-gene-stat rows, always all three, each "count × silver
        // per point" with the product as its silver. Callers that want vanilla
        // StatPart brevity drop the zero-silver ones.
        public static List<Row> Genes(XenogermPricing.PricingBreakdown breakdown,
            XenogermTraderStockSettings settings)
        {
            return new List<Row>
            {
                new Row("XTS_PriceMetabolism",
                    breakdown.AbsoluteMetabolism * settings.valuePerMetabolism,
                    breakdown.AbsoluteMetabolism, settings.valuePerMetabolism.ToString("F0")),
                new Row("XTS_PriceComplexity",
                    breakdown.Complexity * settings.valuePerComplexity,
                    breakdown.Complexity, settings.valuePerComplexity.ToString("F0")),
                new Row("XTS_PriceArchite",
                    breakdown.Archites * settings.valuePerArchite,
                    breakdown.Archites, settings.valuePerArchite.ToString("F0")),
            };
        }
    }
}
