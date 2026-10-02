using RimWorld;
using Xunit;

namespace XenogermTraderStock.Tests
{
    // XenogermTradeability.Normalise is the pure half of the GTG Trader Core
    // guard: given the Xenogerm def's current tradeability, the value to
    // write so traders can sell it, or null when nothing needs to change.
    // TradeabilityUtility is a plain enum extension, so no game state needed.
    public class XenogermTradeabilityTests
    {
        [Theory]
        [InlineData(Tradeability.All)]
        [InlineData(Tradeability.Buyable)]
        public void TraderSellable_IsLeftAlone(Tradeability current)
        {
            Assert.Null(XenogermTradeability.Normalise(current));
        }

        [Theory]
        [InlineData(Tradeability.Sellable)]
        [InlineData(Tradeability.None)]
        public void TraderUnsellable_RestoresAll(Tradeability current)
        {
            Assert.Equal(Tradeability.All, XenogermTradeability.Normalise(current));
        }

        [Fact]
        public void RestoredValue_KeepsPlayerSellability()
        {
            Tradeability restored = XenogermTradeability.Normalise(Tradeability.Sellable).Value;

            Assert.True(restored.PlayerCanSell());
            Assert.True(restored.TraderCanSell());
        }
    }
}
