# World climate (parked)

The wind-sweep climate model, meant for a future world map whose tiles stand for far more ground than the city
map's. It carries air across the map along the wind, lifts it over terrain, forms cloud, rains it out and lets the
ground give part of the rain back, giving rain shadows and coast-to-interior gradients
(see `rainfall-simulation-sweep-design.md` at the repository root).

Unity ignores folders whose name ends in `~`, so nothing here is compiled or tested. To use it again, rename this
folder and `Assets/Tests/EditMode/Sim/WorldClimate~` (its tests) without the `~` and adapt the code to the rest of
`Polis.Sim`.

Contents:
- `PrecipitationGenerator.cs`: the sweep, with `WindStep` (per-step rates) and `AirLiftModel` (how high the air has
  been lifted).
- `PrecipitationSettings.cs`: its settings.
- `AirColumn.cs`: the air above one vertex and how much of its water is cloud.
- `TemperatureGenerator.cs`, `TemperatureSettings.cs`: the temperature model the sweep was built against (noise plus
  cooling to `MaxHeight`), which the city map no longer uses.
