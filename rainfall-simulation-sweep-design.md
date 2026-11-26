# Rainfall simulation: wind sweep for the world map

This model computes the climate of the **world map**, the coarse map on which the player picks a site (see section 3.0 of `hydrology-city-builder-design.md`). Each world-map cell stands for a large area, so the effects the model produces are real at that scale: rain shadows behind mountain ranges, wet windward slopes, and moisture decaying inland from the coast. The city map is a few km across, where real climate barely varies, so it does not run this model; it takes the climate of its site (temperature, annual precipitation, air humidity) and any river crossing the site from the world map.

The model uses the grid conventions of the shared algorithms: it works on the world map's own cells and height steps, and its cell size and height step are parameters, not the city map's scale.

## 1. Grid conventions (vertex storage)

Elevation and temperature are stored at **vertices** (tile corners), not tile centers. The moisture sweep also runs on vertices, so ridge crests keep their true height instead of being averaged away.

### 1.1 Two grids

| Grid | Size | Holds |
|---|---|---|
| Vertex grid (`V`) | `(tilesX + 1) × (tilesY + 1)` | `terrainElevationV`, `temperatureV`, `isWaterV`, the optional input `backgroundLiftV`, and the sweep outputs `precipitationV`, `surfaceHumidityV`, `cloudWaterV`, `cloudBaseHeightV`, `cloudBaseElevationV`, `cloudThicknessV`, `fogV` |
| Tile grid (`T`) | `tilesX × tilesY` | Derived values for consumers that need one value per tile (water cellular automaton, fertility, buildings, biome queries) |

Every field name carries its location as a suffix (`V` or `T`), and each grid has its own size and index helper:

```csharp
int IndexV(int x, int y) => y * (tilesX + 1) + x;
int IndexT(int x, int y) => y * tilesX + x;
```

Indexing a vertex array with the tile stride shears the whole field by one cell per row and raises no error, so these two helpers are the only places indexing is written by hand.

### 1.2 Sample positions

A vertex `(x, y)` sits at position `(x, y)` in tile units. A tile center sits at `(x + 0.5, y + 0.5)`. Noise is sampled at vertex positions directly, with vertex `y` running from 0 to `tilesY`. Do not mix tile-center and vertex sampling conventions in the same field, because that shifts one field half a tile against the other.

### 1.3 Converters

Exactly two conversions exist:

- `VertexToTile`: the mean of a tile's four corner values.
- `TileToVertex`: the mean of the adjacent tiles (four in the interior, two on an edge, one at a corner).

Cloud-base fields are undefined under clear sky and stored as `NaN`. `VertexToTile` averages only the corners that have a value and returns `NaN` when none do.

Derived tile values are produced once, at the end of the pipeline. The sweep never reads tile-averaged elevation, since averaging first would soften ridges and weaken rain shadows.

### 1.4 Masks

`isWaterV` is the primary mask: a vertex is water if its elevation is below sea level, or if any adjacent tile is a river or lake from the hydrology stage. Tiles are classified from vertices: all four corners water means open water, none means land, and a mix means coastal. Use these same rules everywhere.

### 1.5 Lane geometry

The wind is one of 8 compass directions `(dx, dy)`. A **lane** is a line of vertices along the wind, processed from upwind to downwind. Lanes start at the vertices whose predecessor `(x - dx, y - dy)` lies outside the grid. Every vertex lies on exactly one lane per direction.

Step lengths differ: 1 tile for axis-aligned winds and √2 tiles for diagonal winds. Every per-step fraction is therefore expressed per tile of travel and rescaled by the step length:

```csharp
float PerStep(float fractionPerTile, float stepLengthTiles)
    => 1f - MathF.Pow(1f - fractionPerTile, stepLengthTiles);
```

Additive rates (such as evaporation) are multiplied by the step length. Water deposited on the ground is recorded **per tile of travel**: the deposit in a step is divided by the step length. A diagonal vertex then holds the same precipitation depth as an axis-aligned vertex under the same conditions, and the two wind types are equally dry or wet for the same terrain.

All three conversions (shares compound, amounts multiply, deposits divide) live in one step object built once per sweep, so the sweep itself only ever works per step and never converts inline. Mixing them up in place is the easiest way to make diagonal winds wetter or drier than axis winds.

### 1.6 Edges

Boundary vertices represent half a cell and corner vertices a quarter. This is irrelevant for intensity fields such as precipitation depth. Any computation that sums water volume across vertices must weight each vertex by its area share. The exact conservation test (section 8) is run per lane, where the weighting is not needed.

### 1.7 Units

- **Temperature** is in °C.
- **Elevation** is in meters of simulated height, and `lapseRate` is in °C per meter (the real-world figure is about 0.0065).
- **Tile size for climate purposes** is `climateTileSizeMeters`, the real distance one world-map cell represents, typically 0.5 to 1 km, so that a map of a few hundred cells spans a plausible climate region. It is much larger than the city map's 10 m tiles.
- Game terrain is often vertically compressed compared with reality. Compensate by raising `lapseRate`, which stays a tuned value expressed in real units.
- The display-only vertical exaggeration used by the renderer never enters the climate pipeline. Temperature and the sweep read simulation elevation only.

## 2. Moisture and rainfall model

### 2.1 Physics being approximated

