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
        [DebugAction("Portal Gun", "Force Create Portal", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceCreatePortal()
        {
            Pawn selectedPawn = Find.Selector.SelectedPawns.FirstOrDefault();
            if (selectedPawn == null)
            {
                Messages.Message("No pawn selected", MessageTypeDefOf.RejectInput);
                return;
            }

            Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), delegate(LocalTargetInfo target)
            {
                if (target.IsValid)
                {
                    IntVec3 entryPos = selectedPawn.Position;
                    IntVec3 exitPos = PortalSpawner.FindBestExitPosition(target.Cell, selectedPawn.Map);
                    
                    if (PortalSpawner.TrySpawnPortalPair(selectedPawn, entryPos, exitPos, target))
                    {
                        Messages.Message($"Portal created from {entryPos} to {exitPos}", MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message("Failed to create portal", MessageTypeDefOf.RejectInput);
                    }
                }
            }, null, null, "Select portal destination");
        }

        [DebugAction("Portal Gun", "Show Path Cost", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ShowPathCost()
        {
            Pawn selectedPawn = Find.Selector.SelectedPawns.FirstOrDefault();
            if (selectedPawn == null)
            {
                Messages.Message("No pawn selected", MessageTypeDefOf.RejectInput);
                return;
            }

            Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), delegate(LocalTargetInfo target)
            {
                if (target.IsValid)
                {
                    // Calculate path cost using the same method as the Harmony patch
                    try
                    {
                        PathFinderCostTuning tuning = PathFinderCostTuning.For(selectedPawn);
                        TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors);
                        
                        PawnPath path = selectedPawn.Map.pathFinder.FindPathNow(selectedPawn.Position, target.Cell, traverseParms, tuning, PathEndMode.OnCell);
                        
                        if (path != null && path.Found)
                        {
                            int totalCost = path.TotalCost;
                            float distance = selectedPawn.Position.DistanceTo(target.Cell);
                            bool wouldCreatePortal = totalCost > PortalGunSettings.minimumPathCostThreshold;
                            
                            string message = $"Path cost: {totalCost}\nDistance: {distance:F1}\nThreshold: {PortalGunSettings.minimumPathCostThreshold}\nWould create portal: {wouldCreatePortal}";
                            
                            Messages.Message(message, MessageTypeDefOf.NeutralEvent);
                            path.ReleaseToPool();
                        }
                        else
                        {
                            Messages.Message("No path found!", MessageTypeDefOf.RejectInput);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Messages.Message($"Error calculating path: {e.Message}", MessageTypeDefOf.RejectInput);
                    }
                }
            }, null, null, "Select destination for path cost calculation");
        }

        [DebugAction("Portal Gun", "Teleport Pawn", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TeleportPawn()
        {
            Pawn selectedPawn = Find.Selector.SelectedPawns.FirstOrDefault();
            if (selectedPawn == null)
            {
                Messages.Message("No pawn selected", MessageTypeDefOf.RejectInput);
                return;
            }

            Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), delegate(LocalTargetInfo target)
            {
                if (target.IsValid && target.Cell.InBounds(selectedPawn.Map) && target.Cell.Walkable(selectedPawn.Map))
                {
                    // Mimic farskip teleportation
                    IntVec3 originalPos = selectedPawn.Position;
                    
                    // Entry effects
                    FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(selectedPawn, FleckDefOf.PsycastSkipFlashEntry, new Vector3(-0.5f, 0f, -0.5f));
                    dataAttachedOverlay.link.detachAfterTicks = 5;
                    selectedPawn.Map.flecks.CreateFleck(dataAttachedOverlay);
                    
                    EffecterDefOf.Skip_Entry.Spawn(selectedPawn, selectedPawn.Map).Cleanup();
                    SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(originalPos, selectedPawn.Map));
                    
                    // Teleport
                    selectedPawn.teleporting = true;
                    selectedPawn.ExitMap(false, Rot4.Invalid);
                    selectedPawn.teleporting = false;
                    
                    GenSpawn.Spawn(selectedPawn, target.Cell, selectedPawn.Map);
                    
                    // Exit effects
                    EffecterDefOf.Skip_ExitNoDelay.Spawn(target.Cell, selectedPawn.Map).Cleanup();
                    SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(target.Cell, selectedPawn.Map));
                    
                    // Notify teleported
                    selectedPawn.Notify_Teleported();
                    
                    // Fog update
                    if ((selectedPawn.Faction == Faction.OfPlayer || selectedPawn.IsPlayerControlled) && target.Cell.Fogged(selectedPawn.Map))
                    {
                        FloodFillerFog.FloodUnfog(target.Cell, selectedPawn.Map);
                    }
                    
                    Messages.Message($"{selectedPawn.LabelShort} teleported from {originalPos} to {target.Cell}", MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message("Invalid teleport destination", MessageTypeDefOf.RejectInput);
                }
            }, null, null, "Select teleport destination");
        }

        [DebugAction("Portal Gun", "Clear All Portals", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ClearAllPortals()
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
