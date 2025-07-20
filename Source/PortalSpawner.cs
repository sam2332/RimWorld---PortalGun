using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace PortalGun
{
    public static class PortalSpawner
    {
        public static bool TrySpawnPortalPair(Pawn pawn, IntVec3 entryPos, IntVec3 exitPos, LocalTargetInfo originalDestination)
        {
            // For the entry position, allow the pawn's current location since they'll teleport away
            bool entryValid = IsValidPortalLocation(entryPos, pawn.Map, pawn);
            bool exitValid = IsValidPortalLocation(exitPos, pawn.Map);
            
            if (!entryValid || !exitValid)
            {
                if (PortalGunSettings.enableDebugMode)
                {
                    Log.Warning($"[Portal Gun] Invalid portal locations: Entry={entryPos}, Exit={exitPos}");
                }
                return false;
            }

            // Create entry portal
            Portal entryPortal = (Portal)ThingMaker.MakeThing(PortalGunDefOf.PortalEntry);
            entryPortal.owner = pawn;
            entryPortal.isEntryPortal = true;
            entryPortal.originalDestination = originalDestination;

            // Create exit portal
            Portal exitPortal = (Portal)ThingMaker.MakeThing(PortalGunDefOf.PortalExit);
            exitPortal.owner = pawn;
            exitPortal.isEntryPortal = false;
            exitPortal.originalDestination = originalDestination;

            // Link them together
            entryPortal.linkedPortal = exitPortal;
            exitPortal.linkedPortal = entryPortal;

            // Spawn them
            GenSpawn.Spawn(entryPortal, entryPos, pawn.Map);
            GenSpawn.Spawn(exitPortal, exitPos, pawn.Map);

            // Visual effects for spawning
            if (PortalGunSettings.enablePortalVisualization)
            {
                FleckMaker.Static(entryPos, pawn.Map, FleckDefOf.PsycastSkipOuterRingExit);
                FleckMaker.Static(exitPos, pawn.Map, FleckDefOf.PsycastSkipOuterRingExit);
            }

            if (PortalGunSettings.enableDebugMode)
            {
                Log.Message($"[Portal Gun] Spawned portal pair for {pawn.LabelShort}: {entryPos} -> {exitPos}");
            }

            return true;
        }

        public static bool IsValidPortalLocation(IntVec3 cell, Map map, Pawn allowedPawn = null)
        {
            if (PortalGunSettings.enableDebugMode)
            {
                Log.Message($"[Portal Gun] Checking portal location validity: {cell}");
            }
            
            if (!cell.InBounds(map))
            {
                if (PortalGunSettings.enableDebugMode)
                {
                    Log.Message($"[Portal Gun] Location {cell} is out of bounds");
                }
                return false;
            }
            
            if (!cell.Walkable(map))
            {
                if (PortalGunSettings.enableDebugMode)
                {
                    Log.Message($"[Portal Gun] Location {cell} is not walkable");
                }
                return false;
            }

            // Check if cell is blocked by things
            List<Thing> things = cell.GetThingList(map);
            foreach (Thing thing in things)
            {
                if (thing.def.category == ThingCategory.Building && thing.def.passability == Traversability.Impassable)
                {
                    if (PortalGunSettings.enableDebugMode)
                    {
                        Log.Message($"[Portal Gun] Location {cell} blocked by impassable building: {thing.def.defName}");
                    }
                    return false;
                }
                if (thing is Pawn pawnAtLocation)
                {
                    // Allow the specified pawn (portal gun owner) to occupy their entry location
                    if (allowedPawn != null && pawnAtLocation == allowedPawn)
                    {
                        if (PortalGunSettings.enableDebugMode)
                        {
                            Log.Message($"[Portal Gun] Location {cell} occupied by allowed pawn: {pawnAtLocation.LabelShort}");
                        }
                        continue; // Skip this thing and continue checking
                    }
                    
                    if (PortalGunSettings.enableDebugMode)
                    {
                        Log.Message($"[Portal Gun] Location {cell} blocked by pawn: {pawnAtLocation.LabelShort}");
                    }
                    return false;
                }
            }

            if (PortalGunSettings.enableDebugMode)
            {
                Log.Message($"[Portal Gun] Location {cell} is valid");
            }
            return true;
        }

        public static IntVec3 FindBestExitPosition(IntVec3 targetPosition, Map map, int searchRadius = 5)
        {
            // Try the exact target position first
            if (IsValidPortalLocation(targetPosition, map))
                return targetPosition;

            // Search in expanding rings around the target
            for (int radius = 1; radius <= searchRadius; radius++)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(targetPosition, radius, true))
                {
                    if (IsValidPortalLocation(cell, map))
                        return cell;
                }
            }

            // Fallback - return target even if not ideal
            return targetPosition;
        }
    }
}
