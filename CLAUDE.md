# Polis

## Architecture

- The game has a world map (coarse, for climate and site selection; not built yet) and a city map (10 m tiles, 2 m height steps), see section 3.0 of `hydrology-city-builder-design.md`. Algorithms both maps use (terrain noise, erosion, pit filling, flow routing, flow accumulation, rivers, drainage basins, biome lookup) must not assume the city map's scale: they work in cells and height steps, and anything needing metres takes a `GridScale` parameter instead of reading `CityScale` directly (`MapGenerator` passes `CityScale.Grid`).

- Folders and namespaces match: `Assets/Polis.Sim/Grids/` is `Polis.Sim.Grids`. Each system has its own folder (`Grids`, `Terrain`, `Climate`; `Polis.Unity` has `Rendering`, `CameraControl`), and a new system gets a new one (e.g. `Hydrology/`). The map pipeline's entry point (`MapGenerator`, `MapSettings`, `MapState`) and shared basics (`CityScale`, `SimRng`) stay in the root namespace. Under `Polis.Unity`, avoid namespaces named after Unity types (`Camera`, `Terrain`), which would shadow them.

## Testing conventions

- Test files map one to one to the classes they test and mirror their folders: `Foo` in `Assets/Polis.Sim/<Area>/Foo.cs` (namespace `Polis.Sim.<Area>`) is tested by `FooTests` in `Assets/Tests/EditMode/Sim/<Area>/FooTests.cs` (namespace `Polis.Sim.Tests.<Area>`); classes in `Assets/Polis.Unity` likewise in `Assets/Tests/EditMode/Unity` (assembly `Polis.Unity.Tests`). Sim tests must not reference `Polis.Unity`.
- Do not put tests for one class in another class's test file.
- Test properties and invariants, not the implementation's own formula: a test that recomputes the production expression (e.g. `1 - exp(-p / ref)`) passes whatever units or factors the code gets wrong. Prefer checks that would catch a wrong unit or scale, such as the same result for every direction, conserved totals, monotonicity, and known anchor values.
- Keep tests independent of generated noise: switch off the noise-driven inputs (or use a fixed, noiseless fixture) instead of relying on seeded values and small margins.

## Code comments

- Do not leave comments about things that were removed or moved (e.g. "X now lives on Y; edit it there"). Comments describe the code as it is; put change history and pointers in the commit message or your reply, not the source.

## Naming

- Name settings and other public fields for what a designer sees in the world, not for the algorithm behind them: say "air", "ridge", "rain", not "parcel", "lookahead", "knee" or "inertia". Someone who has never read the implementation should be able to tell what a setting does and which way to turn it from its name and summary alone.
- Prefix the settings of one mechanism with a shared word (`AirRiseRate`, `AirSinkRate`) so they group together in the inspector.

## Access modifiers

- Make a `Polis.Sim` type or member `public` only when `Polis.Unity` uses it or it appears in a public signature (e.g. a settings struct inside `MapSettings`); make pipeline internals such as generators and their helpers `internal`. `Polis.Sim.Tests` sees internals through `InternalsVisibleTo` in `Assets/Polis.Sim/AssemblyInfo.cs`, so never widen access just for a test.
- Keep domain logic in production code even if only tests use it so far; move code into the tests only when it just builds fixtures or eases assertions.
