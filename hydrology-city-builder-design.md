# Hydrology-Focused City Builder: Design Notes and MVP Build Plan

*A self-contained summary of the design decisions, architecture, and build order for a tile-based city builder (Age of Empires scale) where water is the central system. Written for readers with no prior context.*

---

## 1. The idea in one paragraph

A tile-based city builder at roughly Age of Empires map scale (about 120×120 to 250×250 tiles) in which **hydrology is the core mechanic**. Streams, lakes, and underground water respond realistically to terrain elevation. Players irrigate with canals, build dams, dig wells, and live with the consequences: over-irrigating upstream starves downstream farms, over-pumping dries neighboring wells, and polluting upstream poisons downstream water. Most of the "interesting decisions" are meant to come from the water system rather than from art or content volume.

## 2. Scope decisions already made

| Question | Decision | Why |
|---|---|---|
| Rendering | 3D engine, constrained isometric-style camera | Free depth sorting, free camera rotation, lighting for free; the genre standard today |
| Terrain data | Heightmap (one elevation per tile), not voxels | Every algorithm in this plan assumes it; voxels are a much bigger project |
| Underground | 2–4 discrete 2D layers (aquifers), not a voxel underground | Gives real groundwater behavior at near-heightmap cost |
| Underground construction (pipes, tunnels, basements) | **Out of MVP, door left open** | Biggest scope item, most separable from the hydrology work |
| Sewage | Abstract: coverage-radius treatment plus a pollution field | Keeps "upstream pollution poisons downstream wells" without a pipe-network simulation |
| Irrigation | Surface canals using the existing water sim | Needs no underground construction |
| Engine | **Unity 6 with C#** (Godot 4 is a viable alternative) | Strong precedent for this genre, good simulation tooling |
| Architecture | Simulation lives in an engine-independent library | Headless testing and engine portability |
| Map scale | One tile is 10 m (one cottage with its plot); a height step is 2 m; a tile rising more than 10 m across is a cliff | A city map is a few km across; every slope is at least one 2 m step (a 20% grade), so it reads as a slope from the angled camera. Gentle real slopes become small terraces, which is the expected stylized look |
| Climate | Two maps: a coarse **world map** computes climate; the **city map** takes one regional climate from its site | At a few km, real climate barely varies (one region, about 1 °C across the relief); rain shadows and coast-to-interior gradients belong at world scale, as in Dwarf Fortress and RimWorld |

## 3. System design

### 3.0 World map and city map

The game has two maps. The **world map** is coarse (each cell stands for a large area) and exists to pick a site and give it a climate. The **city map** is where the player builds, at 10 m tiles. The world map is not built yet; the city map is built first and takes its regional climate as settings until then.

| Algorithm | World map | City map |
|---|---|---|
| Fractal noise, redistribution curve, flatness field (3.1) | Continents and mountain ranges | Hills and plains |
| Thermal and hydraulic erosion (3.1, 3.2) | Yes | Yes |
| Pit filling, D8 flow direction, flow accumulation, river extraction, drainage basins (3.2) | Major rivers | Streams, and rivers entering from the map edge |
| Validity check and reroll (3.2) | Is there a habitable site? | Is there a usable river and enough flat land? |
| Whittaker biome lookup (3.1) | From the computed climate fields | From the regional climate plus local wetness |
| Wind-sweep climate with rain shadows (`rainfall-simulation-sweep-design.md`), latitude, sea level and coastlines | Yes | No |
| Site selection, producing the city map's regional climate and river inflow | Yes | No |
| Surface water, groundwater, player water tools, irrigation, sewage, economy (3.3 to 3.7) | No | Yes |
| Camera, X-ray, overlays (3.8) | Simple top-down view | Yes |

**Shared algorithms do not assume the city's scale.** The algorithms in both columns work in cells and height steps. Anything that needs metres (cell size, height step, a slope threshold) takes the scale as a parameter instead of reading the city map's scale directly, so the world map can reuse them.

