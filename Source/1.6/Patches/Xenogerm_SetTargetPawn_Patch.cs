using HarmonyLib;
using RimWorld;

namespace XenogermTraderStock.Patches
{
    // Marks the span in which vanilla builds the implant confirmation, so
    // GeneUtility_MetabolismAfterImplanting_Patch reports the true metabolism
    // there and not the number it raises to pass the order gates. The finalizer
    // clears the mark even if vanilla throws.
    [HarmonyPatch(typeof(Xenogerm), nameof(Xenogerm.SetTargetPawn))]
    public static class Xenogerm_SetTargetPawn_Patch
    {
        public static bool Confirming { get; private set; }

        public static void Prefix()
        {
            Confirming = true;
        }

        public static void Finalizer()
        {
            Confirming = false;
        }
    }
}