- **Capacity depends on temperature.** Warm air holds much more vapor than cold air, roughly 7% more per °C (Clausius-Clapeyron), which is exponential.
- **Rain comes from cloud, and cloud forms where the air is saturated.** Relative humidity is vapor divided by capacity. Saturation usually needs cooling by lifting.
- **Water is distributed over a layer of air, not a point.** The top of a moist layer saturates before its bottom, so clouds can form aloft while the air at ground level feels dry.
- **Rain removes a fraction of the cloud.** Cloud falls out over a characteristic distance (wind speed times a conversion delay), so moisture decays roughly exponentially with distance inland on flat ground.
- **Rain is not linear in humidity.** It is absent below an onset humidity, rises as more of the layer saturates, and is capped by the water available.

### 2.2 Capacity

Water is measured in **capacity units**, where 1 is the capacity of air at `referenceTemperature`:

```csharp
float CapacityFromTemperature(float tC)
    => MathF.Exp(capacityGrowth * (tC - referenceTemperature));
```

`referenceTemperature` is a fixed constant, 15 °C (about 10 g of vapor per kg of air), and not the map's sea-level temperature. A warmer map's air therefore carries more water at the same humidity and rains more, and `precipitationReference` (section 6.3) means the same on every map.

With temperature in real °C, a `capacityGrowth` of about 0.05 to 0.07 per °C matches the physics, and the constants can be checked against real figures. The Magnus formula (saturation vapor pressure of about `6.112 * exp(17.62 * T / (243.12 + T))` hPa, divided by its value at `referenceTemperature`) can replace the exponential for better accuracy near and below freezing. The closed forms in section 2.5 then become approximations, and the layer integral is evaluated numerically over about 8 slices.

### 2.3 Orographic lift through capacity

Orographic lift is the cause and falling capacity is the mechanism: air forced up a slope expands and cools, cooler air holds less vapor, relative humidity rises, and vapor condenses. The sweep therefore has no separate "uplift" term. Capacity is computed from the temperature of the air at its own elevation, which falls as the air rises (section 2.4), and condensation follows from that. On the far side the air descends, capacity rises, cloud evaporates, and the leeward slope is dry (the foehn effect). Adding a slope-based rain term on top would count the same process twice. A small explicit tuning knob for rain shadow strength is acceptable if the lapse rate at the map's elevation scale gives too little contrast.

### 2.4 The air parcel

The air does not sit on the ground. The sweep carries a **parcel** along each lane with its own elevation, separate from the ground beneath it. The parcel elevation is the **base of the moist layer** described in section 2.5.

**Parcel elevation.** Flow is deflected upward ahead of rising terrain and settles back gradually after the terrain drops. For each lane, with `ground[k]` the elevation of vertex `k` clamped to at least sea level:

```csharp
float ahead  = MaxWithinWindow(ground, k, lookaheadSteps);       // highest ground just ahead
float target = Lerp(ground[k], ahead, aheadWeight);              // air rises before the ridge

float rate = target > parcelElev
    ? PerStep(riseRate, stepLength)
    : PerStep(fallRate, stepLength);
parcelElev = Lerp(parcelElev, target, rate);
parcelElev = MathF.Max(parcelElev, ground[k]);                   // air cannot be below the surface
```

`lookaheadSteps` is `lookaheadTiles / stepLength`, rounded, at least 1. This produces rain building on the windward foothills before the crest, spillover past the crest, and a rain shadow that begins slightly behind the ridge instead of at a sharp line.

**Parcel temperature.** It uses only the map-wide sea-level temperature and the parcel's own elevation, with no noise term:

```csharp
float parcelTemp = seaLevelTemperature - lapseRate * (parcelElev - seaLevel);
float c0         = CapacityFromTemperature(parcelTemp);          // capacity at the base of the layer
```

`seaLevelTemperature` is a single constant for the whole map, the same base value the temperature field starts from. The map does not model a latitude gradient, so the only large-scale temperature variation is the cooling with elevation. Leaving out the noise term means local temperature noise on the ground cannot cause spurious condensation or evaporation in the air above it.

### 2.5 The moist layer

The model assumes that **total water (vapor plus cloud) per unit of air is uniform** over a layer above the parcel base. A rising air parcel keeps its total water until it saturates, and a well-mixed lower layer keeps its water roughly uniform with height, so the assumption is the standard basis for finding where air saturates.

**Capacity profile.** Capacity falls with height `z` above the base as `c0 * exp(-a * z)`, where `a = capacityGrowth * lapseRate` (per meter). At every height, vapor is the smaller of the total water `T` and the local capacity, and the remainder is cloud.

**Derived quantities.**

- **Saturation height** (the lifting condensation level, measured from the base): `zs = ln(c0 / T) / a` when `T < c0`, and 0 otherwise.
- **Onset humidity:** the relative humidity `T / c0` below which no part of the layer saturates. For a layer of depth `D` it is `exp(-a * D)`, so a deeper layer lowers it.
- **Mean cloud water** over the layer, from integrating `max(0, T - c0 * exp(-a z))` over `0..D`.

**The onset humidity is the setting** (`onsetHumidity`, shown to designers as the cloud formation humidity), not the layer depth. Across the layer the capacity falls by the factor `onsetHumidity`, whatever the elevation units, so with `k = -ln(onsetHumidity)` and heights measured as a share `u` of the layer, the cloud depends only on the humidity `h = T / c0` and `k`. No layer depth or elevation scale enters the partition. The depth in metres, `moistLayerDepth = k / a`, is derived and only used to express the cloud base and thickness in metres (section 2.9).