**The world map feeds the city map.** The site's climate becomes the city map's regional climate settings, and a large river crossing the site enters the city map at an edge with a given flow, so city hydrology takes edge inflow as an input.

### 3.1 Terrain generation (runs once, at map creation)

The goal is varied land (mountains, plains, valleys, deserts, wetlands), not one uniform eroded mountain range.

**Elevation.** Multi-octave coherent noise (Perlin or Simplex, fractal Brownian motion) gives the base heightmap. A redistribution curve (a power function or custom spline) compresses the middle of the range so most land sits at a comfortable plains elevation with occasional peaks and basins.

**Plains.** Generate a second, low-frequency "flatness/roughness" noise field. Use it to scale down the high-frequency octaves where it is high. This is the same principle Minecraft 1.18 uses: a separate field controls how much detail is allowed through, rather than smoothing elevation after the fact. Thermal erosion (material slumping off slopes steeper than a threshold) can clean up leftover bumps.

**Feature sizes in metres.** Hill spacing and plain size are given in metres, not per map width, so a larger map shows more land at the same scale instead of the same land stretched.

**Climate on the city map.** Temperature is the region's sea-level temperature cooled with height by the standard lapse rate (6.5 °C per km); there is no temperature noise, since real local differences at this scale come from terrain, water and vegetation. Precipitation is the region's annual amount (mm per year) everywhere on the map, and surface humidity is the region's air humidity read against each vertex's ground temperature. Biome indices use fixed scales, the same for every map.

**Biomes.** Do not derive biome from elevation alone. Classify biome with a Whittaker-style 2D lookup (temperature vs. moisture). On the world map, temperature and rainfall fields come from the climate model, including a rain-shadow pass that walks the prevailing wind and dries the leeward side of tall terrain, so deserts appear behind mountains for a physical reason (`rainfall-simulation-sweep-design.md`). On the city map the regional climate is one point on the diagram, and variety within the map comes from local wetness (river banks, floodplains, a shallow water table, marshes) and slope. Dwarf Fortress takes a similar multi-field approach (separate elevation, rainfall, temperature, drainage, and other fields), and rejects and regenerates worlds that fail the player's constraints.

### 3.2 Static hydrology (also at map creation)

