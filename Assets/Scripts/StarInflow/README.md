# Star Inflow (experimental, self-contained)

Stars orbit the black hole, spiral in faster as they approach, vanish at the event horizon and recycle.
Nothing here modifies existing project assets. Delete this folder to remove it.

## Files
- `StarInflow.shader`  moves every star in the vertex shader (orbit, spiral, fade, recycle) and hides stars that pass behind the hole.
- `StarInflow.cs`      builds the star mesh, finds the black hole (even if spawned at runtime) and feeds the shader.

## Setup (in the PCA scene copy only)
1. Create an empty GameObject, name it `Star Inflow`, add the `StarInflow` component. The Shader field fills in automatically (drag `StarInflow.shader` in if not).
2. Optional: drag the black hole's MeshRenderer into **Black Hole Renderer**. Left empty, it is found at runtime.
3. Rotate the empty object to tilt the stars' orbital disc (its Up axis is the disc axis).
4. Press Play. Tune the sliders live.

## Tuning
- Horizon: by default `SpherePercentage x object scale` (same as the graph). If the stars vanish too early/late, untick **Auto Horizon Radius** and set the override. Select the object to see wire spheres in the Scene view.
- Too sparse/dense: **Star Count**. Too fast/slow: **Lifetime**. Orbit tightness: **Orbit Turns**, **Spin Up**, **Infall Exponent**.
- Disc vs ball: **Flatten**. Spawn region: **Outer Radius Multiple** / **Inner Start Multiple** (multiples of the horizon radius).
- Quest performance: start at 1500 stars; the cost is almost all GPU fill, so raise gradually.

## Known limits
- Stars are ordinary scene objects, so the lens does not bend their light.
- They are drawn after the black hole sphere; stars behind the core are discarded by the shader.
