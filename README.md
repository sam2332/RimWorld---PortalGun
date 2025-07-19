# Rick's Portal Gun – Smart Teleporter AI

A RimWorld mod that enables intelligent portal-based teleportation for pawns when traversing inefficient or long paths.

## Features

- **Automatic Portal Creation**: When a pawn's path cost exceeds a configurable threshold, portal pairs automatically spawn
- **Smart Pathfinding Integration**: Uses RimWorld's native pathfinding system to accurately calculate travel costs
- **Seamless Teleportation**: Based on the farskip psycast mechanics for reliable teleportation
- **Visual & Audio Effects**: Green portal visuals with appropriate sound effects
- **Configurable Settings**: Adjust thresholds, durations, and effects through mod settings
- **Debug Tools**: Developer tools for testing and visualization

## How It Works

1. When a pawn starts moving to a destination, the mod calculates the actual path cost using RimWorld's pathfinding
2. If the cost exceeds the threshold (default: 300), an entry portal spawns at the pawn's position and an exit portal near the destination
3. The pawn walks into the entry portal and emerges from the exit portal
4. Portals automatically despawn after use or timeout
5. The pawn continues to their original destination after teleporting

## Mod Settings

- **Minimum Path Cost Threshold**: Cost required to trigger portal creation (100-1000, default: 300)
- **Enable Portal Visualization**: Show visual effects and sounds (default: enabled)
- **Enable Debug Mode**: Show detailed logging for troubleshooting (default: disabled)
- **Portal Duration**: How long portals last before auto-despawning (1-30 seconds, default: 5)
- **Stun Duration**: Brief stun after teleporting (0-300 ticks, default: 60)

## Debug Tools (Dev Mode)

With developer mode enabled, access these debug actions:

- **Force Create Portal**: Manually create a portal to any location
- **Show Path Cost**: Display exact path costs and threshold comparisons
- **Teleport Pawn**: Instantly teleport a selected pawn
- **Clear All Portals**: Remove all portals from the map

## Technical Details

The mod uses Harmony patches to hook into RimWorld's pathfinding system at `Pawn_PathFollower.StartPath()`. It leverages the same teleportation mechanics as the farskip psycast ability for maximum compatibility and reliability.

### Portal Mechanics

- Portals use `EffecterDefOf.Skip_Entry` and `EffecterDefOf.Skip_Exit` for effects
- Teleportation follows the farskip pattern: `ExitMap()` → `GenSpawn.Spawn()` → `Notify_Teleported()`
- Proper handling of fog of war, inventory unloading, and drafted state
- Safe exit position finding with fallbacks

## Compatibility

- Requires RimWorld 1.6+
- Compatible with most mods that don't heavily modify pathfinding
- Loads after Royalty DLC to ensure psycast effecters are available

## Installation

1. Subscribe to the mod on Steam Workshop or download manually
2. Enable in mod list (should load after Royalty if present)
3. Configure settings in Options → Mod Settings → Portal Gun

## Performance Notes

The mod performs actual pathfinding calculations to determine costs, which may have a small performance impact on maps with complex layouts or many pawns. The performance impact is minimal for most gameplay scenarios.

## Troubleshooting

- Enable debug mode in settings to see detailed logs
- Use debug tools to test portal creation manually
- Check that pawns are colonists or player-controlled (other pawns are excluded by design)
- Verify portal spawn positions aren't blocked by impassable objects
