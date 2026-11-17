using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace XenogermTraderStock.Patches
{
    // Makes vanilla's implant confirmation describe a germline rewrite.
    //
    // Xenogerm.SetTargetPawn builds the text as paragraphs: the coma warning,
    // "will overwrite existing xenogenes" when the pawn has any, the resulting
    // metabolism, then the question. For a rewrite the postfix adds a paragraph
    // after the coma warning saying the germline is replaced and which germline
    // genes go, and drops the xenogene paragraph when the rewrite keeps them (it
    // stays for the Baseliner germ, which does wipe them).
    //
    // The dialog is the message box SetTargetPawn just opened. The text is only
    // touched if it still opens with vanilla's coma paragraph, so a dialog
    // another mod has reworded is left alone.
    //
    // Confirming marks the span of the vanilla method, so
    // GeneUtility_MetabolismAfterImplanting_Patch reports the true metabolism
    // in the confirmation and not the number it raises to pass the order gates.
    // The finalizer clears it even if vanilla throws.
    [HarmonyPatch(typeof(Xenogerm), nameof(Xenogerm.SetTargetPawn))]
    public static class Xenogerm_SetTargetPawn_Patch
    {
        private const string ParagraphBreak = "\n\n";

        public static bool Confirming { get; private set; }

        public static void Prefix()
        {
            Confirming = true;
        }

        public static void Finalizer()
        {
            Confirming = false;
        }

        public static void Postfix(Xenogerm __instance, Pawn newTarget)
        {
            if (newTarget?.genes == null || __instance.GeneSet == null)
            {
                return;
            }

            XenogermSource source = XenogermIdentity.Resolve(__instance);
            bool keepsXenogenes = GeneUtility_ImplantXenogermItem_Patch.WillRetargetGermline(newTarget, source);
            if (!keepsXenogenes && !GeneUtility_ImplantXenogermItem_Patch.IsBaselinerConversion(newTarget, source))
            {
                return;
            }

            Dialog_MessageBox dialog = Find.WindowStack.Windows.OfType<Dialog_MessageBox>().LastOrDefault();
            if (dialog == null)
            {
                return;
            }

            // The same keys and arguments vanilla used, to find its paragraphs.
            int comaTicks = HediffDefOf.XenogerminationComa
                .CompProps<HediffCompProperties_Disappears>().disappearsAfterTicks.TrueMax;
            TaggedString coma = "ImplantXenogermWarningDesc".Translate(
                newTarget.Named("PAWN"), comaTicks.ToStringTicksToPeriod().Named("COMADURATION"));
            string overwrite = null;
            if (keepsXenogenes && newTarget.genes.Xenogenes.Any())
            {
                overwrite = "ImplantXenogermWarningOverwriteXenogenes".Translate(
                    newTarget.Named("PAWN"),
                    newTarget.genes.XenotypeLabelCap.Named("XENOTYPE"),
                    newTarget.genes.Xenogenes.Select(g => g.LabelCap).ToLineList("  - ").Named("XENOGENES")).RawText;
            }

            dialog.text = new TaggedString(RewriteConfirmation(
                dialog.text.RawText,
                coma.RawText,
                overwrite,
                GermlineParagraph(newTarget, __instance.GeneSet.GenesListForReading, keepsXenogenes).RawText));
        }

        private static TaggedString GermlineParagraph(Pawn pawn, List<GeneDef> germGenes, bool keepsXenogenes)
        {
            TaggedString text = (keepsXenogenes ? "XTS_ImplantWarningGermline" : "XTS_ImplantWarningBaseliner")
                .Translate(pawn.Named("PAWN"));
            List<GeneDef> lost = LostGermlineGenes(pawn.genes.Endogenes.Select(g => g.def), germGenes);
            if (lost.Count > 0)
            {
                // Translate leaves a string's first sentence uncapitalized, and
                // this one opens with the pawn's pronoun.
                text += " " + "XTS_ImplantWarningGermlineLost".Translate(
                    pawn.Named("PAWN"),
                    lost.Select(g => g.LabelCap.RawText).ToLineList("  - ").Named("GENES")).CapitalizeFirst();
            }

            return text;
        }

        // The germline genes a rewrite takes away: every endogene the germ does not
        // bring back. Skin and hair colour are left out, since the pawn keeps its
        // own unless the new germline supplies them (GeneLayerRebuild).
        public static List<GeneDef> LostGermlineGenes(IEnumerable<GeneDef> endogenes, List<GeneDef> germGenes)
        {
            return endogenes
                .Where(g => g.endogeneCategory != EndogeneCategory.Melanin
                    && g.endogeneCategory != EndogeneCategory.HairColor
                    && !germGenes.Contains(g))
                .ToList();
        }

        // Pure over raw text: inserts the germline paragraph after the coma
        // paragraph and removes the xenogene paragraph when one is given. Text
        // that does not open with the coma paragraph is returned unchanged.
        public static string RewriteConfirmation(string text, string coma, string overwrite, string germline)
        {
            if (text == null || !text.StartsWith(coma, System.StringComparison.Ordinal))
            {
                return text;
            }

            string rest = text.Substring(coma.Length);
            if (overwrite != null)
            {
                string paragraph = ParagraphBreak + overwrite;
                if (rest.StartsWith(paragraph, System.StringComparison.Ordinal))
                {
                    rest = rest.Substring(paragraph.Length);
                }
            }

            return coma + ParagraphBreak + germline + rest;
        }
    }
}
