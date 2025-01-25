namespace EvolAlgorithm.Utils
{
    public static class Constants
    {
        public static readonly string[] TileDescriptions =
        {
            "Path",
            "Available_ground",
            "Unavailable_ground",
            "Water"
        };

        public static readonly List<(int, int)> Directions = new List<(int, int)>
        {
            (-1, 0), // left
            (1, 0),  // right
            (0, -1), // up
            (0, 1)  // down
        };

        public static readonly List<List<(int, int)>> LShapePatterns = new List<List<(int, int)>>
        {
            // Upright "L"
            new List<(int, int)>
            {
                (0, 0), (1, 0), (2, 0), (2, 1), (2, 2)
            },
            // Rotated 90° clockwise
            new List<(int, int)>
            {
                (0, 0), (0, 1), (0, 2), (1, 0), (2, 0)
            },
            // Rotated 180° clockwise
            new List<(int, int)>
            {
                (0, 0), (0, 1), (0, 2), (1, 2), (2, 2)
            },
            // Rotated 270° clockwise
            new List<(int, int)>
            {
                (0, 2), (1, 2), (2, 2), (2, 1), (2, 0)
            }
        };


        public const int PATH_TILE = 0;
        public const int AVAILABLE_GROUND_TILE = 1;
        public const int UNAVAILABLE_GROUND_TILE = 2;
        public const int WATER_TILE = 3;

        //  PENALTIES
        public const int AVAILABLE_ON_BORDER_SHIFT_PENALTY_WEIGHT = 10;
        public const int TILE_RATIOS_IMBALANCE_PENALTY_WEIGHT = 5;

        public const int EVALUATING_TILES_SHIFT_PENALTY_WEIGHT = 10;
        public const double FOUND_L_SHAPE_PROMOTION_WEIGHT = 1.0;
        public const double NO_L_SHAPE_PENALTY_WEIGHT = 0.0;

        public const double PENALTY_WEIGHT_WRONG_NUM_POOLS = 10.0;
        public const double PENALTY_WEIGHT_WRONG_POOL_AREAS = 5.0;

    }
}
