using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using Random = System.Random;
using EvolutionaryAlgorithmUtils;

/*
 * TODO: 
 *  -   dodaæ prawdopodobieñstwa wymiany do crossoveru i mutacji na podstawie fitnessMatrix - random weighted number generator needed
 *  -   initialization: coœ bardziej sensownego ni¿ losowa - nie musi byæ greedy, to po prostu przysz³o mi pierwsze do g³owy
 *  -   repair: parê ró¿nych metod
 *  -   automatyzacja pobierania parametrów, ¿eby nie by³y pisane z palca tak jak teraz
 *  -   automatyzacja puszczania eksperymentów 
 *  -   warstwa prezentacji: UNITY
 */

/*
    thought: maybe fitness should return both the matrix and the score.
    I could use the knowledge of the matrix to modify probabilities for regions
        that could be improved - for example:
    the bad regions would be more likely to get mutated or changed during crossovers
*/

public class EvolutionaryAlgorithm
{
    private Random random;
    private readonly Dictionary<InitializationMethodType, Func<List<int[,]>>> InitializationStrategies;
    private readonly Dictionary<SelectionMethodType, Func<List<(int[,], double)>, ((int[,], double), (int[,], double))>> SelectionStrategies;
    private readonly Dictionary<CrossoverMethodType, Func<int[,], int[,], int[,]>> CrossoverStrategies;
    private readonly Dictionary<MutationMethodType, Func<int[,], int[,]>> MutationStrategies;
    private readonly Dictionary<RepairMethodType, Func<int[,], int[,]>> RepairStrategies;
    private readonly Dictionary<CreatingNewGenerationType, Func<List<(int[,], double)>, List<int[,]>>> CreatingNewGenerationStrategies;

    private List<int[,]> Population = new();
    private List<double> BestFitnessValues = new();
    private List<int[,]> BestIndividualValues = new();

    private int MapWidth;
    private int MapHeight;

    private int MaxGenerations;
    private int TimeLimit;

    private int PopulationSize;
    private int TournamentSize;
    private double MutationRate;

    private InitializationMethodType InitializationMethod;
    private SelectionMethodType SelectionMethod;
    private CrossoverMethodType CrossoverMethod;
    private MutationMethodType MutationMethod;
    private RepairMethodType RepairMethod;
    private CreatingNewGenerationType CreatingNewGenerationMethod;

    public EvolutionaryAlgorithm(MapParameters mapParams,
                                    StopConditionParameters stopParams,
                                    AlgorithmParameters algParams,
                                    MethodSelectionParameters metParams)
    {
        random = new Random();
        MapWidth = mapParams.Width;
        MapHeight = mapParams.Height;

        MaxGenerations = stopParams.MaxGenerations;
        TimeLimit = stopParams.TimeLimit;

        PopulationSize = algParams.PopulationSize;
        MutationRate = algParams.MutationRate;
        TournamentSize = algParams.TournamentSize;

        InitializationMethod = metParams.InitializationMethod;
        SelectionMethod = metParams.SelectionMethod;
        CrossoverMethod = metParams.CrossoverMethod;
        MutationMethod = metParams.MutationMethod;
        RepairMethod = metParams.RepairMethod;
        CreatingNewGenerationMethod = metParams.CreatorMethod;

        InitializationStrategies = new Dictionary<InitializationMethodType, Func<List<int[,]>>>()
        {
            { InitializationMethodType.Random, InitializeRandom },
            { InitializationMethodType.Greedy, InitializeGreedy }
        };
        SelectionStrategies = new Dictionary<SelectionMethodType, Func<List<(int[,], double)>, ((int[,], double), (int[,], double))>>()
        {
            { SelectionMethodType.Tournament, SelectionTournament },
            { SelectionMethodType.Roulette, SelectionRoulette }
        };
        CrossoverStrategies = new Dictionary<CrossoverMethodType, Func<int[,], int[,], int[,]>>()
        {
            { CrossoverMethodType.OrderedCrossover, CrossoverOX },
            { CrossoverMethodType.CycleCrossover, CrossoverCX }
        };
        MutationStrategies = new Dictionary<MutationMethodType, Func<int[,], int[,]>>()
        {
            { MutationMethodType.Swap, MutationSwap },
            { MutationMethodType.Inverse, MutationInverse }
        };
        RepairStrategies = new Dictionary<RepairMethodType, Func<int[,], int[,]>>()
        {
            { RepairMethodType.Whatever1, RepairWhatever1 },
            { RepairMethodType.Whatever2, RepairWhatever2 }
        };
        CreatingNewGenerationStrategies = new Dictionary<CreatingNewGenerationType, Func<List<(int[,], double)>, List<int[,]>>>()
        {
            { CreatingNewGenerationType.Elitism, CreateNewGenerationElitism },
            { CreatingNewGenerationType.NoElitism, CreateNewGenerationNoElitism }
        };
    }