```csharp
(float cloudMean, float saturationShare, bool hasCloud) Partition(float T, float c0)
{
    float h = T / c0;
    if (h <= onsetHumidity) return (0f, 1f, false);                  // no part of the layer saturates
    if (k < 1e-6f) return (T - c0, 0f, true);                        // onset 1: cloud only above full saturation

    float u = h >= 1f ? 0f : MathF.Log(1f / h) / k;                  // share of the layer below the saturation height
    float cloudShare = h * (1f - u) - (MathF.Exp(-k * u) - MathF.Exp(-k)) / k;
    return (Math.Clamp(cloudShare * c0, 0f, T), u, true);
}
```

The saturation height in metres is `saturationShare * moistLayerDepth`. Near the onset the cloud grows with the square of the excess humidity, about `(h - onset)^2 / (2 h k)` of `c0`, so it starts smoothly with no jump.

The vapor in the layer is `T - cloudMean`. Cloud forms continuously as `T / c0` rises past the onset humidity, because the top of the layer saturates first and the saturated part grows downward.

**Scale check.** With a `capacityGrowth` of 0.065 per °C and a lapse rate of 6.5 °C per km, `a` is about 0.42 per km. A 0.5 km layer has an onset humidity of about 0.81 and a 1 km layer about 0.66. Air at 80% relative humidity has a saturation height of about 0.5 km, the same order as the meteorological rule of thumb of about 125 m of lift per °C of dew-point depression.

**Where the assumption holds and where it does not.** It holds in the well-mixed lower layer, typically the first kilometer or two over land, so sensible onset humidities are those giving a `moistLayerDepth` of roughly 0.5 to 1.5 km (an onset humidity of about 0.55 to 0.8 with the constants above). Above the mixed layer, vapor falls off much faster than air density, so a deep uniform layer overestimates the water aloft. Inversions and dry layers aloft are not represented. Section 2.9 describes what lies above the layer, and section 10 lists the limits.

### 2.6 Water conservation and the boundary condition

The sweep carries one state variable per lane, the **total water** `water` (capacity units, uniform over the layer). Vapor and cloud are not carried separately: they are derived from `water` and the layer's capacity by `Partition` at every step. Water exists in exactly three places, vapor, cloud, and precipitation on the ground, and each step only moves water between them. Because cloud is derived from the total, it condenses when capacity falls (rising air) and re-evaporates when capacity rises (descending air) with nothing leaking.

**Boundary.** Air enters at the first vertex of each lane with `water = boundaryHumidity * c0`, so `boundaryHumidity` is the relative humidity at the base of the column. The cloud at the boundary follows from the same partition. Air entering above the onset humidity carries cloud and rains from the first vertex with no dry fringe at the upwind edge. Air entering at or below the onset humidity carries no cloud. The boundary input counts all of the entering water, cloud included.

### 2.7 Precipitation

Cloud rains out as it travels, losing a constant fraction `rainFractionPerTile` per tile of travel. The fraction can be calibrated from a **cloud delay time**, the lag between condensation and fallout in orographic precipitation theory (Smith and Barstad):

```csharp
float rainFractionPerTile = 1f - MathF.Exp(-climateTileSizeMeters / (windSpeed * cloudDelaySeconds));
```

The characteristic fallout distance is `windSpeed * cloudDelaySeconds`. Reported delay times in the orographic precipitation literature range from a few hundred to about two thousand seconds. Because the cloud keeps traveling downwind, rainfall continues past the place where it condensed, and a longer delay or a faster wind moves the rain farther downwind, past the crest.

```csharp
float rain = cloudMean * PerStep(rainFractionPerTile, stepLength);
```

The fraction is the same for thin and thick cloud. An efficiency curve over absolute cloud amounts (thicker cloud raining more efficiently) would stop cold, high air from raining at all, because its cloud is small in absolute terms however saturated it is. If such a curve is ever wanted, it has to measure the cloud relative to `c0`.

### 2.8 Rain on flat ground

On flat ground the parcel elevation is constant, so `c0` is constant. Rain falls while the relative humidity `water / c0` is above the onset humidity: cloud forms in the upper layer, part of it rains out, `water` drops, and the humidity falls until it reaches the onset humidity. That gives a roughly exponential decay inland with a finite reach that depends on how far above onset the air entered.

Below the onset humidity there is no cloud, and only other mechanisms produce rain: cloud carried from upwind (for example from a ridge), evaporation from water surfaces, the optional background lift (section 5.1), and the optional non-depleting background rain (section 5.2). The ground's rain recycling (section 5.6) only gives back rain that fell, so it carries rain further inland but cannot start it. A flat map whose boundary humidity is at or below the onset humidity is dry unless one of these applies, which is intended and is one of the levers for desert presets.

### 2.9 Above the moist layer

The model ends at the top of the moist layer, `moistLayerDepth` above the layer base, and nothing above it is calculated. Cloud can therefore exist only inside the layer. Because the top of the layer saturates first, the cloud top is always the layer top (`cloudTopElevation = baseElev + moistLayerDepth`), and the cloud's depth is the part of the layer above the saturation height:

```csharp
float cloudThickness = hasCloud ? moistLayerDepth - satHeight : 0f;     // meters
```

