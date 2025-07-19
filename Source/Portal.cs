using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Linq;g RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace PortalGun
{
    public class Portal : ThingWithComps
    {
        public Portal linkedPortal;
        public Pawn owner;
        public int spawnTick;
        public bool isEntryPortal;
        public LocalTargetInfo originalDestination;

        private int TicksUntilDespawn => spawnTick + (int)(PortalGunSettings.portalDuration * 60) - Find.TickManager.TicksGame;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                spawnTick = Find.TickManager.TicksGame;
            }
        }

        public override void Tick()
        {
            base.Tick();

            // Check if portal should despawn
            if (TicksUntilDespawn <= 0)
            {
                DestroyPortalPair();
                return;
            }

            // Check for pawns entering the portal
            if (isEntryPortal)
            {
                CheckForPawnEntry();
            }
        }

        private void CheckForPawnEntry()
        {
            if (linkedPortal?.Spawned != true) return;

            // Check all pawns on this cell and adjacent cells (in case of minor positioning differences)
            foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(Position).Append(Position))
            {
                if (!cell.InBounds(Map)) continue;

                foreach (Thing thing in cell.GetThingList(Map))
                {
                    if (thing is Pawn pawn && pawn == owner && !pawn.Dead && !pawn.Downed)
                    {
                        // Check if pawn is moving towards the portal or is on the portal
                        if (pawn.Position == Position || 
                            (pawn.pather.Moving && pawn.pather.Destination.Cell == Position))
                        {
                            TeleportPawn(pawn);
                            return;
                        }
                    }
                }
            }
        }

        private void TeleportPawn(Pawn pawn)
        {
            if (linkedPortal?.Spawned != true) return;

            // Entry effects - following farskip pattern
            if (PortalGunSettings.enablePortalVisualization)
            {
                // Entry fleck and sound
                FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(pawn, FleckDefOf.PsycastSkipFlashEntry, new Vector3(-0.5f, 0f, -0.5f));
                dataAttachedOverlay.link.detachAfterTicks = 5;
                pawn.Map.flecks.CreateFleck(dataAttachedOverlay);
                
                // Entry effecter (like farskip)
                EffecterDefOf.Skip_Entry.Spawn(pawn, Map).Cleanup();
                
                // Entry sound
                SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(Position, Map));
            }

            // Find safe exit position near linked portal
            IntVec3 exitPosition = linkedPortal.Position;
            if (!CanOccupyCell(exitPosition, pawn))
            {
                // Use RimWorld's cell finder like farskip does
                if (!CellFinder.TryFindRandomSpawnCellForPawnNear(linkedPortal.Position, Map, out exitPosition, 4, 
                    (IntVec3 cell) => cell != linkedPortal.Position && CanOccupyCell(cell, pawn)))
                {
                    Log.Warning("[Portal Gun] Could not find safe exit position for teleport");
                    return;
                }
            }

            // Set teleporting flag and exit map (like farskip)
            pawn.teleporting = true;
            pawn.ExitMap(false, Rot4.Invalid);
            pawn.teleporting = false;

            // Spawn at destination (like farskip)
            GenSpawn.Spawn(pawn, exitPosition, Map);

            // Exit effects
            if (PortalGunSettings.enablePortalVisualization)
            {
                // Exit effecter
                EffecterDefOf.Skip_ExitNoDelay.Spawn(exitPosition, Map).Cleanup();
                
                // Exit sound
                SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(exitPosition, Map));
            }

            // Set drafted state if colonist (like farskip)
            if (pawn.drafter != null && pawn.IsColonistPlayerControlled && !pawn.Downed)
            {
                pawn.drafter.Drafted = true;
            }

            // Stun the pawn (like farskip)
            if (PortalGunSettings.stunTicksAfterTeleport > 0)
            {
                pawn.stances.stunner.StunFor(PortalGunSettings.stunTicksAfterTeleport, owner, false, false);
            }

            // Notify teleported (like farskip)
            pawn.Notify_Teleported();

            // Send skip signal (like farskip)
            Find.SignalManager.SendSignal(new Signal("CompAbilityEffect.SkipUsed", 
                exitPosition.Named("POSITION"), pawn.Named("SUBJECT")));

            // Fog of war update (like farskip)
            if ((pawn.Faction == Faction.OfPlayer || pawn.IsPlayerControlled) && exitPosition.Fogged(Map))
            {
                FloodFillerFog.FloodUnfog(exitPosition, Map);
            }

            // Unload inventory if at home (like farskip)
            if ((pawn.IsColonist || pawn.RaceProps.packAnimal || pawn.IsColonyMech) && Map.IsPlayerHome)
            {
                pawn.inventory.UnloadEverything = true;
            }

            // Resume original job if possible
            if (originalDestination.IsValid && pawn.jobs?.curJob != null)
            {
                // Try to continue to original destination
                Job continueJob = JobMaker.MakeJob(JobDefOf.Goto, originalDestination);
                continueJob.locomotionUrgency = LocomotionUrgency.Walk;
                pawn.jobs.StartJob(continueJob, JobCondition.InterruptForced, null, false);
            }

            // Destroy portals after use
            DestroyPortalPair();
        }

        private bool CanOccupyCell(IntVec3 cell, Pawn pawn)
        {
            if (!cell.InBounds(Map) || !cell.Walkable(Map)) return false;
            
            foreach (Thing thing in cell.GetThingList(Map))
            {
                if (thing is Pawn otherPawn && otherPawn != pawn)
                    return false;
                if (thing.def.category == ThingCategory.Building && thing.def.passability == Traversability.Impassable)
                    return false;
            }
            return true;
        }

        public void DestroyPortalPair()
        {
            if (linkedPortal?.Spawned == true)
            {
                linkedPortal.linkedPortal = null;
                linkedPortal.Destroy();
            }
            linkedPortal = null;
            if (Spawned)
            {
                Destroy();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref linkedPortal, "linkedPortal");
            Scribe_References.Look(ref owner, "owner");
            Scribe_Values.Look(ref spawnTick, "spawnTick");
            Scribe_Values.Look(ref isEntryPortal, "isEntryPortal");
            Scribe_TargetInfo.Look(ref originalDestination, "originalDestination");
        }

        public override string GetInspectString()
        {
            string baseString = base.GetInspectString();
            string portalInfo = $"Portal type: {(isEntryPortal ? "Entry" : "Exit")}\n";
            portalInfo += $"Time remaining: {(TicksUntilDespawn / 60f):F1}s";
            if (linkedPortal?.Spawned == true)
            {
                portalInfo += $"\nLinked to: {linkedPortal.Position}";
            }
            if (owner != null)
            {
                portalInfo += $"\nOwner: {owner.LabelShort}";
            }
            
            return string.IsNullOrEmpty(baseString) ? portalInfo : baseString + "\n" + portalInfo;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            // Clean up linked portal reference when this portal is destroyed
            if (linkedPortal?.Spawned == true)
            {
                linkedPortal.linkedPortal = null;
            }
            base.Destroy(mode);
        }
    }
}
