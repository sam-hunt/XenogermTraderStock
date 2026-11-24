using System.Collections.Generic;
using Verse;

namespace XenogermTraderStock
{
    // What this mod needs to know about a gene that its def does not say.
    //
    // excludeFromXenogermStock: opt-out marker for genes that must never travel in a trader-sold
    // xenogerm. Any xenotype containing such a gene — preset or player-scenario
    // — is dropped at the candidate stage (XenotypeEligibility.IsCandidate):
    // hidden from the settings grid and never generated as stock.
    //
    // Gene-level rather than xenotype-level on purpose: custom xenotypes have
    // no Def to patch, and a xenotype built from android hardware is an android
    // whatever it is called. Vanilla has no "organic humanlike" concept to lean
    // on instead — canGenerateInGeneSet=false sits on Core's hair-colour
    // endogenes (would drop Impid/Waster/Highmate) and selectionWeight=0 on
    // Hemogenic (would drop Sanguophage) — so the marker has to be explicit.
    //
    // Shipped for Vanilla Races Expanded - Android in Patches_VREAndroid.xml.
    // Other mods, or a player's local patch, can flag their own synthetic /
    // animal / insect genelines the same way:
    //
    //   <modExtensions>
    //     <li Class="XenogermTraderStock.GeneExtension">
    //       <excludeFromXenogermStock>true</excludeFromXenogermStock>
    //     </li>
    //   </modExtensions>
    //
    // implantedMetabolism: what the gene costs once it is on a pawn, for a
    // placeholder that replaces itself with other genes when added. The def's
    // own biostatMet is then not what the pawn ends up paying, so pricing and
    // the implant prediction read MetabolismOf, never biostatMet.
    // Patches_VRERandomGenes.xml is the shipped use.
    public class GeneExtension : DefModExtension
    {
        public bool excludeFromXenogermStock;

        public int? implantedMetabolism;

        public static int MetabolismOf(GeneDef gene)
        {
            return gene.GetModExtension<GeneExtension>()?.implantedMetabolism ?? gene.biostatMet;
        }

        // What to add to a sum of biostatMet over these genes to get what
        // they will really cost.
        public static int MetabolismCorrection(IEnumerable<GeneDef> genes)
        {
            int correction = 0;
            foreach (GeneDef gene in genes)
            {
                correction += MetabolismOf(gene) - gene.biostatMet;
            }
            return correction;
        }
    }
}
