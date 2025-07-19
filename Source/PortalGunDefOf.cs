using RimWorld;
using Verse;

namespace PortalGun
{
    [DefOf]
    public static class PortalGunDefOf
    {
        public static ThingDef PortalEntry;
        public static ThingDef PortalExit;
        public static ThingDef PortalGun_Device;

        static PortalGunDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(PortalGunDefOf));
        }
    }
}