`cloudThicknessV` is written at every vertex. It is 0 under clear sky and reaches `moistLayerDepth` when the base of the layer is saturated (fog or ground-level cloud).

**What this covers and what it leaves out.**

- **Orographic cloud is covered.** Lifting still works above the cutoff because the layer base rises and capacity falls. Thickness is capped by the layer depth, but rain is not, since most atmospheric water lies in the lowest kilometers.
- **Deep convective and frontal cloud are not represented**, which is the same gap as the missing convection on flat ground (section 2.8).
- **High ice cloud (cirrus) is not represented.**

**Why the uniform layer is not extended upward.** Ambient vapor falls off with height faster than air density, so air above the mixed layer is rarely saturated in place. Deep cloud mostly forms when air lifted from below keeps condensing as it rises, which is a parcel ascent process and not the saturation of a uniform layer.

**Extensions (not part of the base design).**

1. **An upper layer with decaying vapor.** With vapor falling off at a scale height `Hq` above the layer while capacity keeps falling at rate `a`, relative humidity above the top changes as `RH(z) = RH_top * exp(-(1 / Hq - a) * (z - D))`. With `a` of about 0.42 per km and `Hq` of about 2 km, humidity falls by only about 0.08 per km, so a saturated top would let cloud extend a few kilometers higher. If `1 / Hq` is at or below `a`, humidity never falls and an explicit cloud-top cap is needed. This is a back-of-envelope result that adds a parameter without much visible change at typical values.
2. **Lifted-parcel ascent for deep cloud.** Above the saturation height a saturated parcel keeps condensing, and the condensate at a height `h` above that level is about `T * (1 - exp(-aMoist * h))`, where `aMoist = capacityGrowth * moistLapseRate`, up to a cloud-top limit. This needs a trigger (for example a strong background lift, section 5.1) and a stability limit, and it would also supply the convective rain that flat ground lacks. It is the largest addition and is only worth adding if plains rainfall is too weak after tuning.

## 3. The sweep

### 3.1 Pseudocode

The sweep visits vertices upwind first, row by row. A vertex's upwind neighbour is always in the previous row, or earlier in the same row for an east or west wind, so the state carried downwind (the parcel elevation and the total water, nothing else) is kept for two rows only, not the whole map. Everything else (capacities, cloud, humidities) is derived at the vertex.

```csharp
void Sweep(int dx, int dy)
{
    var step = new WindStep(dx, dy);                              // length and per-step conversions (section 1.5)
    int lookaheadSteps = Math.Max(1, (int)MathF.Round(lookaheadTiles / step.Length));
    float rainShare = step.Share(rainFractionPerTile);

    foreach (int i in VerticesUpwindFirst(dx, dy))
    {
        bool hasUpwind = TryUpwind(i, out Carried up);           // from the previous row, or this row for east/west
        float ground = MathF.Max(terrainElevationV[i], seaLevel);

        // --- parcel and the base of the moist layer (section 2.4)
        float parcelElev = ParcelElevation(hasUpwind ? up.ParcelElev : ground, i, lookaheadSteps, step);
        float baseElev = parcelElev + backgroundLiftV[i];        // lift is 0 unless enabled (section 5.1)
        float c0 = CapacityFromTemperature(seaLevelTemperature - lapseRate * (baseElev - seaLevel));
        float groundCapacity = CapacityFromTemperature(temperatureV[i]);

        float water = hasUpwind ? up.Water : boundaryHumidity * c0;   // boundary condition (section 2.6)

        // --- water surfaces add vapor until the surface air is saturated
        if (isWaterV[i])
            water += MathF.Min(step.Amount(evaporationRate), MathF.Max(0f, groundCapacity - water));

        // --- saturation adjustment over the moist layer (section 2.5)
        var (cloud, satShare, hasCloud) = Partition(water, c0);
        float satHeight = satShare * moistLayerDepth;

        // --- precipitation (section 2.7)
        float rain = cloud * rainShare;
        water -= rain;

        // --- sub-cloud evaporation (section 5.3)
        float surfaceRH = Clamp01(water / groundCapacity);
        float back = rain * subCloudEvaporation * (1f - surfaceRH);
        water += back;

        // --- the ground gives back part of the rain that reached it (section 5.6)
        float recycled = MathF.Min(rainRecycling * (rain - back), MathF.Max(0f, groundCapacity - water));
        water += recycled;

        // --- fog and dew (section 4); both are 0 when disabled
        float baseHeightAboveGround = hasCloud ? baseElev + satHeight - ground : float.NaN;
        float fogIntensity = hasCloud ? 1f - SmoothStep(0f, fogLayerThickness, baseHeightAboveGround) : 0f;
        float cloudLeft = cloud - rain;
        float dew = cloudLeft * dewFactor * fogIntensity;
        water -= dew;

        // --- outputs
        surfaceHumidityV[i]    = Clamp01(water / groundCapacity);
        cloudWaterV[i]         = cloud;
        cloudBaseElevationV[i] = hasCloud ? baseElev + satHeight : float.NaN;
        cloudBaseHeightV[i]    = baseHeightAboveGround;
        cloudThicknessV[i]     = hasCloud ? moistLayerDepth - satHeight : 0f;
        fogV[i]                = (cloudLeft - dew) * fogFactor * fogIntensity;

        // --- deposits are recorded per tile of travel (section 1.5)
        precipitationV[i] += step.PerTile(rain - back + dew);
        precipitationV[i] += backgroundRainFraction * water;     // section 5.2; not conserving, 0 disables

        Carry(i, parcelElev, water);
    }
}
```

