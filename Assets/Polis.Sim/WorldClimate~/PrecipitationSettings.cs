using System;

namespace Polis.Sim
{
    /// <summary>Inputs to PrecipitationGenerator. Validated brings every value into its valid range.</summary>
    [Serializable]
    public struct PrecipitationSettings
    {
        /// <summary>
        /// The direction the prevailing wind comes from, so a West wind blows east. Air enters the map at that
        /// edge and crosses to the opposite one.
        /// </summary>
        public CompassDirection WindFrom;
        /// <summary>
        /// Relative humidity of the air entering the map at the upwind edge, measured like CloudFormationHumidity.
        /// Air that enters above CloudFormationHumidity already carries cloud and rains from the edge.
        /// </summary>
        public float IncomingAirMoisture;
        /// <summary>
        /// The share of the rain falling on the ground that it evaporates back into the air, through soil and plants.
        /// It carries moisture inland from where rain first falls: at 0 rain soaks away for good, and land far from
        /// the upwind edge and windward slopes stays dry; at 1 the air loses almost nothing to the ground. The ground
        /// cannot give back more than the air at ground level has room for.
        /// </summary>
        public float GroundRainRecycling;
        /// <summary>
        /// How fast the moisture air can hold grows with temperature, as a fraction per degree C. About 0.06 is the
        /// physical value; higher makes climbs and cold air dry the air out more.
        /// </summary>
        public float CapacityGrowth;
        /// <summary>
        /// The relative humidity above which cloud forms and lasts, measured at the bottom of the air lifted over the
        /// terrain. The air is colder higher up, so its top saturates first: above this value some of the air higher
        /// up holds cloud without being lifted further, more the higher the humidity, and below it there is none.
        /// Flat land only rains where the air is above this. Higher values mean a shallower body of moist air, which
        /// clouds up only when it is almost saturated.
        /// </summary>
        public float CloudFormationHumidity;
        /// <summary>The share of the cloud that falls as rain per tile of travel. Low values let moisture drift far past where it clouded up.</summary>
        public float CloudRainRate;
        /// <summary>
        /// How far ahead of a ridge, in tiles, the air starts to rise over it. Wind does not hug the ground: it is
        /// pushed up before it reaches high ground, so rain starts on the foothills. With 0 the air follows the
        /// ground exactly and the rain only starts where the ground does.
        /// </summary>
        public float AirAnticipationTiles;
        /// <summary>
        /// How far the air rises before a ridge, as a share of the way up to the highest ground within
        /// AirAnticipationTiles. 0 means the air ignores the ridge until it arrives.
        /// </summary>
        public float AirLiftBeforeRidge;
        /// <summary>How quickly the air climbs to follow rising ground, as the share of the remaining height closed per tile of travel. 1 follows at once.</summary>
        public float AirRiseRate;
        /// <summary>How quickly the air sinks back down after the ground drops, as the share of the remaining height closed per tile of travel. Low values keep the air high past the crest, so the dry rain shadow starts behind the ridge.</summary>
        public float AirSinkRate;
        /// <summary>
        /// Blur radius in tiles applied to the terrain before the wind passes over it and to the rain pattern after;
        /// smooths out the streaks and spots that single-tile bumps and independent wind lines would otherwise leave.
        /// </summary>
        public int RainShadowSmoothing;
        /// <summary>The lowest PrecipitationReference allowed; it is divided by, so it can never be 0.</summary>
        public const float MinPrecipitationReference = 1e-4f;
        /// <summary>
        /// The precipitation that gives a precipitation index of about 0.63; the scale is fixed, so wetter maps read wetter.
        /// Lower values make every map rainier, and heavy rain saturates toward 1.
        /// </summary>
        public float PrecipitationReference;

        public static PrecipitationSettings Default => new PrecipitationSettings
        {
            WindFrom = CompassDirection.West,
            IncomingAirMoisture = 0.75f,
            GroundRainRecycling = 0.5f,
            CapacityGrowth = 0.06f,
            CloudFormationHumidity = 0.8f,
            CloudRainRate = 0.3f,
            AirAnticipationTiles = 4f,
            AirLiftBeforeRidge = 0.5f,
            AirRiseRate = 0.5f,
            AirSinkRate = 0.2f,
            RainShadowSmoothing = 1,
            PrecipitationReference = 0.03f,
        };

        /// <summary>A copy with every value brought into its valid range; Generate does this itself.</summary>
        public PrecipitationSettings Validated()
        {
            var s = this;
            s.IncomingAirMoisture = Math.Clamp(s.IncomingAirMoisture, 0f, 1f);
            s.GroundRainRecycling = Math.Clamp(s.GroundRainRecycling, 0f, 1f);
            s.CapacityGrowth = Math.Max(0f, s.CapacityGrowth);
            s.CloudFormationHumidity = Math.Clamp(s.CloudFormationHumidity, 0f, 1f);
            s.CloudRainRate = Math.Clamp(s.CloudRainRate, 0f, 1f);
            s.AirAnticipationTiles = Math.Max(0f, s.AirAnticipationTiles);
            s.AirLiftBeforeRidge = Math.Clamp(s.AirLiftBeforeRidge, 0f, 1f);
            s.AirRiseRate = Math.Clamp(s.AirRiseRate, 0f, 1f);
            s.AirSinkRate = Math.Clamp(s.AirSinkRate, 0f, 1f);
            s.RainShadowSmoothing = Math.Max(0,s.RainShadowSmoothing);
            s.PrecipitationReference = Math.Max(MinPrecipitationReference, s.PrecipitationReference);
            return s;
        }
    }
}
