using System;

namespace Polis.Sim
{
    /// <summary>The generated state of a map. Components that react to a new map read from this.</summary>
    public sealed class MapState
    {
        public HeightMap Heights { get; }

        /// <summary>Flatness in [0, 1] at every vertex; the generator lets less fine detail through where it is high.</summary>
        public FloatMap Flatness { get; }

        /// <summary>Temperature in degrees C at every vertex.</summary>
        public FloatMap Temperature { get; }

        /// <summary>Temperature index in [0, 1] at every vertex, from cold to hot: Temperature on a fixed scale.</summary>
        public FloatMap TemperatureIndex { get; }

        /// <summary>
        /// Precipitation at every vertex, in mm per year: the region's precipitation, the same across a city map.
        /// </summary>
        public FloatMap Precipitation { get; }

        /// <summary>Precipitation index in [0, 1] at every vertex, from dry to wet: Precipitation on a fixed scale.</summary>
        public FloatMap PrecipitationIndex { get; }

        /// <summary>
        /// Relative humidity in [0, 1] of the air at ground level at every vertex. Cold ground reads more humid than warm
        /// ground under the same air; 1 means the ground is in cloud or fog.
        /// </summary>
        public FloatMap SurfaceHumidity { get; }

        /// <summary>Map size in tiles.</summary>
        public MapSize Size => Heights.Size;

        /// <summary>Highest vertex height in the map, in height units.</summary>
        public int MaxHeight { get; }

        public MapState(HeightMap heights, FloatMap flatness, FloatMap temperature, FloatMap temperatureIndex, FloatMap precipitation, FloatMap precipitationIndex, FloatMap surfaceHumidity)
        {
            Heights = heights ?? throw new ArgumentNullException(nameof(heights));
            Flatness = flatness ?? throw new ArgumentNullException(nameof(flatness));
            Temperature = temperature ?? throw new ArgumentNullException(nameof(temperature));
            TemperatureIndex = temperatureIndex ?? throw new ArgumentNullException(nameof(temperatureIndex));
            Precipitation = precipitation ?? throw new ArgumentNullException(nameof(precipitation));
            PrecipitationIndex = precipitationIndex ?? throw new ArgumentNullException(nameof(precipitationIndex));
            SurfaceHumidity = surfaceHumidity ?? throw new ArgumentNullException(nameof(surfaceHumidity));
            foreach (int h in heights.Data)
                if (h > MaxHeight) MaxHeight = h;
        }
    }
}