Precipitation uses `+=` so several wind directions can be blended with weights (section 5.7).

### 3.2 What the sweep produces

- Wet windward slopes and dry leeward slopes from capacity alone, with the parcel inertia placing both slightly ahead of and behind the crest.
- Leeward dryness by the foehn mechanism: descending air warms, capacity rises, and the cloud evaporates back to vapor.
- Inland decay while the humidity is above the onset humidity, stretched further inland the more of its rain the ground recycles.
- Little moisture carried by cold, high-elevation air, because of its low capacity.
- A cloud base height, a cloud thickness, and a surface humidity at every vertex (section 6).

### 3.3 Post-processing

- **Noise.** Add a small noise term so the pattern is not perfectly banded.
- **Lateral smoothing.** Lanes are independent, so a light blur perpendicular to the wind hides striping where neighboring lanes differ sharply.
- **Distance-to-water baseline.** A breadth-first distance transform from water vertices gives inland vertices near any water a small bonus.
- **Normalization** is described in section 6.3.

## 4. Fog, mist, and dew (optional layer)

Fog, mist, and dew all follow from the cloud base. They are only worth enabling after conservation and rain shadows have been verified.

- **Fog and mist** are cloud whose base is near the ground. `fogIntensity = 1 - SmoothStep(0, fogLayerThickness, cloudBaseHeight)`, and `fogV = cloud * fogFactor * fogIntensity`. A cloud base at or just above the ground is fog, and a base a few tens of meters up is low mist. It feeds a fog overlay in the renderer and cloud-forest logic in biomes.
- **Terrain inside cloud.** Ground higher than `cloudBaseElevationV` is inside the cloud, which marks cloud forests and peaks above the cloud deck.
- **Dew and fog drip** deposit part of the cloud onto the ground without rain: `dew = cloud * dewFactor * fogIntensity`, with `dewFactor` between 0 and 1. The deposit conserves water and can support biomes that depend on fog moisture where rain is scarce.
- **Cloud cover** for visuals or weather comes directly from `cloudWaterV`.

## 5. Optional refinements

### 5.1 Background lift

Add a low-frequency noise field `backgroundLiftV` (non-negative, in meters) to the parcel elevation used for the layer base: `baseElev = parcelElev + backgroundLiftV[i]`. It lowers capacity in patches, so lowlands get patchy, front-like rainfall where the ground provides no lift, and it can be scaled by surface warmth or wetness to imitate convection. Without it, flat ground rains only through the onset humidity rule (section 2.8). `backgroundLiftV` is 0 when disabled.

### 5.2 Non-depleting background rain

A cheap stand-in for local moisture recycling, in the style of mapgen4: a small fraction of the water in the air falls as rain without being removed, on the assumption that it evaporates again. `precipitationV[i] += backgroundRainFraction * water`. It does not conserve water by design, so the conservation test runs with it set to 0.

### 5.3 Sub-cloud evaporation

Part of the rain evaporates on its way down when surface air is dry, which produces cloudy but dry places (virga). It is part of the sweep and is controlled by `subCloudEvaporation`; 0 disables it. It conserves water by returning the evaporated amount to `water`.

### 5.4 Lee warming

Condensation releases heat, so air descending after rain is warmer than air that rose. Carry a decaying warmth value along each lane:

```csharp
carriedWarmth = carriedWarmth * warmthDecay + rain;
warmthV[i] = carriedWarmth;
```

After the sweep, apply it once: `temperatureV[i] += leeWarming * warmthV[i]`. Because it runs after the sweep and capacity does not read it back, it creates no feedback loop.

### 5.5 Temperature correction from moisture

Applied once, after moisture is final, on vertices:

```csharp
temperatureV[i] -= (temperatureV[i] - moderateTemperature) * moistureV[i] * dampingStrength;
```

Wet vertices move toward a mild temperature, and dry vertices stay more extreme.

### 5.6 Rain recycling by the ground

In reality land adds vapor too, which is why forested interiors stay wet, but only as much as its soil and plants hold, and that water comes from the rain. Open-water evaporation over all land would give every cell an unlimited source: dry air crossing a dry plain would moisten without limit and eventually rain everywhere.

Instead, the ground gives back `rainRecycling` of the rain that reached it, in the same step, capped at the room left in the surface air (`groundCapacity - water`). Dry air over dry land stays dry, and rain on coasts and windward slopes is carried further inland, each tile passing on part of what fell on it. It runs in a single pass with no circular dependency, and conserves water: the air loses the rain minus what is recycled. Water surfaces evaporate on their own through `isWaterV`; soil wetness and vegetation can later scale `rainRecycling` per cell.

### 5.7 Second sweep direction

Run a second sweep from a different wind direction, for example 90° away, at a random fraction of the first sweep's strength, and blend the two precipitation fields with weights (`secondWindWeight`). It adds variety to rainfall patterns and gives plains patchy rain from a second moisture source.

### 5.8 Alternative transport

If lane striping or the fixed single direction is unacceptable, replace the lane sweep with iterative advection of the water field along the wind (backward advection), with a small noise term added to the wind vector in each step, and a number of steps in the tens. It handles varying wind and removes the straight streamlines, at the cost of many more steps. The partition, precipitation, and output logic of sections 2 to 4 stay the same.

