using System.Collections.Generic;
using System.Runtime.Serialization;
using Verse;

namespace XenogermTraderStock.Tests
{
    // Synthetic data builders. XenogermPricing is pure (it only reads
    // GeneDef.biostatMet / biostatCpx / biostatArc plus
    // XenogermTraderStockMod.Settings) and the GeneExtension gate only walks
    // GeneDef.modExtensions, so this only needs to populate those fields.
    internal static class TestHelpers
    {
        // Calling `new GeneDef()` runs Def's instance ctor, which assigns
        // debugRandomId via Verse.Rand.RangeInclusive — safe on its own, but
        // there's no reason to depend on Rand's static state being valid
        // outside a live game. GetUninitializedObject allocates without
        // running any constructor, matching the pattern UWU's TestHelpers
        // uses for ThingDef.
        //
        // exclude and implantedMetabolism: both null leaves modExtensions null
        // (the common case for a gene no mod has patched); either one set
        // attaches a GeneExtension carrying them.
        //
        // passOnDirectly: GeneDef's field initializer defaults it to true, but
        // GetUninitializedObject skips initializers, so it is set explicitly here.
        public static GeneDef MakeGene(int metabolism = 0, int complexity = 0, int archites = 0,
            string defName = null, bool? exclude = null, bool passOnDirectly = true,
            int? implantedMetabolism = null)
        {
            var gene = (GeneDef)FormatterServices.GetUninitializedObject(typeof(GeneDef));
            gene.defName = defName ?? "TestGene";
            gene.passOnDirectly = passOnDirectly;
            gene.biostatMet = metabolism;
            gene.biostatCpx = complexity;
            gene.biostatArc = archites;
            if (exclude.HasValue || implantedMetabolism.HasValue)
            {
                gene.modExtensions = new List<DefModExtension>
                {
                    new GeneExtension
                    {
                        excludeFromXenogermStock = exclude ?? false,
                        implantedMetabolism = implantedMetabolism,
                    },
                };
            }
            return gene;
        }

        // Installs a fresh, default-valued settings instance. The setter is
        // internal (XenogermTraderStockMod.Settings), exposed to this test
        // assembly via InternalsVisibleTo on the main project.
        public static XenogermTraderStockSettings InstallDefaultSettings()
        {
            var settings = new XenogermTraderStockSettings();
            XenogermTraderStockMod.Settings = settings;
            return settings;
        }
    }
}
