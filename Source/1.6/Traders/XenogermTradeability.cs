using RimWorld;
using Verse;

namespace XenogermTraderStock
{
    // Keeps the Xenogerm ThingDef sellable BY traders. Vanilla leaves it at
    // Tradeability.All, but GTG Trader Core patches it to Sellable (player
    // may sell, trader may not), and vanilla's ThingSetMaker_TraderStock
    // discards every generated item whose def fails TraderCanSell() with
    // "<trader> generated carrying Xenogerm... which can't be sold by
    // traders. Ignoring..." - so our generator ran, and the consumer threw
    // its output away. Restoring All is the whole fix: GTG's own tradeability
    // hijack only touches defs that are NOT trader-sellable, so it becomes a
    // no-op for xenogerms rather than a conflict.
    //
    // Runs at startup (so the log line sits with the boot output players
    // paste into bug reports) AND from every stock roll, because the value
    // lives on the def INSTANCE: a mid-session play-data reload re-parses and
    // re-patches defs, which brings the foreign value back on an instance no
    // static ctor will see again. Idempotent; logs only when it changes
    // something.
    public static class XenogermTradeability
    {
        public static void EnsureTraderCanSell()
        {
            ThingDef xenogerm = ThingDefOf.Xenogerm;
            if (xenogerm == null)
            {
                return;
            }

            Tradeability? restored = Normalise(xenogerm.tradeability);
            if (restored == null)
            {
                return;
            }

            // One short line in the collapsed log; the reason lives on a
            // second line only seen expanded or in the file. "Sellable" is
            // qualified because in a trade-goods context it reads as the
            // correct state, while vanilla's enum value means player-only.
            Log.Message($"[Xenogerm Trader Stock] Xenogerm tradeability restored from {Describe(xenogerm.tradeability)} to {restored.Value}.\n" +
                "Another mod's patch had made the Xenogerm def unsellable by traders, and vanilla trader stock " +
                "generation silently discards every item whose def traders cannot sell, so no xenogerm would have reached a trader.");
            xenogerm.tradeability = restored.Value;
        }

        private static string Describe(Tradeability value)
        {
            return value == Tradeability.Sellable ? "(player) Sellable" : value.ToString();
        }

        // The pure decision: the value to write when traders cannot sell the
        // current one, null when nothing needs to change. Player sellability
        // is preserved either way - All is the only value that satisfies both
        // TraderCanSell and PlayerCanSell.
        public static Tradeability? Normalise(Tradeability current)
        {
            return current.TraderCanSell() ? (Tradeability?)null : Tradeability.All;
        }
    }

    [StaticConstructorOnStartup]
    public static class XenogermTradeabilityStartup
    {
        static XenogermTradeabilityStartup()
        {
            XenogermTradeability.EnsureTraderCanSell();
        }
    }
}