    private List<int[,]> InitializeRandom()
    {
        var population = new List<int[,]>();
        for (int i = 0; i < PopulationSize; i++)
        {
            var individual = new int[MapWidth, MapHeight];
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    individual[x, y] = random.Next(0, 4);
                }
            }
            population.Add(individual);
        }
        return population;
    }
    private List<int[,]> InitializeGreedy()
    {
        throw new NotImplementedException();
    }

    private ((int[,], double), (int[,], double)) SelectionTournament(List<(int[,], double)> population)
    {
        List<(int[,], double)> candidates = new();
        for (int i = 0; i < TournamentSize * 2; i++)
        {
            int randomIndex = random.Next(PopulationSize);
            candidates.Add(population[randomIndex]);
        }

        var firstHalf = candidates
            .Take(TournamentSize)
            .OrderByDescending(x => x.Item2)
            .ToList();
        var secondHalf = candidates
            .Skip(TournamentSize)
            .Take(TournamentSize)
            .OrderByDescending(x => x.Item2)
            .ToList();

        return (firstHalf.First(), secondHalf.First());
    }

    private ((int[,], double), (int[,], double)) SelectionRoulette(List<(int[,], double)> population)
    {
        double totalFitness = population.Sum(ind => ind.Item2);

        var probabilities = population
            .Select(ind => ind.Item2 / totalFitness)
            .ToList();

        (int[,], double) SelectIndividual()
        {
            double randomValue = random.NextDouble();
            double sum = 0.0;
            for (int i = 0; i < PopulationSize; i++)
            {
                sum += probabilities[i];
                if (randomValue <= sum)
                {
                    return population[i];
                }
            }
            return population.Last();
        }

        (int[,], double) parent1 = SelectIndividual();
        (int[,], double) parent2 = SelectIndividual();
        return (parent1, parent2);
    }

    private int[,] CrossoverOX(int[,] parent1, int[,] parent2)
    {
        // every part of the genotype that is not from the random segment is taken from parent2
        int[,] child = (int[,])parent2.Clone();

        // select random segment within the matrix
        int[] indiciesX = Enumerable
            .Range(0, MapWidth)
            .OrderBy(x => random.Next())
            .Take(2)
            .ToArray();
        int startX = Math.Min(indiciesX[0], indiciesX[1]);
        int endX = Math.Max(indiciesX[0], indiciesX[1]);

        int[] indiciesY = Enumerable
            .Range(0, MapHeight)
            .OrderBy(x => random.Next())
            .Take(2)
            .ToArray();
        int startY = Math.Min(indiciesY[0], indiciesY[1]);
        int endY = Math.Max(indiciesY[0], indiciesY[1]);

        // fill the segment-part of the child with values from parent1
        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                child[y, x] = parent1[y, x];
            }
        }

        return child;
    }

    private int[,] CrossoverCX(int[,] parent1, int[,] parent2)
    {
        int[,] child = new int[MapHeight, MapWidth];
        for (int y = 0; y < MapHeight; y++)
            for (int x = 0; x < MapWidth; x++)
                child[y, x] = -1; // Initialize child with -1

        bool[,] visited = new bool[MapHeight, MapWidth];

        // Iterate through all positions to start cycles
        for (int startY = 0; startY < MapHeight; startY++)
        {
            for (int startX = 0; startX < MapWidth; startX++)
            {
                // Skip visited positions
                if (visited[startY, startX])
                {
                    continue;
                }

                // Begin a new cycle
                int currentX = startX;
                int currentY = startY;

                do
                {
                    // Copy value from parent1
                    child[currentY, currentX] = parent1[currentY, currentX];
                    visited[currentY, currentX] = true;

                    // Find the next position in the cycle using parent2
                    int valueToFind = parent1[currentY, currentX];
                    (int nextY, int nextX) = MapUtils.FindValuePosition(parent2, valueToFind);

                    // Move to the next position
                    currentX = nextX;
                    currentY = nextY;
                }
                while (!visited[currentY, currentX]); // Stop when a cycle is complete
            }
        }

        // Fill remaining unassigned positions with values from parent2
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                if (child[y, x] == -1)
                {
                    child[y, x] = parent2[y, x];
                }
            }
        }

        return child;
    }

    private int[,] MutationSwap(int[,] individual)
    {
        if (random.NextDouble() >= MutationRate)
        {
            return individual;
        }
        var newIndividual = (int[,])individual.Clone();
        int[] indiciesX = Enumerable
            .Range(0, MapWidth)
            .OrderBy(x => random.Next())
            .Take(2)
            .ToArray();
        int[] indiciesY = Enumerable
            .Range(0, MapHeight)
            .OrderBy(x => random.Next())
            .Take(2)
            .ToArray();
        var firstCoord = (indiciesX[0], indiciesY[0]);
        var secondCoord = (indiciesX[1], indiciesY[1]);

        newIndividual[firstCoord.Item1, firstCoord.Item2] = individual[secondCoord.Item1, secondCoord.Item2];
        newIndividual[secondCoord.Item1, secondCoord.Item2] = individual[firstCoord.Item1, firstCoord.Item2];
        return newIndividual;
    }

    private int[,] MutationInverse(int[,] individual)
    {
        if (random.NextDouble() >= MutationRate)
        {
            return individual;
        }
        var newIndividual = (int[,])individual.Clone();

        int startX = random.Next(0, MapWidth);
        int startY = random.Next(0, MapHeight);

        int stepX = random.Next(1, MapWidth - startX + 1);
        int stepY = random.Next(1, MapHeight - startY + 1);

        int subMatrixSize = stepX * stepY;
        List<int> subMatrixValues = new List<int>(subMatrixSize);

        for (int i = 0; i < subMatrixSize; i++)
        {
            int x = startX + (i % stepX);
            int y = startY + (i / stepX);
            subMatrixValues.Add(individual[y, x]);
        }

        subMatrixValues.Reverse();

        for (int i = 0; i < subMatrixSize; i++)
        {
            int x = startX + (i % stepX);
            int y = startY + (i / stepX);
            newIndividual[y, x] = subMatrixValues[i];
        }
        return newIndividual;
    }
    private int[,] RepairWhatever1(int[,] individual)
    {
        return individual;
    }
    private int[,] RepairWhatever2(int[,] individual)
    {
        throw new NotImplementedException();
    }

    private double Fitness(int[,] matrix)
    {
        double[,] fitnessMatrix = new double[MapHeight, MapWidth];
        double totalFitness = 0.0;
        //Console.WriteLine("\nFitness calculation (matrix size - {0})\n", MapWidth);
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                int[,] region = MapUtils.ExtractKernelRegion(matrix, x, y, 3);

                double regionFitness = CalculateRegionFitness(region);
                fitnessMatrix[y, x] = regionFitness;
                totalFitness += regionFitness;
            }
        }

        //Console.WriteLine("Map:");
        //MapUtils.PrintMap(matrix);
        //Console.WriteLine();
        //double sum = fitnessMatrix.Cast<double>().Sum();
        //Console.WriteLine("Fitness matrix sum: {0}", sum);
        //for (int i = 0; i < fitnessMatrix.GetLength(0); i++)
        //{
        //    for (int j = 0; j < fitnessMatrix.GetLength(1); j++)
        //    {
        //        //string description = GetTileDescription(map[i, j]);
        //        string description = fitnessMatrix[i, j].ToString();
        //        Console.Write(description.PadRight(5));
        //    }
        //    Console.WriteLine();
        //}

        //totalFitness += EvaluateTileRatios(matrix);
        var (tileRatioPromotionFitness, availableTilesOnBorder) = EvaluateTileRatios(matrix);
        totalFitness += tileRatioPromotionFitness;
        totalFitness -= PenalizeSoloTiles(matrix);
        totalFitness -= PenalizeAvailableRegionsOnTheBorder(matrix, availableTilesOnBorder, 1);
        totalFitness -= PenalizeTooManyInOutPaths(matrix);
        return totalFitness;
    }

    private double CalculateRegionFitness(int[,] region)
    {
        double regionFitness = 0.0;

        regionFitness += EvaluatePathConnectivity(region);    // evaluate path connectivity
        regionFitness += PromoteL_Shape(region);              // evaluate region 'chunks'
        //regionFitness -= PenalizeSoloTiles(region);           // penalize single tiles

        return regionFitness;
    }

    private double EvaluatePathConnectivity(int[,] region)
    {
        List<bool> connectedPaths = new();
        for (int y = 0; y < region.GetLength(0); y++)
        {
            for (int x = 0; x < region.GetLength(1); x++)
            {
                if (region[y, x] == MapUtils.PATH_TILE)
                {
                    connectedPaths.Add(MapUtils.IsConnectedToOtherPathTiles(region, x, y));
                }
            }
        }
        //Console.WriteLine("EvaluatePathConn\tadded score:\t{0}", connectedPaths.Count(x => x));
        return connectedPaths.Count(x => x);
    }

    private double PromoteL_Shape(int[,] region)
    {
        return MapUtils.ContainsLShape(region) ? MapUtils.FOUND_L_SHAPE_PROMOTION_WEIGHT : MapUtils.NO_L_SHAPE_PENALTY_WEIGHT;
    }

    // weights for each ratio can be adjusted
    private (double, int) EvaluateTileRatios(int[,] map)    // 10 = perfect, even ratio
    {
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
                    case MapUtils.PATH_TILE:
                        pathTilesCount++;
                        break;
                    case MapUtils.AVAILABLE_GROUND_TILE:
                        availableGroundTilesCount++;
                        break;
                    case MapUtils.UNAVAILABLE_GROUND_TILE:
                        unavailableGroundTilesCount++;
                        break;
                    case MapUtils.WATER_TILE:
                        waterTilesCount++;
                        break;
                }
            }
        }
        double pathToTotalRatio = (double)pathTilesCount / totalTiles;
        double availableToTotalRatio = (double)availableGroundTilesCount / totalTiles;
        double unavailableToTotalRatio = (double)(unavailableGroundTilesCount + waterTilesCount) / totalTiles;

        double combinedRatio = MapUtils.WeighTileRatios(pathToTotalRatio, availableToTotalRatio, unavailableToTotalRatio);
        //Console.WriteLine("EvaluateTileRatios\tadded score:\t{0}", combinedRatio);

        return (combinedRatio, availableGroundTilesCount);
    }

    // maybe in regards to available tiles ratio?  
    // availableTilesOnBorder is one thing availableTiles in general is another
    // we can have 100 on the border, if there are 500 in general, but
    // if 450 out of 500 are on the border, it's an issue
    // i can get the total number of available tiles from EvaluateTileRatios method
    private double PenalizeAvailableRegionsOnTheBorder(int[,] matrix, int totalAvailableCount, int borderSize) // penalty=10 - worst case, all available tiles on the border
    {
        int pathTilesCount = 0;

        // top border (+ corners)
        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < borderSize; y++)
            {
                if (matrix[y, x] == MapUtils.AVAILABLE_GROUND_TILE)
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
                if (matrix[y, x] == MapUtils.AVAILABLE_GROUND_TILE)
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
                if (matrix[y, x] == MapUtils.AVAILABLE_GROUND_TILE)
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
                if (matrix[y, x] == MapUtils.AVAILABLE_GROUND_TILE)
                {
                    pathTilesCount++;
                }
            }
        }

        var ratio = (double)pathTilesCount / totalAvailableCount * MapUtils.AVAILABLE_ON_BORDER_SHIFT_PENALTY_WEIGHT;
        //Console.WriteLine("PenalizeAvRegOnBor\tpenalty score:\t{0}", ratio);
        return ratio;
    }

    private double PenalizeSoloTiles(int[,] region)
    {
        double fitnessPenalty = 0.0;

        for (int y = 0; y < region.GetLength(0); y++)
        {
            for (int x = 0; x < region.GetLength(1); x++)
            {
                int value = region[y, x];
                if (value != -1 && MapUtils.IsSoloCell(region, x, y, value))
                {
                    fitnessPenalty += 1.0;
                }
            }
        }
        //Console.WriteLine("PenalizeSoloTiles\tpenalty score:\t{0}", fitnessPenalty - 2.0);
        return fitnessPenalty;
    }

    private double PenalizeTooManyInOutPaths(int[,] matrix)
    {
        double pathsOnBorder = 0.0;

        // top border (+ corners)
        for (int x = 0; x < MapWidth; x++)
        {
            if (matrix[0, x] == MapUtils.PATH_TILE)
            {
                pathsOnBorder += 1.0;
            }
        }

        // bottom border (+ corners)
        for (int x = 0; x < MapWidth; x++)
        {
            if (matrix[MapHeight - 1, x] == MapUtils.PATH_TILE)
            {
                pathsOnBorder += 1.0;
            }
        }

        // left border
        for (int y = 1; y < MapHeight - 1; y++)
        {
            if (matrix[y, 0] == MapUtils.PATH_TILE)
            {
                pathsOnBorder += 1.0;
            }
        }

        // right border
        for (int y = 1; y < MapHeight - 1; y++)
        {
            if (matrix[y, MapWidth - 1] == MapUtils.PATH_TILE)
            {
                pathsOnBorder += 1.0;
            }
        }
        //Console.WriteLine("PenalizeSoloTiles\tpenalty score:\t{0}", pathsOnBorder-2.0);
        return (pathsOnBorder - 2.0);
    }

    private List<(int[,], double)> EvaluatePopulation(List<int[,]> population)
    {
        List<(int[,], double)> EvaluatedPopulation = new();
        foreach (int[,] individual in population)
        {
            var IndividualScore = Fitness(individual);
            EvaluatedPopulation.Add((individual, IndividualScore));
        }
        return EvaluatedPopulation;
    }

    private List<int[,]> CreateNewGenerationNoElitism(List<(int[,], double)> population)
    {
        List<int[,]> NewPopulation = new();

        for (int i = 0; i < PopulationSize; i++)
        {
            var (Parent1, Parent2) = SelectionStrategies[SelectionMethod](population);

            var Child = CrossoverStrategies[CrossoverMethod](Parent1.Item1, Parent2.Item1);

            if (random.NextDouble() < MutationRate)
            {
                Child = MutationStrategies[MutationMethod](Child);
            }

            NewPopulation.Add(RepairStrategies[RepairMethod](Child));
        }
        return NewPopulation;
    }

    private List<int[,]> CreateNewGenerationElitism(List<(int[,], double)> population)
    {
        var SortedPopulation = population
            .OrderByDescending(x => x.Item2)
            .ToList();
        int numElites = (int)(PopulationSize * 0.05);
        //List<int[,]> NewPopulation = SortedPopulation
        //    .Take(numElites)
        //    .Select(ind => ind.Item1)
        //    .ToList();
        List<int[,]> NewPopulation = SortedPopulation
            .Take(numElites)
            .Select(ind => (int[,])ind.Item1.Clone())
            .ToList();

        for (int i = 0; i < PopulationSize; i++)
        {
            var (Parent1, Parent2) = SelectionStrategies[SelectionMethod](population);

            var Child = CrossoverStrategies[CrossoverMethod](Parent1.Item1, Parent2.Item1);

            if (random.NextDouble() < MutationRate)
            {
                Child = MutationStrategies[MutationMethod](Child);
            }

            NewPopulation.Add(RepairStrategies[RepairMethod](Child));
        }
        return NewPopulation;
    }

    public void Run()
    {
        Stopwatch sw = new Stopwatch();
        Population = InitializationStrategies[InitializationMethod]();
        BestFitnessValues = new();
        BestIndividualValues = new();

        sw.Start();
        for (int generation = 0; generation < MaxGenerations; generation++)
        {
            List<(int[,], double)> EvaluatedPopulation = EvaluatePopulation(Population);

            var BestIndividual = EvaluatedPopulation.OrderByDescending(x => x.Item2).First();
            BestIndividualValues.Add(BestIndividual.Item1);
            BestFitnessValues.Add(BestIndividual.Item2);
            //Console.WriteLine("\n------------------------------------");
            Console.WriteLine("Generation: {0}, Best Fitness: {1:F5},   s({2})", generation, BestIndividual.Item2, sw.Elapsed.Seconds);
            //Console.WriteLine("------------------------------------\n");

            if (sw.Elapsed.TotalSeconds >= TimeLimit)
            {
                Console.WriteLine("Time limit reached. Stopping the algorithm.");
                break;
            }
            Population = CreatingNewGenerationStrategies[CreatingNewGenerationMethod](EvaluatedPopulation);
        }
        sw.Stop();
        Console.WriteLine("Elapsed={0:F5} seconds", sw.Elapsed.TotalSeconds);
        Console.WriteLine("Best fitness: {0:F5}", BestFitnessValues.Last());
        Console.WriteLine("Worst fitness: {0:F5}", BestFitnessValues.First());
        MapUtils.PrintMap(Population.Last());
    }

    public static (MapParameters, StopConditionParameters, AlgorithmParameters, MethodSelectionParameters) GetParameters()
    {
        //  whatever as default, it's just a starting point
        return (new MapParameters { Width = 10, Height = 10 },
                new StopConditionParameters { MaxGenerations = 999999, TimeLimit = 60 },
                new AlgorithmParameters { PopulationSize = 500, MutationRate = 0.2, TournamentSize = 2 },
                new MethodSelectionParameters
                {
                    InitializationMethod = InitializationMethodType.Random,
                    SelectionMethod = SelectionMethodType.Tournament,
                    CrossoverMethod = CrossoverMethodType.CycleCrossover,
                    MutationMethod = MutationMethodType.Inverse,
                    RepairMethod = RepairMethodType.Whatever1,
                    CreatorMethod = CreatingNewGenerationType.Elitism
                }
            );
    }

    public static void Main(string[] args)
    {
        var (mapParams, stopParams, algParams, metParams) = GetParameters();

        var generator = new EvolutionaryAlgorithm(mapParams, stopParams, algParams, metParams);
        generator.Run();
    }

}
