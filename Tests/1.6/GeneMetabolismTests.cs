using System.Collections.Generic;
using Verse;
using Xunit;

namespace XenogermTraderStock.Tests
{
    // GeneExtension.implantedMetabolism: a placeholder gene that replaces
    // itself on add is counted at what it turns into, in the price and in the
    // implant prediction's correction.
    [Collection("XenogermPricing")]
    public class GeneMetabolismTests
    {
        public GeneMetabolismTests()
        {
            TestHelpers.InstallDefaultSettings();
        }

        [Fact]
        public void MetabolismOf_UnpatchedGene_IsItsOwnCost()
        {
            Assert.Equal(-2, GeneExtension.MetabolismOf(TestHelpers.MakeGene(metabolism: -2)));
        }

        [Fact]
        public void MetabolismOf_DeclaredCost_ReplacesTheDefs()
        {
            GeneDef placeholder = TestHelpers.MakeGene(metabolism: 0, implantedMetabolism: -3);

            Assert.Equal(-3, GeneExtension.MetabolismOf(placeholder));
        }

        [Fact]
        public void MetabolismOf_ExtensionWithoutACost_IsItsOwnCost()
        {
            GeneDef optedOut = TestHelpers.MakeGene(metabolism: 1, exclude: true);

            Assert.Equal(1, GeneExtension.MetabolismOf(optedOut));
        }

        [Fact]
        public void MetabolismOf_DeclaredZero_IsZero()
        {
            GeneDef placeholder = TestHelpers.MakeGene(metabolism: -4, implantedMetabolism: 0);

            Assert.Equal(0, GeneExtension.MetabolismOf(placeholder));
        }

        [Fact]
        public void MetabolismCorrection_NoPlaceholders_IsZero()
        {
            var genes = new List<GeneDef>
            {
                TestHelpers.MakeGene(metabolism: -2),
                TestHelpers.MakeGene(metabolism: 3, exclude: false),
            };

            Assert.Equal(0, GeneExtension.MetabolismCorrection(genes));
        }

        [Fact]
        public void MetabolismCorrection_SumsEachPlaceholdersDifference()
        {
            var genes = new List<GeneDef>
            {
                TestHelpers.MakeGene(metabolism: -2),
                TestHelpers.MakeGene(metabolism: 0, implantedMetabolism: -3),
                TestHelpers.MakeGene(metabolism: 0, implantedMetabolism: -1),
            };

            Assert.Equal(-4, GeneExtension.MetabolismCorrection(genes));
        }

        [Fact]
        public void Calculate_PricesAPlaceholderAtItsDeclaredCost()
        {
            var genes = new List<GeneDef>
            {
                TestHelpers.MakeGene(metabolism: 2),
                TestHelpers.MakeGene(metabolism: 0, implantedMetabolism: -3),
            };

            // |2| + |-3|
            Assert.Equal(5, XenogermPricing.Calculate(genes).AbsoluteMetabolism);
        }
    }
}
