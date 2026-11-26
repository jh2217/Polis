using System;

namespace Polis.Sim.Grids
{
    internal static class CompassDirectionExtensions
    {
        private const int DirectionCount = 8;
        private const float DiagonalStepLength = 1.41421356f;

        /// <summary>The direction pointing the opposite way.</summary>
        public static CompassDirection Opposite(this CompassDirection direction)
        {
            CheckDefined(direction);
            return (CompassDirection)(((int)direction + DirectionCount / 2) % DirectionCount);
        }

        /// <summary>The change in (x, y) of one step toward the direction, where x increases east and y increases north.</summary>
        public static (int dx, int dy) Step(this CompassDirection direction)
        {
            switch (direction)
            {
                case CompassDirection.East: return (1, 0);
                case CompassDirection.NorthEast: return (1, 1);
                case CompassDirection.North: return (0, 1);
                case CompassDirection.NorthWest: return (-1, 1);
                case CompassDirection.West: return (-1, 0);
                case CompassDirection.SouthWest: return (-1, -1);
                case CompassDirection.South: return (0, -1);
                case CompassDirection.SouthEast: return (1, -1);
                default: throw UndefinedDirection(direction);
            }
        }

        /// <summary>The distance one step toward the direction covers, in tiles: diagonal steps are longer.</summary>
        public static float StepLength(this CompassDirection direction)
        {
            var (dx, dy) = direction.Step();
            return dx != 0 && dy != 0 ? DiagonalStepLength : 1f;
        }

        private static void CheckDefined(CompassDirection direction)
        {
            if (!Enum.IsDefined(typeof(CompassDirection), direction))
                throw UndefinedDirection(direction);
        }

        private static ArgumentOutOfRangeException UndefinedDirection(CompassDirection direction) =>
            new ArgumentOutOfRangeException(nameof(direction), direction, "Not a defined compass direction.");
    }
}
