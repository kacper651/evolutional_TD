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
    }
    public enum MutationMethodType
    {
        Swap,
        Inverse,
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

        public const int PATH_TILE = 0;
        public const int AVAILABLE_GROUND_TILE = 1;
        public const int UNAVAILABLE_GROUND_TILE = 2;
        public const int WATER_TILE = 3;

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
                    string description = GetTileDescription(map[i, j]);
                    Console.Write(description.PadRight(20));
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

            for (int ky = -offset; ky < offset; ky++)
            {
                for (int kx = -offset; kx < offset; kx++)
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
    }
}
