using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolAlgorithm.Utils
{
    public enum InitializationMethodType
    {
        Random,
        Greedy,
        Pools
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
        SwapCrossoverWeighted,
        CrossoverOverlayPools
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
    public struct PoolingInitializationParameters
    {
        public int unavailableGroundPoolCount;
        public int waterPoolCount;

        public double unavailableGroundMinArea;
        public double unavailableGroundMaxArea;
        public double waterMinArea;
        public double waterMaxArea;
        public double pathMinArea;
        public double pathMaxArea;
    }
}
