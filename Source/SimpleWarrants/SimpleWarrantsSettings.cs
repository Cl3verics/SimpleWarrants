using UnityEngine;
using Verse;

namespace SimpleWarrants
{
    public class SimpleWarrantsSettings : ModSettings
    {
        public static float chanceOfWarrantsMadeOnColonist = 0.05f;
        public static bool enableWarrantRewardScaling = true;
        public static bool enableWarrantsOnAnimals = true;
        public static bool enableWarrantsOnArtifact = true;
        public static bool enableWarrantsOnColonists = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref chanceOfWarrantsMadeOnColonist, nameof(chanceOfWarrantsMadeOnColonist), 0.05f);
            Scribe_Values.Look(ref enableWarrantsOnColonists, nameof(enableWarrantsOnColonists), true);
            Scribe_Values.Look(ref enableWarrantsOnArtifact, nameof(enableWarrantsOnArtifact), true);
            Scribe_Values.Look(ref enableWarrantsOnAnimals, nameof(enableWarrantsOnAnimals), true);
            Scribe_Values.Look(ref enableWarrantRewardScaling, nameof(enableWarrantRewardScaling), true);
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Rect rect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(rect);
            listingStandard.Label("SW.ChanceOfWarrantsMadeOnColonist".Translate((chanceOfWarrantsMadeOnColonist * 100f).ToStringDecimalIfSmall() + "%"));
            chanceOfWarrantsMadeOnColonist = listingStandard.Slider(chanceOfWarrantsMadeOnColonist, 0, 1f);
            listingStandard.CheckboxLabeled("SW.EnableWarrantsOnColonists".Translate(), ref enableWarrantsOnColonists);
            listingStandard.CheckboxLabeled("SW.EnableWarrantsOnArtifact".Translate(), ref enableWarrantsOnArtifact);
            listingStandard.CheckboxLabeled("SW.EnableWarrantsOnAnimals".Translate(), ref enableWarrantsOnAnimals);
            listingStandard.CheckboxLabeled("SW.EnableWarrantRewardScaling".Translate(), ref enableWarrantRewardScaling);
            listingStandard.End();
        }
    }
}
