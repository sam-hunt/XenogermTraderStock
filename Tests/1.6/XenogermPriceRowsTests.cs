using Xunit;

namespace XenogermTraderStock.Tests
{
    // Unit coverage for XenogermPriceRows: the keys each row carries, the
    // placeholders handed to the translator, and the silver each row adds.
    // Label() is not exercised - it needs the game's language database.
    [Collection("XenogermPricing")]
    public class XenogermPriceRowsTests
    {
        [Fact]
        public void Base_IsVanillaBaseValueUnderTheBaseKey()
        {
            TestHelpers.InstallDefaultSettings();

            var row = XenogermPriceRows.Base;

            Assert.Equal("XTS_PriceBase", row.Key);
            Assert.Equal(XenogermPricing.BaseXenogermValue, row.Silver);
            Assert.Empty(row.Args);
        }

        [Fact]
        public void Preset_CarriesBasePresetValueFromSettings()
        {
            var settings = TestHelpers.InstallDefaultSettings();
            settings.basePresetValue = 750f;

            var row = XenogermPriceRows.Preset(settings);

            Assert.Equal("XTS_PricePreset", row.Key);
            Assert.Equal(750f, row.Silver);
            Assert.Empty(row.Args);
        }

        [Fact]
        public void Genes_OneRowPerStat_CountTimesPerPointSilver()
        {
            var settings = TestHelpers.InstallDefaultSettings();
            settings.valuePerMetabolism = 10f;
            settings.valuePerComplexity = 15f;
            settings.valuePerArchite = 100f;
            var breakdown = new XenogermPricing.PricingBreakdown
            {
                AbsoluteMetabolism = 4,
                Complexity = 3,
                Archites = 2,
            };

            var rows = XenogermPriceRows.Genes(breakdown, settings);

            Assert.Collection(rows,
                r =>
                {
                    Assert.Equal("XTS_PriceMetabolism", r.Key);
                    Assert.Equal(40f, r.Silver);
                    Assert.Equal(new object[] { 4, "10" }, r.Args);
                },
                r =>
                {
                    Assert.Equal("XTS_PriceComplexity", r.Key);
                    Assert.Equal(45f, r.Silver);
                    Assert.Equal(new object[] { 3, "15" }, r.Args);
                },
                r =>
                {
                    Assert.Equal("XTS_PriceArchite", r.Key);
                    Assert.Equal(200f, r.Silver);
                    Assert.Equal(new object[] { 2, "100" }, r.Args);
                });
        }

        [Fact]
        public void Genes_ZeroCounts_StillYieldAllThreeRowsAtZeroSilver()
        {
            var settings = TestHelpers.InstallDefaultSettings();

            var rows = XenogermPriceRows.Genes(new XenogermPricing.PricingBreakdown(), settings);

            Assert.Equal(3, rows.Count);
            Assert.All(rows, r => Assert.Equal(0f, r.Silver));
        }

        [Fact]
        public void Genes_PerPointPlaceholderIsWholeSilver()
        {
            var settings = TestHelpers.InstallDefaultSettings();
            settings.valuePerMetabolism = 12.6f;
            var breakdown = new XenogermPricing.PricingBreakdown { AbsoluteMetabolism = 1 };

            var row = XenogermPriceRows.Genes(breakdown, settings)[0];

            // The slider snaps to whole silver; the label shows it that way
            // while the silver column keeps the exact product.
            Assert.Equal("13", row.Args[1]);
            Assert.Equal(12.6f, row.Silver);
        }
    }
}
