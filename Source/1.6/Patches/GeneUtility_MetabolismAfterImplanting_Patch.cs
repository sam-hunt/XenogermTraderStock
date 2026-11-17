using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace XenogermTraderStock.Patches
{
    // Corrects vanilla's predicted metabolism for an implant that rewrites the
    // germline.
    //
    // Vanilla sums the pawn's endogenes plus the germ's genes as xenogenes, which
    // is what its own implant leaves. A rewrite
    // (GeneUtility_ImplantXenogermItem_Patch) leaves the germ's genes as the
    // germline with the pawn's old xenogenes on top, or no genes at all for the
    // Baseliner germ. Uncorrected, a born member of another xenotype was refused
    // implants that fit, and a pawn with heavy xenogenes was allowed ones that
    // did not.
    //
    // The number gates the three ways to order an implant
    // (Xenogerm.GetFloatMenuOptions, Xenogerm.GetGizmos,
    // Dialog_SelectXenogerm.Accept) and is shown in Xenogerm.SetTargetPawn's
    // confirmation. Nothing checks again at surgery time.
    //
    // Vanilla passes the gene set, not the germ, and identity needs the germ's
    // comp, so the owner is looked up among the map's xenogerms. A gene set with
    // no owner there keeps vanilla's number.
    //
    // Dropped: inferring identity from the gene set alone. It ignores the comp,
    // so the prediction could name a different xenotype than the implant stamps.
    //
    // Not covered: the confirmation's "will overwrite xenogenes" paragraph still
    // lists xenogenes a rewrite keeps.
    [HarmonyPatch(typeof(GeneUtility), nameof(GeneUtility.MetabolismAfterImplanting))]
    public static class GeneUtility_MetabolismAfterImplanting_Patch
    {
        public static void Postfix(Pawn pawn, GeneSet geneSet, ref int __result)
        {
            if (pawn?.genes == null || geneSet == null)
            {
                return;
            }

            Xenogerm xenogerm = FindOwner(pawn.MapHeld, geneSet);
            if (xenogerm == null)
            {
                return;
            }

            XenogermSource source = XenogermIdentity.Resolve(xenogerm);
            bool keepsXenogenes = GeneUtility_ImplantXenogermItem_Patch.WillRetargetGermline(pawn, source);
            if (!keepsXenogenes && !GeneUtility_ImplantXenogermItem_Patch.IsBaselinerConversion(pawn, source))
            {
                return;
            }

            List<GeneDef> keptXenogenes = keepsXenogenes
                ? pawn.genes.Xenogenes.Select(g => g.def).ToList()
                : null;
            __result = GeneLayerRebuild.Predict(geneSet.GenesListForReading, keptXenogenes)
                .NonOverriddenGenes()
                .Sum(g => g.biostatMet);
        }

        private static Xenogerm FindOwner(Map map, GeneSet geneSet)
        {
            if (map == null)
            {
                return null;
            }

            foreach (Thing thing in map.listerThings.ThingsOfDef(ThingDefOf.Xenogerm))
            {
                if (thing is Xenogerm xenogerm && xenogerm.GeneSet == geneSet)
                {
                    return xenogerm;
                }
            }

            return null;
        }
    }
}
