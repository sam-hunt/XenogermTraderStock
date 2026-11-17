using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Xunit;

namespace XenogermTraderStock.Tests
{
    // GeneLayerRebuild.Predict: which layer each gene lands on. Apply and the
    // metabolism sum need a live game.
    public class GeneLayerRebuildTests
    {
        [Fact]
        public void GermGenes_LandOnTheGermline()
        {
            GeneDef a = TestHelpers.MakeGene(defName: "A");
            GeneDef b = TestHelpers.MakeGene(defName: "B");

            List<GeneDefWithType> layers = GeneLayerRebuild.Predict(new List<GeneDef> { a, b }, null);

            Assert.Equal(new[] { a, b }, layers.Select(l => l.geneDef));
            Assert.All(layers, l => Assert.False(l.isXenogene));
        }

        [Fact]
        public void KeptXenogenes_StayXenogenes_AfterTheGermline()
        {
            GeneDef germ = TestHelpers.MakeGene(defName: "Germ");
            GeneDef kept = TestHelpers.MakeGene(defName: "Kept");

            List<GeneDefWithType> layers = GeneLayerRebuild.Predict(
                new List<GeneDef> { germ }, new List<GeneDef> { kept });

            Assert.Equal(2, layers.Count);
            Assert.Same(germ, layers[0].geneDef);
            Assert.False(layers[0].isXenogene);
            Assert.Same(kept, layers[1].geneDef);
            Assert.True(layers[1].isXenogene);
        }

        // Vanilla allows a gene on both layers; the xenogene copy overrides.
        [Fact]
        public void SameGeneOnBothLayers_IsListedTwice()
        {
            GeneDef shared = TestHelpers.MakeGene(defName: "Shared");

            List<GeneDefWithType> layers = GeneLayerRebuild.Predict(
                new List<GeneDef> { shared }, new List<GeneDef> { shared });

            Assert.Equal(new[] { false, true }, layers.Select(l => l.isXenogene));
        }

        // The Baseliner conversion: an empty germ and nothing kept.
        [Fact]
        public void EmptyGermAndNoXenogenes_PredictsNoGenes()
        {
            Assert.Empty(GeneLayerRebuild.Predict(new List<GeneDef>(), null));
            Assert.Empty(GeneLayerRebuild.Predict(null, null));
        }
    }
}