## 6. Outputs and downstream use

### 6.1 Output fields

| Output | Location | Meaning and use |
|---|---|---|
| `precipitationV` | Vertex | Raw rain deposited, in capacity units per tile of travel. Feeds the normalization below and, at the chosen site, the city map's annual precipitation |
| `moistureV` | Vertex | Normalized precipitation (0 to 1). Biome classification, soil and sand aridity, and other 0-to-1 weights |
| `temperatureV` | Vertex | Final temperature in °C, after optional corrections |
| `surfaceHumidityV` | Vertex | Relative humidity of the air at ground level, 0 to 1 (displayed as 0 to 100%). Crop stress, lake and canal evaporation, tooltips |
| `cloudBaseHeightV` | Vertex | Height of the cloud base above the ground, in meters. `NaN` where there is no cloud (clear sky) |
| `cloudBaseElevationV` | Vertex | Height of the cloud base above sea level, in meters. `NaN` where there is no cloud |
| `cloudThicknessV` | Vertex | Depth of the cloud in meters, from the cloud base to the top of the moist layer; 0 under clear sky. Capped at `moistLayerDepth` |
| `cloudWaterV`, `fogV` | Vertex | Mean cloud water and fog intensity. Rendering, weather visuals, cloud-forest and fog biomes |
| Tile versions (`VertexToTile`) | Tile | World-map biome queries, site selection, tooltips |

`surfaceHumidityV` is the total water divided by the capacity at the ground temperature, clamped to 1. Because the water is uniform in the layer, the vapor at ground level is the smaller of the two, so a value of 1 means the ground is inside the cloud. It uses the ground temperature (which includes elevation cooling and local noise), so cold ground reads as more humid.

### 6.2 Heatmaps for designers and players

Surface humidity and cloud-base height are everyday weather terms, so designers and players can both read them directly in the overlay framework that already shows temperature, precipitation, and the water table:

- **Surface humidity** shows the relative humidity at ground level as 0 to 100%. It reads like the humidity in a weather report: dry interiors show low values, coasts and windward slopes higher values, and fog and mist show as 100%.
- **Cloud-base height** shows how high above the ground the clouds sit, in meters. Clear sky is drawn as transparent or blank, fog is near 0, and a low ceiling over a valley or a high one over a plain is visible at a glance. The cloud-base elevation variant (above sea level) shows which peaks and ridges rise into or above the cloud.
- **Cloud thickness** shows how deep the cloud is, from its base to the top of the moist layer. Thick cloud marks windward slopes and saturated lowlands where rain is most likely, and thin cloud marks the edges of cloud. It is capped at the layer depth, so it is a relative indicator and not a measurement of real cloud depth.

Together they explain why a place is dry, foggy, or rainy: designers can see where cloud forms and how close to the ground it is, and players can read when a mountain is in cloud. For the legend, use units on both (percent, meters), draw `NaN` as clear, and clamp the cloud-base and cloud-thickness color ramps to `0..moistLayerDepth`. These are simulation values at the climate scale and not meteorological measurements.

### 6.3 Normalization and calibration

Normalized fields use **fixed reference scales**, never each map's own minimum and maximum, so a desert preset and a humid preset remain distinguishable:

```csharp
moistureV = Clamp01(precipitationV / precipitationReference);
temperature01 = Clamp01(InverseLerp(biomeTemperatureMin, biomeTemperatureMax, temperatureV));
```

`precipitationReference` is the rain per vertex that counts as fully wet, in the sweep's units (capacity units per tile of travel). To calibrate it, generate several seeds with the wettest intended preset, take a high percentile (for example the 95th to 99th) of `precipitationV` over land vertices, and freeze that value. Recalibrate it whenever the rain model changes (`onsetHumidity`, `rainFractionPerTile` or what calibrates it, `rainRecycling`, `capacityGrowth`). Climate inputs such as `boundaryHumidity` or `seaLevelTemperature` do not call for recalibration: a wetter or warmer preset is meant to read wetter.

Use the raw `precipitationV` for anything that moves water, because the clamp in `moistureV` discards how much rain fell in the wettest places. Use `moistureV` and `temperature01` for classification and other 0-to-1 weights.

### 6.4 Biome classification

Biome classification (a Whittaker-style 2D lookup on `temperature01` and `moistureV`) can run on vertices to give smooth weights that the splat shader interpolates across each tile. Tile-level queries use the converted tile values.

## 7. Parameters

All values are starting points to tune by eye against the debug heatmaps.

