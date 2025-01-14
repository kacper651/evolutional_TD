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

        public MapParameters(int width, int height, int kernelSize)
        {
            // Rule 1: Width and height must be greater than 0
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width and height must be greater than 0.");
            
            // Rule 2: Kernel size must be greater than 0
            if (kernelSize <= 0)
                throw new ArgumentException("Kernel size must be greater than 0.");

            // Rule 3: Kernel size must be less than or equal to width and height
            if (kernelSize > width || kernelSize > height)
                throw new ArgumentException("Kernel size must be less than or equal to width and height.");

            this.Width = width;
            this.Height = height;
            this.KernelSize = kernelSize;
        }
    }
    public struct StopConditionParameters
    {
        public int MaxGenerations;
        public int TimeLimit;

        public StopConditionParameters(int maxGenerations, int timeLimit)
        {
            // Rule 1: Max generations must be greater than 0
            if (maxGenerations <= 0)
                throw new ArgumentException("Max generations must be greater than 0.");
            
            // Rule 2: Time limit must be greater than 0
            if (timeLimit <= 0)
                throw new ArgumentException("Time limit must be greater than 0.");

            this.MaxGenerations = maxGenerations;
            this.TimeLimit = timeLimit;
        }
    }
    public struct AlgorithmParameters
    {
        public int PopulationSize;
        public double MutationRate;
        public int TournamentSize;
        public double ElitesPercentage;

        public AlgorithmParameters(
            int populationSize,
            double mutationRate,
            int tournamentSize,
            double elitesPercentage)
        {
            // Rule 1: Mutation rate must be in the range [0.0, 1.0]
            if (!Utils.IsBetweenZeroAndOne(mutationRate))
                throw new ArgumentException("Mutation rate must be between 0.0 and 1.0.");

            // Rule 2: Elites percentage must be in the range [0.0, 1.0]
            if (!Utils.IsBetweenZeroAndOne(elitesPercentage))
                throw new ArgumentException("Elites percentage must be between 0.0 and 1.0.");
            
            // Rule 3: Population size must be greater than 0
            if (populationSize <= 0)
                throw new ArgumentException("Population size must be greater than 0.");
            
            // Rule 4: Tournament size must be greater than 0 and less than population size
            if (tournamentSize <= 0 || tournamentSize >= populationSize)
                throw new ArgumentException("Tournament size must be greater than 0 and less than population size.");

            this.PopulationSize = populationSize;
            this.MutationRate = mutationRate;
            this.TournamentSize = tournamentSize;
            this.ElitesPercentage = elitesPercentage;
        }
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

        public PoolingInitializationParameters(
            int unavailableGroundPoolCount,
            int waterPoolCount,
            double unavailableGroundMinArea,
            double unavailableGroundMaxArea,
            double waterMinArea,
            double waterMaxArea,
            double pathMinArea,
            double pathMaxArea)
        {
            // Rule 1: Max areas must always be greater than min areas
            if (unavailableGroundMaxArea < unavailableGroundMinArea)
                throw new ArgumentException("Unavailable ground max area must be greater than or equal to min area.");
            if (waterMaxArea < waterMinArea)
                throw new ArgumentException("Water max area must be greater than or equal to min area.");
            if (pathMaxArea < pathMinArea)
                throw new ArgumentException("Path max area must be greater than or equal to min area.");

            // Rule 2: Min and max areas must be in the range [0.0, 1.0]
            if (!Utils.IsBetweenZeroAndOne(unavailableGroundMinArea) || !Utils.IsBetweenZeroAndOne(unavailableGroundMaxArea))
                throw new ArgumentException("Unavailable ground areas must be between 0.0 and 1.0.");
            if (!Utils.IsBetweenZeroAndOne(waterMinArea) || !Utils.IsBetweenZeroAndOne(waterMaxArea))
                throw new ArgumentException("Water areas must be between 0.0 and 1.0.");
            if (!Utils.IsBetweenZeroAndOne(pathMinArea) || !Utils.IsBetweenZeroAndOne(pathMaxArea))
                throw new ArgumentException("Path areas must be between 0.0 and 1.0.");

            // Rule 3: The sum of min and max areas for all types cannot exceed 1.0
            if ((unavailableGroundMinArea + waterMinArea + pathMinArea) > 1.0)
                throw new ArgumentException("Sum of min areas for all types cannot exceed 1.0.");
            if ((unavailableGroundMaxArea + waterMaxArea + pathMaxArea) > 1.0)
                throw new ArgumentException("Sum of max areas for all types cannot exceed 1.0.");

            this.unavailableGroundPoolCount = unavailableGroundPoolCount;
            this.waterPoolCount = waterPoolCount;
            this.unavailableGroundMinArea = unavailableGroundMinArea;
            this.unavailableGroundMaxArea = unavailableGroundMaxArea;
            this.waterMinArea = waterMinArea;
            this.waterMaxArea = waterMaxArea;
            this.pathMinArea = pathMinArea;
            this.pathMaxArea = pathMaxArea;
        }
    }

}
