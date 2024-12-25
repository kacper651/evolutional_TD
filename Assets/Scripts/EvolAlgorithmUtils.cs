using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolutionaryAlgorithmUtils
{
    public enum InitializationMethodType
    {
        Random,
        Greedy
    }
    public enum SelectionMethodType
    {
        Tournament,
        Roulette
    }
    public enum CrossoverMethodType
    {
        OrderedCrossover,
        CycleCrossover,
        SwapCrossoverWeighted
    }
    public enum MutationMethodType
    {
        Swap,
        Inverse,
        Random
    }
    public enum RepairMethodType
    {
        Whatever1,
        Whatever2,
    }
    public enum CreatingNewGenerationType
    {
        Elitism,
        NoElitism
    }

    public struct MapParameters
    {
        public int Width;
        public int Height;
        public int KernelSize;
    }
    public struct StopConditionParameters
    {
        public int MaxGenerations;
        public int TimeLimit;
    }
    public struct AlgorithmParameters
    {
        public int PopulationSize;
        public double MutationRate;
        public int TournamentSize;
        public double ElitesPercentage;
    }
    public struct MethodSelectionParameters
    {
        public InitializationMethodType InitializationMethod;
        public SelectionMethodType SelectionMethod;
        public CrossoverMethodType CrossoverMethod;
        public MutationMethodType MutationMethod;
        public RepairMethodType RepairMethod;
        public CreatingNewGenerationType CreatorMethod;
    }

    public static class MapUtils
    {
        public static readonly string[] TileDescriptions =
        {
            "Path",
            "Available_ground",
            "Unavailable_ground",
            "Water"
        };

        private static readonly List<(int, int)> Directions = new List<(int, int)>
        {
            (-1, 0), // left
            (1, 0),  // right
            (0, -1), // up
            (0, 1)  // down
            //(-1, 1), // leftUp
            //(1, 1),  // rightUp
            //(-1, -1), // leftDown
            //(1, -1)  // rightDown
        };

        private static readonly List<List<(int, int)>> LShapePatterns = new List<List<(int, int)>>
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

        public const int AVAILABLE_ON_BORDER_SHIFT_PENALTY_WEIGHT = 10;
        public const int TILE_RATIOS_IMBALANCE_PENALTY_WEIGHT = 5;
        public const int EVALUATING_TILES_SHIFT_PENALTY_WEIGHT = 10;
        public const double FOUND_L_SHAPE_PROMOTION_WEIGHT = 1.0;
        public const double NO_L_SHAPE_PENALTY_WEIGHT = 0.0;

        public static string GetTileDescription(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= TileDescriptions.Length)
                throw new ArgumentOutOfRangeException(nameof(tileIndex), "Invalid tile index");

            return TileDescriptions[tileIndex];
        }

        public static void PrintMap(int[,] map)
        {
            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    //string description = GetTileDescription(map[i, j]);
                    string description = map[i, j].ToString();
                    Console.Write(description.PadRight(5));
                }
                Console.WriteLine();
            }
        }

        public static (int, int) FindValuePosition(int[,] matrix, int value)
        {
            for (int y = 0; y < matrix.GetLength(0); y++)
            {
                for (int x = 0; x < matrix.GetLength(1); x++)
                {
                    if (matrix[y, x] == value)
                    {
                        return (y, x);
                    }
                }
            }
            throw new InvalidOperationException("Value not found in the matrix.");
        }

        public static int[,] ExtractKernelRegion(int[,] matrix, int anchorX, int anchorY, int kernelSize)
        {
            int offset = kernelSize / 2;
            int[,] kernelRegion = new int[kernelSize, kernelSize];

            for (int ky = -offset; ky <= offset; ky++)
            {
                for (int kx = -offset; kx <= offset; kx++)
                {
                    int x = anchorX + kx;
                    int y = anchorY + ky;

                    if (x < 0 || x >= matrix.GetLength(1) || y < 0 || y >= matrix.GetLength(0))
                    {
                        kernelRegion[ky + offset, kx + offset] = -1;
                    }
                    else
                    {
                        kernelRegion[ky + offset, kx + offset] = matrix[y, x];
                    }
                }
            }

            return kernelRegion;
        }

        public static bool IsSoloCell(int[,] region, int x, int y, int value)
        {
            foreach (var (dx, dy) in Directions)
            {
                int newX = x + dx;
                int newY = y + dy;

                if (newX >= 0 && newX < region.GetLength(1) && newY >= 0 && newY < region.GetLength(0))
                {
                    if (region[newY, newX] == value)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool IsConnectedToOtherPathTiles(int[,] region, int x, int y)
        {
            foreach (var (dx, dy) in Directions)
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

        public static int CountTilesOfTypeInRegion(int[,] region, int wantedValue)
        {
            int count = 0;
            for (int y = 0; y < region.GetLength(0); y++)
            {
                for (int x = 0; x < region.GetLength(1); x++)
                {
                    if (region[y, x] == wantedValue)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public static double WeighTileRatios(double r1, double r2, double r3)
        {
            //// Ideal value for all ratios
            //double ideal = 0.33;

            // Calculate variance
            double mean = (r1 + r2 + r3) / 3.0;
            double variance = Math.Pow(r1 - mean, 2) + Math.Pow(r2 - mean, 2) + Math.Pow(r3 - mean, 2);

            // Additional penalty for having one value significantly larger than the others
            double maxRatio = Math.Max(r1, Math.Max(r2, r3));
            double imbalancePenalty = maxRatio - mean; // Penalizes large differences

            // Weight the penalty and combine with variance
            double score = 1.0 / (1.0 + variance + TILE_RATIOS_IMBALANCE_PENALTY_WEIGHT * imbalancePenalty); // Lower variance means better, invert it to make larger score better

            return score * MapUtils.EVALUATING_TILES_SHIFT_PENALTY_WEIGHT;
        }

        public static bool IsLShape(int[,] matrix, int startRow, int startCol)
        {
            foreach (var pattern in LShapePatterns)
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

        public static (int startX, int startY, int endX, int endY) GetRandomStartEndCoordinates(int MapWidth, int MapHeight)
        {
            Random random = new();
            // Sides of the rectangle: 0 = top, 1 = bottom, 2 = left, 3 = right
            int startSide = random.Next(4);
            int endSide;

            do
            {
                endSide = random.Next(4);
            } while (endSide == startSide);

            Console.WriteLine("Start side: {0} | End side: {1}", startSide, endSide);

            int startX, startY;
            switch (startSide)
            {
                case 0: // Top
                    startX = random.Next(1, MapWidth - 2);
                    startY = 0;
                    break;
                case 1: // Bottom
                    startX = random.Next(1, MapWidth - 2);
                    startY = MapHeight - 1;
                    break;
                case 2: // Left
                    startX = 0;
                    startY = random.Next(1, MapHeight - 2);
                    break;
                case 3: // Right
                    startX = MapWidth - 1;
                    startY = random.Next(1, MapHeight - 2);
                    break;
                default:
                    throw new InvalidOperationException("Invalid start side");
            }

            int endX, endY;
            switch (endSide)
            {
                case 0: // Top
                    endX = random.Next(1, MapWidth - 2);
                    endY = 0;
                    break;
                case 1: // Bottom
                    endX = random.Next(MapWidth);
                    endY = MapHeight - 1;
                    break;
                case 2: // Left
                    endX = 0;
                    endY = random.Next(1, MapHeight - 2);
                    break;
                case 3: // Right
                    endX = MapWidth - 1;
                    endY = random.Next(1, MapHeight - 2);
                    break;
                default:
                    throw new InvalidOperationException("Invalid end side");
            }

            return (startX, startY, endX, endY);
        }

        public static List<(int, int)> GenerateTrail(int startX, int startY, int endX, int endY, int MapWidth, int MapHeight)
        {
            List<(int, int)> trail = new();
            Random random = new();

            int currentX = startX;
            int currentY = startY;

            trail.Add((currentX, currentY));

            //Console.WriteLine($"\nStart: ({currentX}, {currentY}), End: ({endX}, {endY})\n");

            while (currentX != endX || currentY != endY)
            {
                List<(int dx, int dy)> moves = new();
                if (currentX < endX) moves.Add((1, 0)); // Move right
                if (currentX > endX) moves.Add((-1, 0)); // Move left
                if (currentY < endY) moves.Add((0, 1)); // Move down
                if (currentY > endY) moves.Add((0, -1)); // Move up

                if (random.NextDouble() < 0.3)
                {
                    if (currentY > 1) moves.Add((0, -1)); // Move up
                    if (currentY < MapHeight - 2) moves.Add((0, 1)); // Move down
                    if (currentX > 1) moves.Add((-1, 0)); // Move left
                    if (currentX < MapWidth - 2) moves.Add((1, 0)); // Move right
                }

                if (moves.Count == 0)
                {
                    break;
                }

                var (dx, dy) = moves[random.Next(moves.Count)];

                currentX += dx;
                currentY += dy;

                currentX = Math.Clamp(currentX, 0, MapWidth - 1);
                currentY = Math.Clamp(currentY, 0, MapHeight - 1);

                if (!trail.Contains((currentX, currentY)))
                {
                    trail.Add((currentX, currentY));
                }
            }

            return trail;
        }

        public static ((int x1, int y1), (int x2, int y2)) SelectTwoWeightedRandom(double[,] fitnessMatrix)
        {
            Random random = new();
            List<(int x, int y, double weight)> weightedElements = new();
            double totalWeight = 0.0;

            for (int y = 0; y < fitnessMatrix.GetLength(0); y++)
            {
                for (int x = 0; x < fitnessMatrix.GetLength(1); x++)
                {
                    double fitness = fitnessMatrix[y, x];
                    double weight = fitness <= 0 ? 0.0 : (1.0 / fitness);
                    weightedElements.Add((x, y, weight));
                    totalWeight += weight;
                }
            }

            (int x, int y) SelectRandomElement()
            {
                double randomValue = random.NextDouble() * totalWeight;
                double currentWeight = 0.0;

                foreach (var element in weightedElements)
                {
                    currentWeight += element.weight;
                    if (currentWeight >= randomValue)
                    {
                        return (element.x, element.y);
                    }
                }
                return (weightedElements.Last().x, weightedElements.Last().y);
            }

            var firstElement = SelectRandomElement();
            var secondElement = SelectRandomElement();

            return (firstElement, secondElement);
        }

        public static void InsertKernelRegion(int[,] map, int centerX, int centerY, int[,] kernelRegion)
        {
            int kernelSize = kernelRegion.GetLength(0);
            int offset = kernelSize / 2;

            for (int ky = -offset; ky <= offset; ky++)
            {
                for (int kx = -offset; kx <= offset; kx++)
                {
                    int x = centerX + kx;
                    int y = centerY + ky;

                    if (x >= 0 && x < map.GetLength(1) && y >= 0 && y < map.GetLength(0) && kernelRegion[ky + offset, kx + offset] != -1)
                    {
                        map[y, x] = kernelRegion[ky + offset, kx + offset];
                    }
                }
            }
        }

        public static (int x, int y) SelectIndexWeightedPromoteLowFitness(double[,] fitnessMatrix)
        {
            Random random = new();
            var flatFitness = fitnessMatrix.Cast<double>().ToArray();
            double totalFitness = flatFitness.Sum();
            var probabilities = flatFitness.Select(fitness => 1.0 - (fitness / totalFitness)).ToArray();
            double randomValue = random.NextDouble() * probabilities.Sum();

            double cumulativeProbability = 0;
            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulativeProbability += probabilities[i];
                if (randomValue <= cumulativeProbability)
                {
                    int x = i % fitnessMatrix.GetLength(1);
                    int y = i / fitnessMatrix.GetLength(0);
                    return (x, y);
                }
            }
            return (0, 0); // Fallback, should not reach here
        }

        public static (int x, int y) SelectIndexWeightedPromoteHighFitness(double[,] fitnessMatrix)
        {
            Random random = new();
            var flatFitness = fitnessMatrix.Cast<double>().ToArray();
            double totalFitness = flatFitness.Sum();
            var probabilities = flatFitness.Select(fitness => fitness / totalFitness).ToArray();
            double randomValue = random.NextDouble() * probabilities.Sum();

            double cumulativeProbability = 0;
            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulativeProbability += probabilities[i];
                if (randomValue <= cumulativeProbability)
                {
                    int x = i % fitnessMatrix.GetLength(1);
                    int y = i / fitnessMatrix.GetLength(0);
                    return (x, y);
                }
            }
            return (0, 0); // Fallback, should not reach here
        }
    }
}