| Parameter | Role | Notes |
|---|---|---|
| `seaLevelTemperature` | Base temperature of the whole map at sea level, °C | Single constant; raise it for hot maps and lower it for cold ones |
| `lapseRate` | Elevation cooling, °C per meter | About 0.0065 in reality; raise it if terrain is vertically compressed |
| `capacityGrowth` | Exponential growth of capacity with temperature, per °C | About 0.05 to 0.07 |
| `referenceTemperature` | Temperature at which capacity is 1 | A fixed constant, 15 °C; fixes the capacity units |
| `onsetHumidity` | Base relative humidity above which part of the moist layer saturates (the cloud formation humidity) | About 0.55 to 0.8; `moistLayerDepth = -ln(onsetHumidity) / (capacityGrowth * lapseRate)` is a read-only derived value to show in debug UI |
| `boundaryHumidity` | Relative humidity of incoming air at the base of the column | The main wetness control |
| `evaporationRate` | Water added over water surfaces per tile of travel | Capped at saturation of the surface air |
| `rainRecycling` | Share of the rain the ground gives back to the air | Sets how far rain reaches inland; 0 lets rain soak away for good |
| `rainFractionPerTile` | Share of the cloud that rains per tile of travel | Can be calibrated from `windSpeed`, `cloudDelaySeconds` and `climateTileSizeMeters` (section 2.7); the delay is typically a few hundred to about 2000 s |
| `lookaheadTiles`, `aheadWeight` | How far ahead and how strongly the parcel rises before terrain | Position of windward rain and the start of the shadow |
| `riseRate`, `fallRate` | How quickly the parcel climbs and descends | `fallRate` slower than `riseRate` gives spillover past the crest |
| `subCloudEvaporation` | Share of falling rain that evaporates in dry air | 0 disables it |
| `backgroundLiftAmplitude` | Strength of patchy lift over flat ground | 0 disables it |
| `backgroundRainFraction` | Non-depleting background rain per tile of travel | 0 disables it |
| `secondWindWeight` | Blend weight of the second sweep direction | 0 disables it |
| `leeWarming`, `warmthDecay` | Strength and carry distance of lee warming | Optional |
| `dewFactor`, `fogFactor`, `fogLayerThickness` | Dew and fog strength, and how close to the ground the cloud base must be | Optional layer |
| `precipitationReference` | Fixed normalization scale for precipitation | Calibrated from the wettest preset, section 6.3 |
| `biomeTemperatureMin/Max` | Fixed range mapping °C to 0 to 1 for biomes | Same for every map |
| `noiseAmplitude` | Breaks up banding | Small |

The most influential knobs in practice are `seaLevelTemperature`, `lapseRate`, `capacityGrowth`, `boundaryHumidity`, `onsetHumidity`, `rainFractionPerTile`, `rainRecycling`, and `lookaheadTiles`.

## 8. Build and verification order

### 8.1 Order

1. Sea level, `isWaterV`, the two grids with their index helpers and converters, and the unit conventions.
2. Run after final erosion elevation, on the vertex grid.
3. Temperature field on vertices.
4. `CapacityFromTemperature` and `Partition`, with their unit tests (section 8.2), before any sweep.
5. Parcel elevation and the layer base for one wind direction.
6. Moisture sweep for that direction (water, cloud, precipitation).
7. **Heatmap temperature, precipitation, surface humidity, cloud-base height, and cloud water** in the debug visualizer before anything else.
8. Generalize to the 8 compass directions, with step-length scaling.
9. Calibrate `precipitationReference` and the biome temperature range, then the Whittaker biome lookup.
10. Presets, then the optional layers (sections 4 and 5).

### 8.2 Checks

- **Grid indexing (shear test):** fill a vertex field with its own `x` coordinate, convert to tile values, and confirm each tile reads the midpoint of its corners. Repeat with `y`.
- **Partition function:** cloud is 0 below the onset humidity and continuous across it, increases monotonically with total water, never exceeds the total water, scales with the units the water is measured in, gives `T - c0` at an onset of 1, and matches a numeric integration of the layer in thin slices.
- **Cloud base:** at a relative humidity of 1 the base sits at the base of the layer, and at 0.8 with the constants in section 2.5 it comes out near 0.5 km above the base, the same order as the dew-point rule of thumb.
- **Cloud thickness:** it is 0 where there is no cloud, equals `moistLayerDepth` when the base of the layer is saturated, and decreases as the saturation height rises.
- **Conservation (unit test, per lane):** boundary water plus the water added by evaporation (after the saturation cap) equals the water leaving the lane plus the sum of deposits, where each deposit is weighted by its step length because deposits are stored per tile of travel. Run it with `backgroundRainFraction` at 0, because that term does not conserve. Sub-cloud evaporation, dew and rain recycling conserve, so the balance must hold with them enabled (recycled water returns to the air and is subtracted from the deposits it came from), and evaporation from water surfaces counts as input.
- **No negative values** in water, cloud, or precipitation.
- **Determinism:** the same seed and settings give identical fields.
- **Uniform terrain:** on perfectly flat terrain with no temperature noise, moisture is identical across lanes and varies only along the wind direction.
- **Upwind edge:** with boundary humidity above the onset humidity, precipitation at the first vertex of a lane is already in line with the next few vertices, with no dry fringe.
- **Flat plain:** with boundary humidity above the onset humidity, moisture decays monotonically inland from the upwind edge. With boundary humidity at or below the onset humidity and no water surfaces or other rain source, a flat plain is dry whatever `rainRecycling` is.
- **Synthetic ridge:** a ridge row of vertices produces a wet windward side, rain beginning on the foothills before the crest, a dry leeward side that starts slightly behind the crest, and cloud base height dropping toward the windward slope and rising on the lee. If the contrast is too weak, raise `lapseRate` or `capacityGrowth` before adding any slope term.
- **Wind symmetry:** the same terrain under an axis-aligned and a diagonal wind gives similar area-averaged precipitation within a tolerance, which confirms the step-length scaling and the per-tile-of-travel deposits.
- **Presets:** the arid preset is mostly dry, with wetter spots near lakes, rivers, and windward highlands. If a desert map comes out green, lower `boundaryHumidity` or `rainRecycling` before changing the wind direction.
- **Wind arrow consistency:** the arrow in the map preview matches the dry side of the ridges.
- **Lane striping:** inspect the precipitation heatmap for visible lane lines, and apply the lateral smoothing if needed.

