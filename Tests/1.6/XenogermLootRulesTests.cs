using RimWorld;
using Xunit;

namespace XenogermTraderStock.Tests
{
    // The loot set maker's pure decisions: which quest givers may offer a
    // xenogerm, which price band a germ must fit, which pool entries survive
    // that band, and how the commonality settings become a weight factor.
    public class XenogermLootRulesTests
    {
        [Theory]
        [InlineData(TechLevel.Neolithic, false)]
        [InlineData(TechLevel.Medieval, false)]
        [InlineData(TechLevel.Industrial, true)]
        [InlineData(TechLevel.Ultra, true)]
        public void GiverAllowed_FactionMustReachTheFloor(TechLevel giver, bool expected)
        {
            Assert.Equal(expected, XenogermLootRules.GiverAllowed(giver, TechLevel.Industrial, allowFactionless: false));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GiverAllowed_NoFactionFollowsTheToggle(bool allowFactionless)
        {
            Assert.Equal(allowFactionless, XenogermLootRules.GiverAllowed(null, TechLevel.Ultra, allowFactionless));
        }

        [Fact]
        public void GiverAllowed_ToggleDoesNotAdmitALowTechFaction()
        {
            Assert.False(XenogermLootRules.GiverAllowed(TechLevel.Neolithic, TechLevel.Industrial, allowFactionless: true));
        }

        [Fact]
        public void PriceBand_BudgetSetsCeilingAndCheapItemFloor()
        {
            XenogermLootRules.PriceBand(2000f, 0.15f, null, null, out float min, out float max);

            Assert.Equal(300f, min);
            Assert.Equal(2000f, max);
        }

        [Fact]
        public void PriceBand_NoBudgetIsBoundedOnlyByTheCap()
        {
            XenogermLootRules.PriceBand(null, 0.15f, null, 2500f, out float min, out float max);

            Assert.Equal(0f, min);
            Assert.Equal(2500f, max);
        }

        [Fact]
        public void PriceBand_TightestCeilingWins()
        {
            XenogermLootRules.PriceBand(5000f, null, 1200f, 2500f, out float min, out float max);

            Assert.Equal(0f, min);
            Assert.Equal(1200f, max);
        }

        [Fact]
        public void PriceBand_UnboundedWithoutAnyLimit()
        {
            XenogermLootRules.PriceBand(null, null, null, null, out float min, out float max);

            Assert.Equal(0f, min);
            Assert.Equal(float.MaxValue, max);
        }

        [Fact]
        public void WeightsWithin_ZeroesPricesOutsideTheBand()
        {
            float[] prices = { 100f, 900f, 1500f, 4000f };

            float[] weights = XenogermCommonality.WeightsWithin(prices, XenogermSelectionStrategy.Uniform, 300f, 2000f);

            Assert.Equal(new[] { 0f, 1f, 1f, 0f }, weights);
        }

        [Fact]
        public void WeightsWithin_BandEdgesAreInclusive()
        {
            float[] prices = { 300f, 2000f };

            float[] weights = XenogermCommonality.WeightsWithin(prices, XenogermSelectionStrategy.Uniform, 300f, 2000f);

            Assert.Equal(new[] { 1f, 1f }, weights);
        }

        [Fact]
        public void WeightsWithin_SurvivorsKeepTheirWholePoolWeight()
        {
            float[] prices = { 400f, 900f, 1600f, 6000f };
            float[] whole = XenogermCommonality.Weights(prices, XenogermSelectionStrategy.BellCurve);

            float[] weights = XenogermCommonality.WeightsWithin(prices, XenogermSelectionStrategy.BellCurve, 500f, 2000f);

            Assert.Equal(0f, weights[0]);
            Assert.Equal(whole[1], weights[1]);
            Assert.Equal(whole[2], weights[2]);
            Assert.Equal(0f, weights[3]);
        }

        [Fact]
        public void WeightsWithin_NothingFitsLeavesNoPositiveWeight()
        {
            float[] prices = { 900f, 1500f };

            float[] weights = XenogermCommonality.WeightsWithin(prices, XenogermSelectionStrategy.SoftInversePrice, 0f, 500f);

            Assert.All(weights, w => Assert.Equal(0f, w));
        }

        [Fact]
        public void LootCommonalityFactor_ShipsOffForBothChannels()
        {
            var settings = new XenogermTraderStockSettings();

            Assert.Equal(0f, settings.LootCommonalityFactor(XenogermLootChannel.QuestReward));
            Assert.Equal(0f, settings.LootCommonalityFactor(XenogermLootChannel.WorldLoot));
        }

        [Fact]
        public void LootCommonalityFactor_ReadsEachChannelsOwnPercentage()
        {
            var settings = new XenogermTraderStockSettings
            {
                questRewardCommonality = 100f,
                worldLootCommonality = 250f,
            };

            Assert.Equal(1f, settings.LootCommonalityFactor(XenogermLootChannel.QuestReward));
            Assert.Equal(2.5f, settings.LootCommonalityFactor(XenogermLootChannel.WorldLoot));
        }
    }
}