1. **Pit filling.** Noise terrain has many tiny local minima where flow would stop. Fill them (priority-flood or similar) so every cell drains somewhere. Heights are whole 2 m steps, so the terrain has large flats (terraces, and the filled pits themselves) with no downhill direction; resolve flow across flats (priority-flood with flat resolution, or a tiny epsilon gradient toward the flat's outlet) so water still leaves them.
2. **D8 flow direction.** Each cell points to its steepest-descent neighbor among 8.
3. **Flow accumulation.** Count how much upstream area drains through each cell (process in topological order, not recursively). High accumulation becomes rivers; width scales with flow.
4. **Erosion.** Droplet-based hydraulic erosion (droplets pick up sediment when fast and deposit it when slow) carves valleys along the flow paths. Re-run flow routing afterward.
5. **Drainage basin IDs.** Flood-fill backward from each river mouth so every tile knows which watershed it belongs to.
6. **Validity check.** Reject and reroll seeds without a usable river, enough flat land, or a reasonable biome mix.

### 3.3 Dynamic surface water (runs every tick)

A cellular-automaton "shallow water" model.

- Each tile stores `terrain_height` (static) and `water_depth` (dynamic). Flow is driven by `surface_height = terrain_height + water_depth`.
- Per tick: water flows **down first**, then **sideways to equalize** with neighbors. All moves are written to a second buffer and swapped at the end (in-place updates introduce directional bias).
- **Stability matters.** Moving the full height difference each tick causes checkerboard oscillation. The fixes are to divide equalization flow by the neighbor count (about 4) and to apply extra damping to very small flows. Treat the CA as a relaxation process that needs damping.
- **Sources and sinks:** rainfall adds water, evaporation removes it, and the map edge or ocean drains.
- **Performance:** track an *active set* of tiles (wet, or adjacent to something that changed) instead of iterating the whole map. Tick the water sim at a rate decoupled from rendering.
- **Optional upgrade:** the "virtual pipes" model (per-edge flow rate with inertia) gives more convincing fast floods and dam breaks, at the cost of more state and tuning. Start with simple mass equalization.

### 3.4 Groundwater (slow tick)

- Model the **water table as an elevation field**, like terrain but underground. Initialize it as a heavily smoothed copy of the terrain, offset downward.
- Update it slowly (about once per in-game day), by diffusion or relaxation.
- **Recharge:** rain and surface water raise it. **Extraction:** wells lower it, creating a local drawdown that refills over time.
- **Surface interaction** comes from comparing three heights: where the table meets the surface you get springs, marshes, and gaining streams; where river water sits above the table the river is losing water into the aquifer.
- Use 2–4 layers (for example topsoil, shallow aquifer, bedrock, deep aquifer) with a vertical permeability value between layers.

### 3.5 Player interaction with water

All of these are edits to what the simulation already reads, not new systems:

- **Dam/weir:** mark edges between tiles as blocked.
- **Canal:** lower the bed along a player-chosen path so it behaves like a narrow riverbed in the same water sim. A gravity-fed canal must be elevation-monotonic from source to field, which is a real engineering puzzle.
- **Gate/sluice:** toggles a blocked edge on and off.
- **Conservation of mass** means a canal draws real water from its source, so upstream over-use naturally causes downstream scarcity without hand-scripting.

### 3.6 Irrigation and fertility

Farmland fertility is a *read* of fields that already exist:

- **Rain-fed:** baseline from local rainfall.
- **Flood-plain:** a bonus proportional to nearby surface water depth.
- **Sub-irrigation:** a smaller bonus where the water table is shallow.
- **Canal-irrigated:** the highest yield, at the cost of building and maintaining the canal.

### 3.7 Sewage and pollution (abstract, v1)

Buildings generate waste in proportion to population. A treatment building covers a radius. Untreated waste writes a pollution value into the local water field. That pollution then travels through the existing surface and groundwater simulations, so downstream wells are really affected. A real pipe network can be added later as a more efficient way to achieve the same coverage effect.

### 3.8 Rendering and camera

- **Terrain:** one continuous deformable mesh with splat-mapped textures (4–6 biome textures blended by the biome weights from worldgen, plus a slope-based rock rule). No hand-authored tile sets.
- **Water:** one material driven by the depth field (color and opacity by depth, scrolling normals, foam at the shoreline), reused for rivers, lakes, canals, and floods.
- **Buildings obstructing buildings:** correct sort order comes free with a 3D engine. Camera rotation is the primary answer; translucent "X-ray" of occluders around the selection is a supplement.
- **Valleys** block the view from two sides at once, so rotation alone is not enough. The combined fix is: pitch that steepens as the camera zooms out, optional axis-aware view suggestions, a display-only vertical-exaggeration slider (simulation elevation is unchanged), terrain X-ray on the focused selection, and information overlays and a minimap that never depend on the 3D view being clear.
- **Underground information** (the water table) is shown as a toggleable heatmap overlay on the normal view, not as geometry.

## 4. MVP art budget

Because terrain and water are shader- and data-driven, the art list is short.

| Category | Count |
|---|---|
| Terrain textures | 4–6 |
| Water material | 1 |
| Buildings and structures | 10–12 (town center, 1–3 houses, well, storehouse, market, one production chain of 2, dam) |
| Rigged characters | 2 (one worker, one hauling unit) |
| Scatter props | 5–8 |
| UI icons | about 30 |

Farm fields and roads can be ground-texture overlays rather than meshes. If you want to validate gameplay first, **grey-box every building** (labeled primitives at the right footprint) and put real effort only into the terrain shader and water material.

## 5. Technology

**Engine:** Unity 6 with C#. Reasons: precedent for this genre (Cities: Skylines is a Unity game), the Job System and Burst compiler for parallel simulation, and mature shader and terrain tooling. Godot 4 is a capable open-source alternative with a thinner 3D ecosystem. Unreal is more than this project needs. Check current Unity licensing terms before committing, since they have changed in the past.

**Architecture:** keep all simulation (noise, erosion, flow routing, water CA, groundwater, economy) in a standalone C# library with no engine dependency. The engine layer only renders and handles input. This lets you unit-test the simulation headlessly and swap engines later if needed.

## 6. MVP build order

### Phase 0: Foundations
1. Simulation library skeleton plus a test project. Use flat arrays and an `Index(x,y)` helper.
2. Deterministic seeded RNG passed into every generator.
3. Debug visualizer: render any float field as a heatmap, with a field selector.
4. Minimal engine shell: heightmap to mesh, orthographic camera, pan and zoom.

*Done when:* a seeded heightmap appears as a mesh and a heatmap.

### Phase 1: Static terrain
5. Noise library (Simplex or Perlin, fractal octaves).
6. Elevation generator with redistribution curve.
7. Flatness/roughness field gating the high octaves.
8. City map climate: temperature cooled with height, the region's precipitation, surface humidity. (The world map's climate, with rain shadows, comes with the world map.)
9. Biome classifier (Whittaker lookup) producing IDs and splat weights. On the city map, best done after Phase 2, once local wetness exists.
10. Splat-map terrain shader.

