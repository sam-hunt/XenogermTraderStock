using RimWorld;
using Verse;

namespace XenogermTraderStock
{
    // "Rewards and loot" settings section: how often ThingSetMaker_Xenogerms
    // offers a xenogerm in the pools Patches_Loot.xml adds it to. Both channels
    // ship at 0%, which is off. A commonality is a percentage of a genepack's
    // own chance in the same pool, so 100% carries the "same as genepacks"
    // mark, and it is read at generation time, so a change needs no restart.
    //
    // Which givers may offer a germ and how much a looted one may be worth are
    // fixed (XenogermLootRules); the section tooltip states both.
    public partial class XenogermTraderStockSettings
    {
        public const float DefaultQuestRewardCommonality = 0f;
        public float questRewardCommonality = DefaultQuestRewardCommonality;

        public const float DefaultWorldLootCommonality = 0f;
        public float worldLootCommonality = DefaultWorldLootCommonality;

        public const float GenepackParityCommonality = 100f;
        public const float MaxLootCommonality = 500f;
        public const float StepLootCommonality = 25f;

        // The commonality as the factor the set maker scales a genepack's
        // weight by.
        public float LootCommonalityFactor(XenogermLootChannel channel)
        {
            float percent = channel == XenogermLootChannel.QuestReward
                ? questRewardCommonality
                : worldLootCommonality;
            return percent / 100f;
        }

        private void ExposeLootSettings()
        {
            Scribe_Values.Look(ref questRewardCommonality, "questRewardCommonality", DefaultQuestRewardCommonality);
            Scribe_Values.Look(ref worldLootCommonality, "worldLootCommonality", DefaultWorldLootCommonality);
        }

        private void ResetLootSettings()
        {
            questRewardCommonality = DefaultQuestRewardCommonality;
            worldLootCommonality = DefaultWorldLootCommonality;
        }

        private void DrawLootSection(Listing_Standard listing)
        {
            SectionHeader(listing, "XTS_LootSection".Translate(), "XTS_LootSectionDesc".Translate(
                XenogermLootRules.MinGiverTechLevel.ToStringHuman(),
                XenogermLootRules.MaxUnbudgetedValue.ToString("F0")));

            questRewardCommonality = SliderRow(listing,
                "XTS_QuestRewardCommonality", "XTS_QuestRewardCommonalityDesc",
                questRewardCommonality, DefaultQuestRewardCommonality,
                0f, MaxLootCommonality, StepLootCommonality,
                GenepackParityCommonality, "XTS_SameAsGenepacks");

            // The loot pools belong to these two expansions; without either
            // there is nothing for the row to control.
            if (ModsConfig.IdeologyActive || ModsConfig.OdysseyActive)
            {
                worldLootCommonality = SliderRow(listing,
                    "XTS_WorldLootCommonality", "XTS_WorldLootCommonalityDesc",
                    worldLootCommonality, DefaultWorldLootCommonality,
                    0f, MaxLootCommonality, StepLootCommonality,
                    GenepackParityCommonality, "XTS_SameAsGenepacks");
            }

            listing.Gap(SectionGap);
        }
    }
}
