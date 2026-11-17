using RimWorld;
using UnityEngine;
using Verse;

namespace XenogermTraderStock
{
    // "Rewards and loot" settings section: how often ThingSetMaker_Xenogerms
    // offers a xenogerm in the pools Patches_Loot.xml adds it to. Both channels
    // ship at 0%, which is off. A commonality is a percentage of the option's
    // XML weight, so the patches stay the single source of shipped rarity, and
    // it is read at generation time, so a change needs no restart.
    //
    // Each channel's other rows are children of its commonality: drawn
    // indented, and dimmed and inert while the commonality is 0%.
    public partial class XenogermTraderStockSettings
    {
        public const float DefaultQuestRewardCommonality = 0f;
        public float questRewardCommonality = DefaultQuestRewardCommonality;

        public const TechLevel DefaultQuestRewardMinTechLevel = TechLevel.Industrial;
        public TechLevel questRewardMinTechLevel = DefaultQuestRewardMinTechLevel;

        public const bool DefaultQuestRewardAllowFactionless = true;
        public bool questRewardAllowFactionless = DefaultQuestRewardAllowFactionless;

        public const float DefaultWorldLootCommonality = 0f;
        public float worldLootCommonality = DefaultWorldLootCommonality;

        public const float DefaultLootMaxValue = 2500f;
        public float lootMaxValue = DefaultLootMaxValue;

        public const float MaxLootCommonality = 500f;
        public const float StepLootCommonality = 25f;
        public const TechLevel LowestTechLevelFloor = TechLevel.Neolithic;
        public const TechLevel HighestTechLevelFloor = TechLevel.Ultra;
        public const float MinLootMaxValue = 500f;
        public const float MaxLootMaxValue = 10000f;
        public const float StepLootMaxValue = 250f;

        private const float ChildIndent = 12f;

        // The commonality as the factor the set maker scales its weight by.
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
            Scribe_Values.Look(ref questRewardMinTechLevel, "questRewardMinTechLevel", DefaultQuestRewardMinTechLevel);
            Scribe_Values.Look(ref questRewardAllowFactionless, "questRewardAllowFactionless", DefaultQuestRewardAllowFactionless);
            Scribe_Values.Look(ref worldLootCommonality, "worldLootCommonality", DefaultWorldLootCommonality);
            Scribe_Values.Look(ref lootMaxValue, "lootMaxValue", DefaultLootMaxValue);
        }

        private void ResetLootSettings()
        {
            questRewardCommonality = DefaultQuestRewardCommonality;
            questRewardMinTechLevel = DefaultQuestRewardMinTechLevel;
            questRewardAllowFactionless = DefaultQuestRewardAllowFactionless;
            worldLootCommonality = DefaultWorldLootCommonality;
            lootMaxValue = DefaultLootMaxValue;
        }

        private void DrawLootSection(Listing_Standard listing)
        {
            SectionHeader(listing, "XTS_LootSection".Translate(), "XTS_LootSectionDesc".Translate());

            questRewardCommonality = SliderRow(listing,
                "XTS_QuestRewardCommonality", "XTS_QuestRewardCommonalityDesc",
                questRewardCommonality, DefaultQuestRewardCommonality,
                0f, MaxLootCommonality, StepLootCommonality);

            float top = BeginChildRows(listing);
            bool enabled = questRewardCommonality > 0f;
            questRewardMinTechLevel = TechLevelRow(listing, questRewardMinTechLevel, enabled);
            ChildCheckbox(listing, "XTS_QuestRewardFactionless", "XTS_QuestRewardFactionlessDesc",
                ref questRewardAllowFactionless, enabled);
            EndChildRows(listing, top, enabled);

            // The loot pools belong to these two expansions; without either
            // there is nothing for the rows to control.
            if (ModsConfig.IdeologyActive || ModsConfig.OdysseyActive)
            {
                worldLootCommonality = SliderRow(listing,
                    "XTS_WorldLootCommonality", "XTS_WorldLootCommonalityDesc",
                    worldLootCommonality, DefaultWorldLootCommonality,
                    0f, MaxLootCommonality, StepLootCommonality);

                top = BeginChildRows(listing);
                enabled = worldLootCommonality > 0f;
                lootMaxValue = ChildSliderRow(listing,
                    LabelWithDefault("XTS_LootMaxValue".Translate(lootMaxValue.ToString("F0")),
                        Mathf.Approximately(lootMaxValue, DefaultLootMaxValue)),
                    "XTS_LootMaxValueDesc".Translate(),
                    lootMaxValue, MinLootMaxValue, MaxLootMaxValue, StepLootMaxValue, enabled);
                EndChildRows(listing, top, enabled);
            }

            listing.Gap(SectionGap);
        }

        private TechLevel TechLevelRow(Listing_Standard listing, TechLevel value, bool enabled)
        {
            string label = LabelWithDefault(
                "XTS_QuestRewardMinTech".Translate(value.ToStringHuman().CapitalizeFirst()),
                value == DefaultQuestRewardMinTechLevel);
            float edited = ChildSliderRow(listing, label, "XTS_QuestRewardMinTechDesc".Translate(),
                (float)value, (float)LowestTechLevelFloor, (float)HighestTechLevelFloor, 1f, enabled);
            return (TechLevel)Mathf.RoundToInt(edited);
        }

        private static string LabelWithDefault(string label, bool isDefault)
        {
            return isDefault ? label + "XTS_DefaultSuffix".Translate() : label;
        }

        // Only the left edge moves; the right stays flush with the parent row.
        private static float BeginChildRows(Listing_Standard listing)
        {
            listing.Indent(ChildIndent);
            listing.ColumnWidth -= ChildIndent;
            return listing.CurHeight;
        }

        // Dims disabled children with a wash of the window's own fill, since
        // vanilla's slider has no disabled look of its own.
        private static void EndChildRows(Listing_Standard listing, float top, bool enabled)
        {
            if (!enabled)
            {
                Rect rows = new Rect(ChildIndent, top, listing.ColumnWidth, listing.CurHeight - top);
                Color wash = Widgets.WindowBGFillColor;
                wash.a = 0.6f;
                Widgets.DrawBoxSolid(rows, wash);
            }
            listing.ColumnWidth += ChildIndent;
            listing.Outdent(ChildIndent);
        }

        // SliderRow for a child. A disabled slider is drawn on repaint only:
        // vanilla's slider starts a drag on mouse-down and has no off switch.
        private static float ChildSliderRow(Listing_Standard listing, string label, string tooltip,
            float value, float min, float max, float step, bool enabled)
        {
            listing.Label(label, tooltip: tooltip);
            if (!enabled)
            {
                Rect rect = listing.GetRect(22f);
                if (Event.current.type == EventType.Repaint)
                {
                    Widgets.HorizontalSlider(rect, value, min, max);
                }
                listing.Gap(listing.verticalSpacing);
                return value;
            }
            return Mathf.Round((listing.Slider(value, min, max) - min) / step) * step + min;
        }

        private static void ChildCheckbox(Listing_Standard listing, string labelKey, string descKey,
            ref bool value, bool enabled)
        {
            string label = labelKey.Translate();
            Rect rect = listing.GetRect(Text.CalcHeight(label, listing.ColumnWidth));
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }
            TooltipHandler.TipRegion(rect, descKey.Translate());
            Widgets.CheckboxLabeled(rect, label, ref value, disabled: !enabled);
            listing.Gap(listing.verticalSpacing);
        }
    }
}
