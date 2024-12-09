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

    }
}
