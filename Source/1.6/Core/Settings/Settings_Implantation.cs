using RimWorld;
using Verse;

namespace XenogermTraderStock
{
    // Which xenogerms vanilla's metabolism floor still refuses at order time.
    // Scribed by name, so renaming a member resets the setting.
    public enum MetabolismLimitScope
    {
        AllXenogerms,
        ExceptKnownXenotypes,
        NoXenogerms,
    }

    // "Implantation" settings section: how trader-sold xenogerms behave when
    // implanted (the GeneUtility.ImplantXenogermItem postfix reads it).
    public partial class XenogermTraderStockSettings
    {
        // Write a trader-sold germline (inheritable) xenogerm's genes into the
        // pawn's endogenes rather than as xenogenes, making a born member of
        // the xenotype (children inherit, later implants stack instead of
        // replacing). Free-standing on purpose: it acts at implant time on an
        // item the player already owns, so it must not depend on what the shop
        // grid currently has ticked.
        public const bool DefaultImplantGermlineAsEndogenes = true;
        public bool implantGermlineAsEndogenes = DefaultImplantGermlineAsEndogenes;

        // Restore a pawn's deathrest capacity (bought with ~1000-silver serums,
        // stored on the Gene_Deathrest instance) after an implant or sanguophage
        // reimplant recreates the gene - vanilla's Gene_Deathrest.PostAdd resets
        // capacity to 1 unconditionally. Applies to EVERY xenogerm implantation,
        // not just trader-sold items: the reset is a vanilla-wide annoyance and
        // gating it on our comp would leave the same pawn inconsistent between
        // a bought and a crafted xenogerm.
        public const bool DefaultPreserveDeathrestCapacity = true;
        public bool preserveDeathrestCapacity = DefaultPreserveDeathrestCapacity;

        // Vanilla refuses an implant that would end below its metabolism floor,
        // though preset xenotypes may ship below it and hunger stops growing there.
        // The middle option exempts germs XenogermIdentity resolves to a xenotype.
        public const MetabolismLimitScope DefaultMetabolismLimitScope = MetabolismLimitScope.AllXenogerms;
        public MetabolismLimitScope metabolismLimitScope = DefaultMetabolismLimitScope;

        private void ExposeImplantationSettings()
        {
            Scribe_Values.Look(ref implantGermlineAsEndogenes, "implantGermlineAsEndogenes", DefaultImplantGermlineAsEndogenes);
            Scribe_Values.Look(ref preserveDeathrestCapacity, "preserveDeathrestCapacity", DefaultPreserveDeathrestCapacity);
            Scribe_Values.Look(ref metabolismLimitScope, "metabolismLimitScope", DefaultMetabolismLimitScope);
        }

        private void ResetImplantationSettings()
        {
            implantGermlineAsEndogenes = DefaultImplantGermlineAsEndogenes;
            preserveDeathrestCapacity = DefaultPreserveDeathrestCapacity;
            metabolismLimitScope = DefaultMetabolismLimitScope;
        }

        private void DrawImplantationSection(Listing_Standard listing)
        {
            SectionHeader(listing, "XTS_ImplantationSection".Translate(), "XTS_ImplantationSectionDesc".Translate());

            listing.CheckboxLabeled(
                "XTS_ImplantGermlineAsEndogenes".Translate(),
                ref implantGermlineAsEndogenes,
                "XTS_ImplantGermlineAsEndogenesDesc".Translate());
            listing.Gap(SettingGap);

            listing.CheckboxLabeled(
                "XTS_PreserveDeathrestCapacity".Translate(),
                ref preserveDeathrestCapacity,
                "XTS_PreserveDeathrestCapacityDesc".Translate());
            listing.Gap(SettingGap);

            listing.Label(
                "XTS_MetabolismLimit".Translate(),
                tooltip: "XTS_MetabolismLimitDesc".Translate(GeneTuning.BiostatRange.TrueMin));
            LimitScopeRadio(listing, MetabolismLimitScope.AllXenogerms,
                "XTS_LimitAllXenogerms", "XTS_LimitAllXenogermsDesc");
            LimitScopeRadio(listing, MetabolismLimitScope.ExceptKnownXenotypes,
                "XTS_LimitExceptKnown", "XTS_LimitExceptKnownDesc");
            LimitScopeRadio(listing, MetabolismLimitScope.NoXenogerms,
                "XTS_LimitOff", "XTS_LimitOffDesc");

            listing.Gap(SectionGap);
        }

        // Same row style as the commonality section's StrategyRadio, indented
        // under the group's label.
        private void LimitScopeRadio(Listing_Standard listing, MetabolismLimitScope scope,
            string labelKey, string descKey)
        {
            const float indent = 12f;

            string label = labelKey.Translate();
            if (scope == DefaultMetabolismLimitScope)
            {
                label += "XTS_DefaultSuffix".Translate();
            }
            if (listing.RadioButton(label, metabolismLimitScope == scope, indent, descKey.Translate()))
            {
                metabolismLimitScope = scope;
            }
        }
    }
}
