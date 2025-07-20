using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System;

namespace PortalGun
{
    [HarmonyPatch(typeof(Pawn_PathFollower), "StartPath")]
    public static class Patch_PathFollower_StartPath
    {
        static bool Prefix(Pawn_PathFollower __instance, LocalTargetInfo dest, PathEndMode peMode)
        {
            // Use reflection to access the private pawn field
            Pawn pawn = (Pawn)typeof(Pawn_PathFollower).GetField("pawn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(__instance);
            

            
            // Only apply to colonists and player-controlled pawns
            if (pawn == null || !pawn.Spawned || 
                (!pawn.IsColonist && !pawn.IsPlayerControlled) ||
                pawn.Dead || pawn.Downed)
            {
                return true;
            }

            // Don't interfere if pawn is already teleporting
            if (pawn.teleporting)
            {
                return true;
            }

            // Check if pawn has a portal gun device
            if (!HasPortalGunDevice(pawn))
            {
                return true;
            }

            // Calculate path cost
            int pathCost = CalculatePathCost(pawn.Position, dest.Cell, pawn.Map, pawn);
            
            if (PortalGunSettings.enableDebugMode)
            {
                Log.Message($"[Portal Gun] {pawn.LabelShort} path cost: {pathCost} (threshold: {PortalGunSettings.minimumPathCostThreshold})");
            }

            // Check if we should create a portal
            if (pathCost > PortalGunSettings.minimumPathCostThreshold)
            {
                if (TryCreatePortalPath(pawn, dest, peMode))
                {
                    // Portal path created, skip original pathfinding
                    return false;
                }
            }

            // Continue with normal pathfinding
            return true;
        }

        private static bool TryCreatePortalPath(Pawn pawn, LocalTargetInfo dest, PathEndMode peMode)
        {
            try
            {
                IntVec3 entryPos = pawn.Position;
                IntVec3 exitPos = PortalSpawner.FindBestExitPosition(dest.Cell, pawn.Map);

                if (PortalSpawner.TrySpawnPortalPair(pawn, entryPos, exitPos, dest))
                {
                    // Since the pawn is already at the entry position, they'll automatically walk into it
                    // The portal will handle the teleportation and job continuation
                    
                    if (PortalGunSettings.enableDebugMode)
                    {
                        Log.Message($"[Portal Gun] Created portal path for {pawn.LabelShort}: {entryPos} -> {exitPos}");
                    }

                    return true;
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Portal Gun] Error creating portal path: {e}");
            }

            return false;
        }

        private static bool HasPortalGunDevice(Pawn pawn)
        {
            if (PortalGunSettings.enableDebugMode)
            {
                Log.Message($"[Portal Gun] Checking if {pawn.LabelShort} has portal gun device...");
            }
            
            // Check if pawn has portal gun device equipped
            if (pawn.apparel?.WornApparel != null)
            {
                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    if (PortalGunSettings.enableDebugMode)
                    {
                        Log.Message($"[Portal Gun] Checking apparel: {apparel.def.defName}");
                    }
                    if (apparel.def == PortalGunDefOf.PortalGun_Device)
                    {
                        if (PortalGunSettings.enableDebugMode)
                        {
                            Log.Message($"[Portal Gun] Found portal gun device in apparel!");
                        }
                        return true;
                    }
                }
            }

            // Check if pawn has portal gun device in inventory
            if (pawn.inventory?.innerContainer != null)
            {
                foreach (var item in pawn.inventory.innerContainer)
                {
                    if (PortalGunSettings.enableDebugMode)
                    {
                        Log.Message($"[Portal Gun] Checking inventory item: {item.def.defName}");
                    }
                    if (item.def == PortalGunDefOf.PortalGun_Device)
                    {
                        if (PortalGunSettings.enableDebugMode)
                        {
                            Log.Message($"[Portal Gun] Found portal gun device in inventory!");
                        }
                        return true;
                    }
                }
            }

            if (PortalGunSettings.enableDebugMode)
            {
                Log.Message($"[Portal Gun] No portal gun device found for {pawn.LabelShort}");
            }
            return false;
        }

        private static int CalculatePathCost(IntVec3 start, IntVec3 end, Map map, Pawn pawn)
        {
            // Use RimWorld's actual pathfinding system to get accurate cost
            try
            {
                PathFinderCostTuning tuning = PathFinderCostTuning.For(pawn);
                TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors);
                
                // Try to find the actual path using RimWorld's pathfinder
                PawnPath path = map.pathFinder.FindPathNow(start, end, traverseParms, tuning, PathEndMode.OnCell);
                
                if (path != null && path.Found)
                {
                    int totalCost = (int)path.TotalCost;
                    path.ReleaseToPool(); // Important: release path back to pool
                    
                    if (PortalGunSettings.enableDebugMode)
                    {
                        Log.Message($"[Portal Gun] Actual path cost from {start} to {end}: {totalCost}");
                    }
                    
                    return totalCost;
                }
                else
                {
                    // Fallback to distance estimation if no path found
                    int fallbackCost = (int)(start.DistanceTo(end) * 20f); // Higher cost for unreachable areas
                    
                    if (PortalGunSettings.enableDebugMode)
                    {
                        Log.Message($"[Portal Gun] No path found from {start} to {end}, using fallback cost: {fallbackCost}");
                    }
                    
                    return fallbackCost;
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[Portal Gun] Error calculating path cost: {e.Message}");
                // Fallback to simple distance calculation
                return (int)(start.DistanceTo(end) * 15f);
            }
        }
    }
}
