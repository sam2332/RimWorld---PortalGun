using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace PortalGun
{
    [StaticConstructorOnStartup]
    public static class PortalGunInitializer
    {
        static PortalGunInitializer()
        {
            var harmony = new Harmony("portalgun.smartteleporter");
            harmony.PatchAll();
            Log.Message("[Portal Gun] Mod loaded successfully");
        }
    }

    public class PortalGunSettings : ModSettings
    {
        public static int minimumPathCostThreshold = 80;
        public static bool enablePortalVisualization = true;
        public static bool enableDebugMode = true;
        public static float portalDuration = 5f; // seconds

        public override void ExposeData()
        {
            Scribe_Values.Look(ref minimumPathCostThreshold, "minimumPathCostThreshold", 300);
            Scribe_Values.Look(ref enablePortalVisualization, "enablePortalVisualization", true);
            Scribe_Values.Look(ref enableDebugMode, "enableDebugMode", false);
            Scribe_Values.Look(ref portalDuration, "portalDuration", 5f);
            base.ExposeData();
        }
    }

    public class PortalGunMod : Mod
    {
        private PortalGunSettings settings;

        public PortalGunMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<PortalGunSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.Label("Minimum path cost to trigger portal: " + PortalGunSettings.minimumPathCostThreshold);
            PortalGunSettings.minimumPathCostThreshold = (int)listingStandard.Slider(PortalGunSettings.minimumPathCostThreshold, 100, 1000);

            listingStandard.CheckboxLabeled("Enable portal visualization", ref PortalGunSettings.enablePortalVisualization);
            listingStandard.CheckboxLabeled("Enable debug mode", ref PortalGunSettings.enableDebugMode);

            listingStandard.Label("Portal duration (seconds): " + PortalGunSettings.portalDuration.ToString("F1"));
            PortalGunSettings.portalDuration = listingStandard.Slider(PortalGunSettings.portalDuration, 1f, 30f);

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Portal Gun";
        }
    }
}