## 9. Runtime extension (later)

The same stores (total water with its vapor and cloud partition, and precipitation) carry over to a dynamic version:

- Run on the **slow tick**, together with the groundwater simulation, not on every simulation step.
- **Read last tick's values and write to a separate buffer.** A coupling that is always one tick delayed, with small coefficients and damping, cannot blow up or oscillate indefinitely. This is the same pattern as the surface water cellular automaton.
- Hook into the water simulation: `precipitationV` is converted with `VertexToTile`, scaled by a gameplay constant (`rainDepthScale`) to water depth, and falls into the surface water grid. Evaporation from lakes and canals feeds the water field.
- Seasonal wind reversal and temperature swings fit here, and the surface humidity and cloud-base heatmaps then update over the year.

## 10. Known limitations

- No latitude gradient in the base model, so temperature varies only with elevation and noise, and climate contrasts come from wind, moisture, and terrain. A world map spanning many degrees of latitude would add one to the sea-level temperature.
- One prevailing wind direction for the whole map, snapped to 8 directions. Arbitrary angles need ray walking, grid rotation, or iterative advection (section 5.8).
- Lanes are independent. Air cannot flow around a tall ridge or funnel through a gap, which overstates shadows behind narrow ranges and misses gap-driven rain. Lateral smoothing only hides the visible striping.
- Parcel inertia is a lookahead heuristic and not fluid dynamics. Its tuning depends on the map's elevation scale.
- The uniform total water assumption holds only in the lower mixed layer. There are no inversions, no dry layers aloft, and no vertical structure beyond one moist layer. Nothing is calculated above the layer, so cloud thickness is capped at `moistLayerDepth` and deep convective, frontal, and high ice cloud are absent (section 2.9).
- One lapse rate is used for both dry and saturated air. Saturated air cools more slowly in reality, so a saturated parcel runs slightly too cool.
- Cloud re-evaporates immediately when capacity rises. Falling precipitation does not drift sideways beyond the carrying of the cloud itself.
- Convection and fronts are not modeled. The background lift and the non-depleting background rain are stand-ins, and rain on flat ground below the onset humidity needs one of them.
- Cloud base uses the parcel temperature, and surface humidity uses the ground temperature including local noise, so the two can disagree slightly on noisy ground.
- The rain-out fraction depends on the choice of a climate tile size, a wind speed, and a delay time, which are tuning values and not measurements.
- No spatially varying wind, sea breezes, storms, or seasons in the static version.
- Soil and vegetation feedback is limited to one rain-recycling share for all land; letting soil wetness or vegetation scale it per cell is the next step.

## 11. Glossary

- **Vertex grid / tile grid:** corner-based storage with one more sample per axis than the tile grid, and the per-tile grid derived from it.
- **Capacity:** the maximum water vapor the air can hold at a given temperature, in capacity units where 1 is the capacity at the reference temperature.
- **Relative humidity:** vapor divided by capacity.
- **Clausius-Clapeyron relation:** the roughly exponential growth of capacity with temperature.
- **Magnus formula:** an empirical formula for saturation vapor pressure, usable in place of the simple exponential.
- **Parcel:** the unit of air carried along a lane, whose elevation is the base of the moist layer.
- **Lane:** one line of vertices along the wind direction, processed upwind to downwind.
- **Moist layer:** the layer above the parcel base in which total water is assumed uniform; its depth `moistLayerDepth` follows from the onset humidity.
- **Onset humidity:** the base relative humidity below which no part of the moist layer saturates; the setting from which the layer depth follows (`exp(-a * D)`).
- **Rain recycling:** the share of the rain the ground evaporates back into the air.
- **Saturation height (lifting condensation level):** the height above the layer base at which the layer first saturates.
- **Cloud-base height:** the height of the cloud base above the ground, in meters.
- **Cloud-base elevation:** the height of the cloud base above sea level, in meters.
- **Cloud thickness:** the depth of the cloud, from the cloud base to the top of the moist layer, in meters.
- **Surface humidity:** the relative humidity of the air at ground level, computed at the ground temperature and clamped to 100%.
- **Cloud delay time:** the lag between condensation and precipitation, used to set the rain-out distance.
- **Sea-level temperature:** the single base temperature applied across the whole map before elevation cooling and noise.
- **Lapse rate:** the rate at which temperature falls with elevation.
- **Orographic lift / rain shadow:** air forced up a slope cools and rains on the windward side, leaving the leeward side dry.
- **Foehn effect:** descending air warms and evaporates cloud, drying the leeward side.
- **Cloud water:** condensed liquid in the moist layer that has not yet fallen as rain.
- **Saturation adjustment:** partitioning the total water into vapor and cloud so that water is conserved.
- **Non-depleting background rain:** a small rain term proportional to the water content that does not remove water, standing in for local recycling.
- **Virga:** rain that evaporates before reaching the ground.
- **Boundary condition:** the assumed state of the air entering the map at its upwind edge.
- **Whittaker diagram:** an ecology chart classifying biomes by temperature and precipitation, used here as a 2D lookup.