*Done when:* different seeds give visibly distinct mountains, plains, and biomes.

### Phase 2: Static hydrology
11. Pit filling.
12. D8 flow direction.
13. Flow accumulation.
14. River extraction.
15. Thermal erosion.
16. Hydraulic (droplet) erosion, then re-run flow routing.
17. Drainage basin IDs.
18. Map validity check and reroll.

*Done when:* valleys and rivers line up, and about 20 seeds look sensible by eye.

### Phase 3: Surface water simulation
19. Water depth array and surface height.
20. Core CA step: down first, then sideways equalization, double buffer.
21. Damping and minimum-flow threshold.
22. Sources and sinks.
23. Initialization from worldgen (rivers full, rest dry).
24. Active-tile tracking (after the naive version is correct).
25. Water shader.
26. Headless tests: mass conservation, closed basins settle, no oscillation, determinism.

*Done when:* a river-fed map settles stably, mass is conserved, and dry tiles cost nothing per tick.

### Phase 4: Player terraforming of water
27. Per-edge open/blocked connectivity layer.
28. Dam/weir.
29. Canal tool with a monotonic-downhill check and placement feedback.
30. Gate/sluice.
31. Command layer: every player action is a command object (enables undo, save/load, replay).

*Done when:* you can dam a river, watch a lake fill, divert it down a canal, and close the gate.

### Phase 5: Groundwater
32. Aquifer layer data model with vertical permeability.
33. Initial water table (smoothed, offset terrain).
34. Slow-tick diffusion.
35. Recharge, including losing-river behavior.
36. Springs and marshes where the table meets the surface.
37. Wells with local drawdown and refill.
38. Water-table heatmap overlay.

*Done when:* a new canal visibly raises the nearby water table and heavy pumping makes neighboring wells fail.

### Phase 6: Camera and readability
39. Camera rig: 4-direction rotation, zoom-linked pitch, bounds.
40. Display-only vertical exaggeration.
41. Terrain X-ray on focused selection.
42. Axis-aware focus suggestion (optional).
43. General overlay toggle framework.

### Phase 7: Economy loop
44. Grid occupancy and footprint placement rules.
45. Data-driven building definitions and placement tool (grey-box prefabs).
46. Resource and inventory model.
47. A* pathfinding with terrain and water costs.
48. Worker agents and a job system (haul, work, eat/drink).
49. Fertility model reading rainfall, surface water, and water table.
50. Farms.
51. One production chain.
52. Population: housing, water and food needs, growth and decline.

*Done when:* a settlement can survive or fail depending on its water decisions.

### Phase 8: Consequences
53. Pollution field and coverage-radius treatment building.
54. Pollution transport through surface and groundwater.
55. Health effects from contaminated water.
56. Drought and flood events through rainfall modifiers.

