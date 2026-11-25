using System;

namespace Polis.Sim
{
    /// <summary>
    /// The air above one vertex as the wind carries it: the water it holds (vapor and cloud together) and the height it
    /// has been lifted to. Only these two travel downwind; everything else (how much the air can hold, its cloud, its
    /// humidity at ground level) follows from them and the vertex, so it is computed when asked for, never stored.
    ///
    /// The water is shared by the air at two temperatures. The lifted air is cold from its lifting, and how much of
    /// the water it can hold decides the cloud, and so the rain. The air at ground level has the ground's temperature,
    /// and how much of the water it can hold limits how much of the rain the ground can give back.
    ///
    /// Water is measured in units of what saturated air at ReferenceTemperature holds.
    /// </summary>
    internal readonly struct AirColumn
    {
        /// <summary>
        /// The temperature in degrees C at which air holds one unit of moisture, the unit all water is measured in.
        /// It is fixed rather than taken from the map's sea-level temperature, so a warmer map's air carries more
        /// water and rains more than a colder map's at the same humidity. At 15 degrees C, one unit is about 10 g of
        /// vapor per kg of air.
        /// </summary>
        internal const float ReferenceTemperature = 15f;

        // Keeps the relative humidity finite however cold the air gets.
        private const float MinCapacity = 1e-9f;
        // Keeps the capacity's fall through the lifted air finite when the cloud formation humidity is 0.
        private const float MinOnsetHumidity = 1e-3f;
        // Below this fall in capacity through the lifted air it is treated as constant, where cloud only forms above full saturation.
        private const float MinCapacityFall = 1e-6f;

        /// <summary>Vapor plus cloud, in units of what air at ReferenceTemperature can hold.</summary>
        public readonly float Water;
        /// <summary>The height the air has been lifted to, in height units; it sets how cold the lifted air is.</summary>
        public readonly float LiftedHeight;

        public AirColumn(float water, float liftedHeight)
        {
            Water = water;
            LiftedHeight = liftedHeight;
        }

        /// <summary>Air entering the map at the given relative humidity of the lifted air.</summary>
        public static AirColumn Entering(float liftedHeight, float liftedCapacity, float humidity) =>
            new AirColumn(humidity * liftedCapacity, liftedHeight);

        /// <summary>
        /// The moisture air at the given temperature can hold, relative to air at ReferenceTemperature (which holds 1).
        /// It grows exponentially with temperature, as real air does, by capacityGrowth per degree C.
        /// </summary>
        public static float CapacityAt(float temperature, float capacityGrowth) =>
            Math.Max(MinCapacity, (float)Math.Exp(capacityGrowth * (temperature - ReferenceTemperature)));

        /// <summary>The same water, lifted to a new height.</summary>
        public AirColumn LiftedTo(float liftedHeight) => new AirColumn(Water, liftedHeight);

        /// <summary>
        /// The column after the ground gives back the given water (rain it evaporates again), as much as the air at
        /// ground level has room for. Air saturated at ground level takes up nothing.
        /// </summary>
        public AirColumn Recycled(float groundCapacity, float recycled) =>
            new AirColumn(Water + Math.Min(recycled, Math.Max(0f, groundCapacity - Water)), LiftedHeight);

        /// <summary>The column after the given share of its cloud falls as rain; fallen is the water that fell.</summary>
        public AirColumn Rained(float liftedCapacity, float onsetHumidity, float rainShare, out float fallen)
        {
            fallen = CloudWater(liftedCapacity, onsetHumidity) * rainShare;
            return new AirColumn(Water - fallen, LiftedHeight);
        }

        /// <summary>The cloud in the lifted air (see CloudWaterOf).</summary>
        public float CloudWater(float liftedCapacity, float onsetHumidity) => CloudWaterOf(Water, liftedCapacity, onsetHumidity);

        /// <summary>Relative humidity of the air at ground level, in [0, 1]; 1 means the ground is in cloud or fog.</summary>
        public float SurfaceHumidity(float groundCapacity) => Math.Min(1f, Water / groundCapacity);

        /// <summary>
        /// The mean cloud water in the lifted air, in the units of the total water. The air is colder higher up, so it
        /// can hold less there, and the top saturates first while the bottom is still dry. The water is taken to be
        /// spread evenly through the lifted air, and the capacity to fall exponentially with height through it; where
        /// the water exceeds the capacity at some height, the excess is cloud.
        ///
        /// How deep the lifted air is shows only through the onset humidity: the humidity at its bottom below which
        /// none of it saturates, since the capacity falls by the factor 1 / onset from bottom to top whatever the
        /// units. The cloud is 0 up to the onset humidity, rises smoothly and without a jump beyond it, grows with the
        /// total water, and never exceeds it.
        /// </summary>
        /// <param name="totalWater">Vapor plus cloud, spread evenly through the lifted air.</param>
        /// <param name="baseCapacity">What the lifted air at its bottom can hold, in the same units (above 0).</param>
        /// <param name="onsetHumidity">Humidity at the bottom, from 0 to 1, below which there is no cloud.</param>
        public static float CloudWaterOf(float totalWater, float baseCapacity, float onsetHumidity)
        {
            float humidity = totalWater / baseCapacity;
            if (humidity <= onsetHumidity) return 0f;

            float capacityFall = -(float)Math.Log(Math.Max(onsetHumidity, MinOnsetHumidity));
            if (capacityFall < MinCapacityFall) return totalWater - baseCapacity;

            // Where the capacity profile e^(-capacityFall * height) crosses the humidity: below it the air is clear.
            float clearShare = humidity >= 1f ? 0f : (float)Math.Log(1f / humidity) / capacityFall;
            float cloudShare = humidity * (1f - clearShare)
                - ((float)Math.Exp(-capacityFall * clearShare) - (float)Math.Exp(-capacityFall)) / capacityFall;
            return Math.Clamp(cloudShare * baseCapacity, 0f, totalWater);
        }
    }
}
