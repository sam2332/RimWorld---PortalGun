using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Linq;

namespace PortalGun
{
    [StaticConstructorOnStartup]
    public static class PortalDebugActions
    {
        // Simple debug methods that can be called manually
        public static void ForceCreatePortal()
        {
            Pawn selectedPawn = Find.Selector.SelectedPawns.FirstOrDefault();
            if (selectedPawn == null)
            {
                Messages.Message("No pawn selected", MessageTypeDefOf.RejectInput);
                return;
            }

            // For now, create a portal 10 cells away for testing
            IntVec3 entryPos = selectedPawn.Position;
            IntVec3 exitPos = entryPos + new IntVec3(10, 0, 0);
            exitPos = PortalSpawner.FindBestExitPosition(exitPos, selectedPawn.Map);
            
            if (PortalSpawner.TrySpawnPortalPair(selectedPawn, entryPos, exitPos, new LocalTargetInfo(exitPos)))
            {
                Messages.Message($"Portal created from {entryPos} to {exitPos}", MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("Failed to create portal", MessageTypeDefOf.RejectInput);
            }
        }

        public static void ClearAllPortals()
        {
            Map map = Find.CurrentMap;
            var portals = map.listerThings.ThingsOfDef(PortalGunDefOf.PortalEntry)
                .Concat(map.listerThings.ThingsOfDef(PortalGunDefOf.PortalExit))
                .OfType<Portal>()
                .ToList();

            int count = portals.Count;
            foreach (Portal portal in portals)
            {
                portal.DestroyPortalPair();
            }

            Messages.Message($"Cleared {count} portals", MessageTypeDefOf.NeutralEvent);
        }
    }
}
