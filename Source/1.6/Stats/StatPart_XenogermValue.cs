using System.Text;
using RimWorld;
using Verse;

namespace XenogermTraderStock
{
    // Adds market value to trader-sold xenogerms (CompXenotypeSource names a preset or a
    // scenario template). Player-crafted xenogerms (empty comp) retain their base 20 silver value.
    // Pricing formula is defined in XenogermPricing and configurable via mod settings.
    //
    // Reads the raw comp on purpose, not XenogermIdentity: a germ that merely CARRIES a
    // preset's genes (a ReSplice-duplicated copy, a player-assembled match) implants as the
    // preset but is not trader stock, and pricing it as such would make buy-one-copy-many a
    // silver printer.
    public class StatPart_XenogermValue : StatPart
    {
        private static XenogermTraderStockSettings Settings => XenogermTraderStockMod.Settings;

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!req.HasThing)
                return;

            if (!(req.Thing is Xenogerm xenogerm))
                return;

            // Only add value for preset xenotype xenogerms (from traders)
            var comp = xenogerm.TryGetComp<CompXenotypeSource>();
            if (comp?.IsTraderSold != true)
                return;

            var geneSet = xenogerm.GeneSet;
            if (geneSet == null)
                return;

            val += XenogermPricing.Calculate(geneSet.GenesListForReading).Premium;
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!req.HasThing)
                return null;

            if (!(req.Thing is Xenogerm xenogerm))
                return null;

            var comp = xenogerm.TryGetComp<CompXenotypeSource>();
            if (comp?.IsTraderSold != true)
                return null;

            var geneSet = xenogerm.GeneSet;
            if (geneSet == null)
                return null;

            // Same rows the settings grid tooltip shows (XenogermPriceRows), in
            // vanilla StatPart shape: "label: +$n", zero-silver gene rows dropped.
            // The premium row names the germ's source the way vanilla's own
            // parts parenthesise their input ("Quality (Excellent): x150%").
            var settings = Settings;
            var breakdown = XenogermPricing.Calculate(geneSet.GenesListForReading);
            var sb = new StringBuilder();

            string sourceLabel = comp.sourceXenotype != null
                ? "XTS_PricePreset".Translate().ToString() + " (" + comp.sourceXenotype.LabelCap.ToString() + ")"
                : "XTS_PriceScenario".Translate().ToString() + " (" + comp.sourceCustomName + ")";
            sb.AppendLine(sourceLabel + ": " + Offset(XenogermPriceRows.Preset(settings).Silver));

            foreach (var row in XenogermPriceRows.Genes(breakdown, settings))
            {
                if (row.Silver > 0f)
                {
                    sb.AppendLine(row.Label() + ": " + Offset(row.Silver));
                }
            }

            return sb.ToString().TrimEnd();
        }

        private static string Offset(float silver)
        {
            return silver.ToStringByStyle(ToStringStyle.Money, ToStringNumberSense.Offset);
        }
    }
}
