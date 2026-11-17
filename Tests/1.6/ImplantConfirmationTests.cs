using System.Collections.Generic;
using RimWorld;
using Verse;
using XenogermTraderStock.Patches;
using Xunit;

namespace XenogermTraderStock.Tests
{
    // Xenogerm_SetTargetPawn_Patch's pure halves: the text rewrite and the list of
    // germline genes a rewrite takes away.
    public class ImplantConfirmationTests
    {
        private const string Coma = "Coma.";
        private const string Overwrite = "Overwrites xenogenes.";
        private const string Tail = "\n\nMetabolism.\n\nContinue?";

        [Fact]
        public void KeptXenogenes_ReplacesTheOverwriteParagraph()
        {
            string result = Xenogerm_SetTargetPawn_Patch.RewriteConfirmation(
                Coma + "\n\n" + Overwrite + Tail, Coma, Overwrite, "Germline.");

            Assert.Equal(Coma + "\n\nGermline." + Tail, result);
        }

        [Fact]
        public void NoOverwriteGiven_KeepsVanillaParagraphAfterOurs()
        {
            string result = Xenogerm_SetTargetPawn_Patch.RewriteConfirmation(
                Coma + "\n\n" + Overwrite + Tail, Coma, null, "Germline.");

            Assert.Equal(Coma + "\n\nGermline.\n\n" + Overwrite + Tail, result);
        }

        [Fact]
        public void PawnWithoutXenogenes_InsertsAfterTheComaParagraph()
        {
            string result = Xenogerm_SetTargetPawn_Patch.RewriteConfirmation(Coma + Tail, Coma, null, "Germline.");

            Assert.Equal(Coma + "\n\nGermline." + Tail, result);
        }

        [Fact]
        public void TextNotOpeningWithTheComaParagraph_IsUnchanged()
        {
            const string other = "Some other dialog.";

            Assert.Equal(other, Xenogerm_SetTargetPawn_Patch.RewriteConfirmation(other, Coma, Overwrite, "Germline."));
            Assert.Null(Xenogerm_SetTargetPawn_Patch.RewriteConfirmation(null, Coma, Overwrite, "Germline."));
        }

        [Fact]
        public void LostGenes_AreTheEndogenesTheGermDoesNotCarry()
        {
            GeneDef kept = TestHelpers.MakeGene(defName: "Kept");
            GeneDef lost = TestHelpers.MakeGene(defName: "Lost");

            List<GeneDef> result = Xenogerm_SetTargetPawn_Patch.LostGermlineGenes(
                new[] { kept, lost }, new List<GeneDef> { kept });

            Assert.Equal(new[] { lost }, result);
        }

        [Fact]
        public void LostGenes_LeaveOutSkinAndHairColour()
        {
            GeneDef skin = TestHelpers.MakeGene(defName: "Skin");
            skin.endogeneCategory = EndogeneCategory.Melanin;
            GeneDef hair = TestHelpers.MakeGene(defName: "Hair");
            hair.endogeneCategory = EndogeneCategory.HairColor;
            GeneDef jaw = TestHelpers.MakeGene(defName: "Jaw");
            jaw.endogeneCategory = EndogeneCategory.Jaw;

            List<GeneDef> result = Xenogerm_SetTargetPawn_Patch.LostGermlineGenes(
                new[] { skin, hair, jaw }, new List<GeneDef>());

            Assert.Equal(new[] { jaw }, result);
        }
    }
}
