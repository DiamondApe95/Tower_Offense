# NavMesh Setup Guide

This guide explains how to set up Unity NavMesh for Tower Offense to enable builder pathfinding and movement.

## Why NavMesh is Required

The game uses Unity's Navigation system (NavMesh) for:
- **Builder units** moving from spawn points to construction sites
- **AI pathfinding** for any units that need to navigate the terrain
- **Dynamic obstacle avoidance** during gameplay

Without a baked NavMesh, builders and other AI units cannot move, resulting in errors like:
```
Failed to create agent because there is no valid NavMesh
SetDestination can only be called on an active agent that has been placed on a NavMesh
```

## How to Bake NavMesh for Each Level

### Step 1: Open Navigation Window
1. In Unity, go to `Window > AI > Navigation` (or `Window > Navigation` in older versions)
2. This opens the Navigation window with tabs: Agents, Areas, Bake, Object

### Step 2: Mark Walkable Surfaces
1. Select the terrain/ground GameObject in your scene
2. In the Inspector, check "Navigation Static" (or in the Navigation window's Object tab, set Navigation Area to "Walkable")
3. Repeat for all surfaces where builders should walk (platforms, bridges, etc.)

### Step 3: Configure Bake Settings
In the Navigation window's **Bake** tab, configure:
- **Agent Radius**: 0.5 (width of builder units)
- **Agent Height**: 1.0 (height of builder units)
- **Max Slope**: 45 (maximum walkable slope in degrees)
- **Step Height**: 0.4 (maximum step height)

### Step 4: Bake the NavMesh
1. Click the **Bake** button at the bottom of the Navigation window
2. Wait for Unity to generate the NavMesh data
3. You should see blue overlay on walkable surfaces in Scene view

### Step 5: Verify NavMesh Coverage
- Ensure NavMesh covers:
  - **Player base spawn point** (where PlayerBase is located)
  - **Enemy base spawn point** (where EnemyBase is located)
  - **All valid tower placement locations**
  - **Paths between bases and construction sites**

## NavMesh for Multiple Levels

You need to bake NavMesh **for each scene/level** separately:

1. Open `Level_1.unity` → Bake NavMesh → Save scene
2. Open `Level_2.unity` → Bake NavMesh → Save scene
3. Open `Level_3.unity` → Bake NavMesh → Save scene
4. Open `Level_4.unity` → Bake NavMesh → Save scene
5. Open `Level_5.unity` → Bake NavMesh → Save scene

## Troubleshooting

### "No NavMesh found near builder spawn position"
- The builder spawn point (usually the base) is not on NavMesh
- Select the ground near the base and mark it as Navigation Static
- Re-bake the NavMesh

### Builders not moving
- Verify NavMesh is baked (blue overlay visible in Scene view)
- Check that both spawn point AND construction site locations have NavMesh coverage
- Ensure there's a valid path between spawn and destination

### NavMesh gaps or holes
- Increase **Max Slope** if terrain is too steep
- Decrease **Agent Radius** if passages are too narrow
- Mark additional surfaces as Navigation Static

## Code Implementation

The game now includes defensive checks for missing NavMesh:

### BuilderController.cs
- Checks `agent.isOnNavMesh` before calling `SetDestination()`
- Checks `agent.isOnNavMesh` before accessing `remainingDistance`
- Logs warnings when NavMesh is not available

### ConstructionManager.cs
- Uses `NavMesh.SamplePosition()` to find valid spawn positions
- Falls back to default builder creation if prefab is missing
- Logs warnings when builders spawn outside NavMesh

## Best Practices

1. **Bake after terrain changes**: Re-bake whenever you modify terrain or add/remove static obstacles
2. **Test each level**: Play each level to verify builders can reach all construction sites
3. **Use NavMesh visualization**: Enable NavMesh display in Scene view to verify coverage
4. **Keep Agent Radius consistent**: Should match actual builder unit size (0.5 units)

## References

- Unity NavMesh Documentation: https://docs.unity3d.com/Manual/nav-BuildingNavMesh.html
- Unity Navigation System: https://docs.unity3d.com/Manual/Navigation.html