### Phase 9: Shipping the MVP
57. Time controls and decoupled tick rates (surface water fast, groundwater slow, economy medium).
58. Save/load (seed plus command log or state arrays, plus tick counters).
59. Minimal UI: build menu, resource bar, overlay toggles, tile inspector tooltips.
60. Tutorial prompts.
61. Profiling pass (active-set size, per-tick budget, worldgen time).

## 7. Priorities, risks, and cut lines

- **Phases 3–5 are the real MVP.** If water behaves convincingly on grey boxes, the project works. Phases 7–9 turn it into a game.
- **Do not skip** the debug visualizer (3), the headless tests (26), the command layer (31), or the map validity check (18). They are cheap early and expensive to retrofit.
- **The risky parts are early on purpose.** The water CA and its coupling to groundwater decide whether the concept works; the economy is conventional.
- **First cuts if scope grows:** hydraulic erosion (noise plus thermal erosion is enough), the world map (the city map works with a hand-picked regional climate), terrain X-ray, gates, and the whole pollution phase.
- **World map:** terrain and static hydrology at world scale with the shared algorithms, the wind-sweep climate, then site selection feeding the city map's regional climate and river inflow.
- **Future doors left open:** underground construction (qanats as underground irrigation tunnels are a natural first use), pumps and waterwheels as the historically honest way to move water uphill, and a real sewage pipe network.

## 8. Reference material

Terrain and noise
- Red Blob Games, "Making maps with noise functions": https://www.redblobgames.com/maps/terrain-from-noise/
- Red Blob Games, "Noise Functions and Map Generation": https://www.redblobgames.com/articles/noise/introduction.html
- mapgen4 (island generator with simulated river flow): https://github.com/redblobgames/mapgen4

Dwarf Fortress worldgen
- DF Wiki, Advanced world generation: https://dwarffortresswiki.org/index.php/Advanced_world_generation
- Game Developer, Q&A with Tarn Adams: https://www.gamedeveloper.com/design/q-a-dissecting-the-development-of-i-dwarf-fortress-i-with-creator-tarn-adams

Erosion implementations
- SebLague/Hydraulic-Erosion (C#/Unity, droplet-based): https://github.com/SebLague/Hydraulic-Erosion
- csaddison/Hydraulic-Erosion-Sim (simpler, easier to read end to end): https://github.com/csaddison/Hydraulic-Erosion-Sim

Water simulation
- "Simple Fluid Simulation With Cellular Automata" (the common reference for tile-based water): https://w-shadow.com/blog/2009/09/01/simple-fluid-simulation/

Other procedural generation techniques mentioned
- RogueBasin, cellular automata caves: https://www.roguebasin.com/index.php/Cellular_Automata_Method_for_Generating_Random_Cave-Like_Levels
- Wave Function Collapse (original implementation): https://github.com/mxgmn/WaveFunctionCollapse
- Minecraft Wiki, World generation: https://minecraft.wiki/w/World_generation

## 9. Glossary

- **Heightmap:** a grid with one elevation value per tile.
- **fBm (fractal Brownian motion):** summing noise at several frequencies and amplitudes (octaves) to get natural-looking detail.
- **Whittaker diagram:** an ecology chart classifying biomes by temperature and precipitation; used as a 2D lookup table in worldgen.
- **D8 flow routing:** each cell drains to its steepest-descent neighbor among 8.
- **Flow accumulation:** for each cell, the amount of upstream area draining through it.
- **Hydraulic erosion:** simulated water carving terrain by picking up and depositing sediment.
- **Thermal erosion:** material sliding off slopes steeper than a stability threshold.
- **Cellular automaton (CA):** a grid simulation where each cell updates from simple local rules based on its neighbors.
- **Water table:** the underground elevation at which soil becomes saturated.
- **Gaining / losing stream:** a stream the water table feeds, versus one that leaks into the aquifer.
- **Splat mapping:** blending several terrain textures by per-tile weights.
- **Qanat:** a historical gravity-fed underground irrigation tunnel.
