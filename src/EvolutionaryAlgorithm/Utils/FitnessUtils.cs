using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolAlgorithm.Utils
{
    public static class FitnessUtils
    {
        public static double EvaluatePathConnectivity(int[,] region)
        {
            List<bool> connectedPaths = new();
            for (int y = 0; y < region.GetLength(0); y++)
            {
                for (int x = 0; x < region.GetLength(1); x++)
                {
                    if (region[y, x] == Constants.PATH_TILE)
                    {
                        connectedPaths.Add(IsConnectedToOtherPathTiles(region, x, y));
                    }
                }
            }
            //Console.WriteLine("EvaluatePathConn\tadded score:\t{0}", connectedPaths.Count(x => x));
            return connectedPaths.Count(x => x);
        }

        public static bool IsConnectedToOtherPathTiles(int[,] region, int x, int y)
        {
            foreach (var (dx, dy) in Constants.Directions)
            {
                int newX = x + dx;
                int newY = y + dy;
                if (newX >= 0 && newX < region.GetLength(1) && newY >= 0 && newY < region.GetLength(0))
                {
                    if (region[newY, newX] == 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static double PromoteL_Shape(int[,] region)
        {
            return ContainsLShape(region) ? Constants.FOUND_L_SHAPE_PROMOTION_WEIGHT : Constants.NO_L_SHAPE_PENALTY_WEIGHT;
        }

        public static bool IsLShape(int[,] matrix, int startRow, int startCol)
        {
            foreach (var pattern in Constants.LShapePatterns)
            {
                bool matches = true;

                foreach (var (rowOffset, colOffset) in pattern)
                {
                    int row = startRow + rowOffset;
                    int col = startCol + colOffset;

                    // Check if within bounds and if the cell is zero
                    if (row < 0 || row >= matrix.GetLength(0) || col < 0 || col >= matrix.GetLength(1) || matrix[row, col] != 0)
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    return true; // Match found for this orientation
                }
            }

            return false; // No match for any orientation
        }

        public static bool ContainsLShape(int[,] matrix)
        {
            for (int y = 0; y < matrix.GetLength(0); y++)
            {
                for (int x = 0; x < matrix.GetLength(1); x++)
                {
                    if (matrix[y, x] == 0 && IsLShape(matrix, y, x))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // weights for each ratio can be adjusted
        public static (double, int) EvaluateTileRatios(int[,] map)    // 10 = perfect, even ratio
        {
            int MapWidth = map.GetLength(1);
            int MapHeight = map.GetLength(0);

            int totalTiles = MapWidth * MapHeight;
            int pathTilesCount = 0;
            int availableGroundTilesCount = 0;
            int unavailableGroundTilesCount = 0;
            int waterTilesCount = 0;

            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    switch (map[y, x])
                    {
                        case Constants.PATH_TILE:
                            pathTilesCount++;
                            break;
                        case Constants.AVAILABLE_GROUND_TILE:
                            availableGroundTilesCount++;
                            break;
                        case Constants.UNAVAILABLE_GROUND_TILE:
                            unavailableGroundTilesCount++;
                            break;
                        case Constants.WATER_TILE:
                            waterTilesCount++;
                            break;
                    }
                }
            }
            double pathToTotalRatio = (double)pathTilesCount / totalTiles;
            double availableToTotalRatio = (double)availableGroundTilesCount / totalTiles;
            double unavailableToTotalRatio = (double)unavailableGroundTilesCount / totalTiles;
            double waterToTotalRatio = (double)waterTilesCount / totalTiles;

            double combinedRatio = WeighTileRatios(pathToTotalRatio, availableToTotalRatio, unavailableToTotalRatio, waterToTotalRatio);
            //Console.WriteLine("EvaluateTileRatios\tadded score:\t{0}", combinedRatio);

            return (combinedRatio, availableGroundTilesCount);
        }

        // maybe in regards to available tiles ratio?  
        // availableTilesOnBorder is one thing availableTiles in general is another
        // we can have 100 on the border, if there are 500 in general, but
        // if 450 out of 500 are on the border, it's an issue
        // i can get the total number of available tiles from EvaluateTileRatios method
        public static double PenalizeAvailableRegionsOnTheBorder(int[,] matrix, int totalAvailableCount, int borderSize) // penalty=10 - worst case, all available tiles on the border
        {
            int MapWidth = matrix.GetLength(1);
            int MapHeight = matrix.GetLength(0);
            int pathTilesCount = 0;

            // top border (+ corners)
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < borderSize; y++)
                {
                    if (matrix[y, x] == Constants.AVAILABLE_GROUND_TILE)
                    {
                        pathTilesCount++;
                    }
                }
            }

            // bottom border (+ corners)
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = MapHeight - borderSize; y < MapHeight; y++)
                {
                    if (matrix[y, x] == Constants.AVAILABLE_GROUND_TILE)
                    {
                        pathTilesCount++;
                    }
                }
            }

            // left border
            for (int x = 0; x < borderSize; x++)
            {
                for (int y = borderSize; y < MapHeight - borderSize; y++)
                {
                    if (matrix[y, x] == Constants.AVAILABLE_GROUND_TILE)
                    {
                        pathTilesCount++;
                    }
                }
            }

            // right border
            for (int x = MapWidth - borderSize; x < MapWidth; x++)
            {
                for (int y = borderSize; y < MapHeight - borderSize; y++)
                {
                    if (matrix[y, x] == Constants.AVAILABLE_GROUND_TILE)
                    {
                        pathTilesCount++;
                    }
                }
            }

            var ratio = (double)pathTilesCount / totalAvailableCount * Constants.AVAILABLE_ON_BORDER_SHIFT_PENALTY_WEIGHT;
            //Console.WriteLine("PenalizeAvRegOnBor\tpenalty score:\t{0}", ratio);
            return ratio;
        }

        public static double PenalizeSoloTiles(int[,] region)
        {
            double fitnessPenalty = 0.0;

            for (int y = 0; y < region.GetLength(0); y++)
            {
                for (int x = 0; x < region.GetLength(1); x++)
                {
                    int value = region[y, x];
                    if (value != -1 && Utils.IsSoloCell(region, x, y, value))
                    {
                        fitnessPenalty += 1.0;
                    }
                }
            }
            //Console.WriteLine("PenalizeSoloTiles\tpenalty score:\t{0}", fitnessPenalty - 2.0);
            return fitnessPenalty;
        }

        public static double PenalizeTooManyInOutPaths(int[,] matrix)
        {
            int MapWidth = matrix.GetLength(1);
            int MapHeight = matrix.GetLength(0);
            double pathsOnBorder = 0.0;

            // top border (+ corners)
            for (int x = 0; x < MapWidth; x++)
            {
                if (matrix[0, x] == Constants.PATH_TILE)
                {
                    pathsOnBorder += 1.0;
                }
            }

            // bottom border (+ corners)
            for (int x = 0; x < MapWidth; x++)
            {
                if (matrix[MapHeight - 1, x] == Constants.PATH_TILE)
                {
                    pathsOnBorder += 1.0;
                }
            }

            // left border
            for (int y = 1; y < MapHeight - 1; y++)
            {
                if (matrix[y, 0] == Constants.PATH_TILE)
                {
                    pathsOnBorder += 1.0;
                }
            }

            // right border
            for (int y = 1; y < MapHeight - 1; y++)
            {
                if (matrix[y, MapWidth - 1] == Constants.PATH_TILE)
                {
                    pathsOnBorder += 1.0;
                }
            }
            //Console.WriteLine("PenalizeSoloTiles\tpenalty score:\t{0}", pathsOnBorder-2.0);
            return pathsOnBorder - 2.0;
        }

        public static double PenalizeIncorrectPooling(
            int[,] matrix,
            int UnavailableGroundPoolCount,
            int WaterPoolCount,
            double UnavailableGroundMinPoolArea,
            double UnavailableGroundMaxPoolArea,
            double WaterMinPoolArea,
            double WaterMaxPoolArea,
            double PathMinArea,
            double PathMaxArea
            )
        {
            int MapWidth = matrix.GetLength(1);
            int MapHeight = matrix.GetLength(0);

            double penalty = 0.0;

            List<List<(int x, int y)>> trail = Utils.ExtractAllPools(matrix, Constants.PATH_TILE);
            List<List<(int x, int y)>> unavailableGroundPools = Utils.ExtractAllPools(matrix, Constants.UNAVAILABLE_GROUND_TILE);
            List<List<(int x, int y)>> waterPools = Utils.ExtractAllPools(matrix, Constants.WATER_TILE);

            if (unavailableGroundPools.Count != UnavailableGroundPoolCount)
            {
                penalty += Constants.PENALTY_WEIGHT_WRONG_NUM_POOLS;
            }
            if (waterPools.Count != WaterPoolCount)
            {
                penalty += Constants.PENALTY_WEIGHT_WRONG_NUM_POOLS;
            }
            if (trail.Count != 1)
            {
                penalty += Constants.PENALTY_WEIGHT_WRONG_NUM_POOLS;
            }

            //penalize pools with areas outside of acceptable range
            var unavailableGroundArea = 0;
            var waterArea = 0;
            var pathArea = 0;

            foreach (var pool in unavailableGroundPools)
            {
                unavailableGroundArea += pool.Count;
            }
            foreach (var pool in waterPools)
            {
                waterArea += pool.Count;
            }
            foreach (var pool in trail)
            {
                pathArea += pool.Count;
            }

            if (unavailableGroundArea < UnavailableGroundMinPoolArea * MapWidth * MapHeight || unavailableGroundArea > UnavailableGroundMaxPoolArea * MapWidth * MapHeight)
            {
                penalty += Constants.PENALTY_WEIGHT_WRONG_POOL_AREAS;
            }
            if (waterArea < WaterMinPoolArea * MapWidth * MapHeight || waterArea > WaterMaxPoolArea + MapWidth * MapHeight)
            {
                penalty += Constants.PENALTY_WEIGHT_WRONG_POOL_AREAS;
            }
            if (pathArea < PathMinArea * MapWidth * MapHeight || PathMaxArea > 0.15 * MapWidth * MapHeight)
            {
                penalty += Constants.PENALTY_WEIGHT_WRONG_POOL_AREAS;
            }
            //Console.WriteLine("\nPenalizeIncorrectPooling\tpenalty score:\t{0}", penalty);
            //Console.WriteLine("Expected Unavailable Ground Pools: {0}, Found: {1}", UnavailableGroundPoolCount, unavailableGroundPools.Count);
            //Console.WriteLine("Expected Water Pools: {0}, Found: {1}", WaterPoolCount, waterPools.Count);
            //Console.WriteLine("Expected Path Pools: 1, Found: {0}", trail.Count);
            return penalty;
        }

        public static double WeighTileRatios(double ratioPath, double ratioAvailableGround, double ratioUnavailableGround, double ratioWater)
        {
            //// Ideal value for all ratios
            //double ideal = 0.33;

            // Calculate variance
            double mean = (ratioPath + ratioAvailableGround + ratioUnavailableGround + ratioWater) / 4.0;
            double variance = Math.Pow(ratioPath - mean, 2) + Math.Pow(ratioAvailableGround - mean, 2) + Math.Pow(ratioUnavailableGround - mean, 2) + Math.Pow(ratioWater - mean, 2);

            // Additional penalty for having one value significantly larger than the others
            double maxRatio = Math.Max(ratioPath, Math.Max(ratioAvailableGround, Math.Max(ratioUnavailableGround, ratioWater)));
            double imbalancePenalty = maxRatio - mean; // Penalizes large differences

            // Weight the penalty and combine with variance
            double score = 1.0 / (1.0 + variance + Constants.TILE_RATIOS_IMBALANCE_PENALTY_WEIGHT * imbalancePenalty); // Lower variance means better, invert it to make larger score better

            return score * Constants.EVALUATING_TILES_SHIFT_PENALTY_WEIGHT;
        }
    }
}
