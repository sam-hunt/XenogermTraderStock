using XenogermTraderStock.Patches;
using Xunit;

namespace XenogermTraderStock.Tests
{
    // GeneUtility_MetabolismAfterImplanting_Patch.ApplyLimitScope: the number the
    // order gates see under each setting.
    public class MetabolismLimitScopeTests
    {
        private const int Floor = -5;

        private static int Apply(int metabolism, MetabolismLimitScope scope, bool known)
        {
            return GeneUtility_MetabolismAfterImplanting_Patch.ApplyLimitScope(metabolism, Floor, scope, known);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AllXenogerms_NeverRaises(bool known)
        {
            Assert.Equal(-8, Apply(-8, MetabolismLimitScope.AllXenogerms, known));
        }

        [Fact]
        public void ExceptKnownXenotypes_RaisesKnownXenotypeToFloor()
        {
            Assert.Equal(Floor, Apply(-8, MetabolismLimitScope.ExceptKnownXenotypes, known: true));
        }

        [Fact]
        public void ExceptKnownXenotypes_LeavesUnknownGermRefused()
        {
            Assert.Equal(-8, Apply(-8, MetabolismLimitScope.ExceptKnownXenotypes, known: false));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NoXenogerms_RaisesEveryGermToFloor(bool known)
        {
            Assert.Equal(Floor, Apply(-8, MetabolismLimitScope.NoXenogerms, known));
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(0)]
        [InlineData(3)]
        public void WithinTheLimit_IsNeverChanged(int metabolism)
        {
            Assert.Equal(metabolism, Apply(metabolism, MetabolismLimitScope.NoXenogerms, known: true));
        }
    }
}
