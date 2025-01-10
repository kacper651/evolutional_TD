using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using Random = System.Random;
using EvolutionaryAlgorithmUtils;

/*
 * TODO: 
 *  -   repair: par� r�nych metod
 *  -   automatyzacja pobierania parametr�w, �eby nie by�y pisane z palca tak jak teraz
 *  -   automatyzacja puszczania eksperyment�w 
 *  -   warstwa prezentacji: UNITY
 */

/*
    Oceny s� mega nieskalibrowane - OrderedCrossoverWeighted promuje �cie�ki tak bardzo, �e prawie ca�a mapa staje si� �cie�k�, Randomowe mutacje w zasadzie tak samo
 */

public class EvolutionaryAlgorithm
{
    private Random random;
    private readonly Dictionary<InitializationMethodType, Func<List<int[,]>>> InitializationStrategies;
    private readonly Dictionary<SelectionMethodType, Func<List<(int[,], double, double[,])>, ((int[,], double, double[,]), (int[,], double, double[,]))>> SelectionStrategies;
    private readonly Dictionary<CrossoverMethodType, Func<(int[,] map, double[,] fitnessMatrix), (int[,] map, double[,] fitnessMatrix), int[,]>> CrossoverStrategies;
    private readonly Dictionary<MutationMethodType, Func<int[,], int[,]>> MutationStrategies;
    private readonly Dictionary<RepairMethodType, Func<int[,], int[,]>> RepairStrategies;
    private readonly Dictionary<CreatingNewGenerationType, Func<List<(int[,], double, double[,])>, List<int[,]>>> CreatingNewGenerationStrategies;

    public List<int[,]> Population { get; private set; } = new();
    public List<double> BestFitnessValues { get; private set; } = new();
    public List<int[,]> BestIndividualValues { get; private set; } = new();

    private int MapWidth;
    private int MapHeight;
    private int KernelSize;

    private int MaxGenerations;
    private int TimeLimit;

    private int PopulationSize;
    private int TournamentSize;
    private double MutationRate;
    private double ElitesPercentage;

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
        KernelSize = mapParams.KernelSize;

        MaxGenerations = stopParams.MaxGenerations;
        TimeLimit = stopParams.TimeLimit;

        PopulationSize = algParams.PopulationSize;
        MutationRate = algParams.MutationRate;
        TournamentSize = algParams.TournamentSize;
        ElitesPercentage = algParams.ElitesPercentage;

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
        SelectionStrategies = new Dictionary<SelectionMethodType, Func<List<(int[,], double, double[,])>, ((int[,], double, double[,]), (int[,], double, double[,]))>>()
        {
            { SelectionMethodType.Tournament, SelectionTournament },
            { SelectionMethodType.Roulette, SelectionRoulette }
        };
        CrossoverStrategies = new Dictionary<CrossoverMethodType, Func<(int[,] map, double[,] fitnessMatrix), (int[,] map, double[,] fitnessMatrix), int[,]>>()
        {
            { CrossoverMethodType.OrderedCrossover, CrossoverOX },
            { CrossoverMethodType.CycleCrossover, CrossoverCX },
            { CrossoverMethodType.SwapCrossoverWeighted, CrossoverSwapWeighted }
        };
        MutationStrategies = new Dictionary<MutationMethodType, Func<int[,], int[,]>>()
        {
            { MutationMethodType.Swap, MutationSwap },
            { MutationMethodType.Inverse, MutationInverse },
            { MutationMethodType.Random, MutationRandom }
        };
        RepairStrategies = new Dictionary<RepairMethodType, Func<int[,], int[,]>>()
        {
            { RepairMethodType.Whatever1, RepairWhatever1 },
            { RepairMethodType.Whatever2, RepairWhatever2 }
        };
        CreatingNewGenerationStrategies = new Dictionary<CreatingNewGenerationType, Func<List<(int[,], double, double[,])>, List<int[,]>>>()
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
        var population = new List<int[,]>();
        for (int i = 0; i < PopulationSize; i++)
        {
            var individual = new int[MapWidth, MapHeight];

            //(int startX, int startY) = (0, random.Next(MapHeight));
            //(int endX, int endY) = (MapWidth - 1, random.Next(MapHeight));
            (int startX, int startY, int endX, int endY) = MapUtils.GetRandomStartEndCoordinates(MapWidth, MapHeight);

            List<(int, int)> trail = MapUtils.GenerateTrail(startX, startY, endX, endY, MapWidth, MapHeight);

            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    if (trail.Contains((x, y)))
                    {
                        individual[x, y] = MapUtils.PATH_TILE;
                    }
                    else
                    {
                        individual[x, y] = random.Next(1, 4);
                    }
                }
            }
            //MapUtils.PrintMap(individual);
            //Console.WriteLine("\n - - - -\n\n");

            population.Add(individual);
        }


        return population;
    }

    private ((int[,], double, double[,]), (int[,], double, double[,])) SelectionTournament(List<(int[,], double, double[,])> population)
    {
        List<(int[,] map, double fitnessValue, double[,] fitnessMatrix)> candidates = new();
        for (int i = 0; i < TournamentSize * 2; i++)
        {
            int randomIndex = random.Next(PopulationSize);
            candidates.Add(population[randomIndex]);
        }

        var firstHalf = candidates
            .Take(TournamentSize)
            .OrderByDescending(x => x.fitnessValue)
            .ToList();
        var secondHalf = candidates
            .Skip(TournamentSize)
            .Take(TournamentSize)
            .OrderByDescending(x => x.fitnessValue)
            .ToList();

        return (firstHalf.First(), secondHalf.First());
    }

    private ((int[,], double, double[,]), (int[,], double, double[,])) SelectionRoulette(List<(int[,] map, double fitnessValue, double[,] fitnessMatrix)> population)
    {
        double totalFitness = population.Sum(ind => ind.fitnessValue);

        var probabilities = (totalFitness == 0 ?
            population.Select(ind => 1.0 / PopulationSize) :
            population.Select(ind => ind.fitnessValue / totalFitness))
            .ToList();

        (int[,], double, double[,]) SelectIndividual()
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

        (int[,], double, double[,]) parent1 = SelectIndividual();
        (int[,], double, double[,]) parent2 = SelectIndividual();
        return (parent1, parent2);
    }

    private int[,] CrossoverOX((int[,] map, double[,] fitnessMatrix) parent1, (int[,] map, double[,] fitnessMatrix) parent2)
    {
        // every part of the genotype that is not from the random segment is taken from parent2
        int[,] child = (int[,])parent2.map.Clone();

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
                child[y, x] = parent1.map[y, x];
            }
        }

        return child;
    }

    private int[,] CrossoverCX((int[,] map, double[,] fitnessMatrix) parent1, (int[,] map, double[,] fitnessMatrix) parent2)
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
                    child[currentY, currentX] = parent1.map[currentY, currentX];
                    visited[currentY, currentX] = true;

                    // Find the next position in the cycle using parent2
                    int valueToFind = parent1.map[currentY, currentX];
                    (int nextY, int nextX) = MapUtils.FindValuePosition(parent2.map, valueToFind);

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
                    child[y, x] = parent2.map[y, x];
                }
            }
        }

        return child;
    }

    private int[,] CrossoverSwapWeighted((int[,] map, double[,] fitnessMatrix) parent1, (int[,] map, double[,] fitnessMatrix) parent2)
    {
        int[,] child = (int[,])parent2.map.Clone();

        (int parent1X, int parent1Y) = MapUtils.SelectIndexWeightedPromoteHighFitness(parent1.fitnessMatrix); // this is the region to be inserted into the child, so I want it to be good
        (int parent2X, int parent2Y) = MapUtils.SelectIndexWeightedPromoteLowFitness(parent2.fitnessMatrix);  // this is the region to be replaced, so I want it it be garbage

        // Extract kernel regions from parent 1 (to be inserted into the child)
        int[,] segmentFromParent1 = MapUtils.ExtractKernelRegion(parent1.map, parent1X, parent1Y, KernelSize);

        MapUtils.InsertKernelRegion(child, parent2X, parent2Y, segmentFromParent1);

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

    private int[,] MutationRandom(int[,] individual)
    {
        if (random.NextDouble() >= MutationRate)
        {
            return individual;
        }
        var newIndividual = (int[,])individual.Clone();

        int x = random.Next(0, MapWidth);
        int y = random.Next(0, MapHeight);

        int[,] region = MapUtils.ExtractKernelRegion(individual, x, y, KernelSize);
        int[,] mutatedRegion = MutateKernelRegion(region);

        MapUtils.InsertKernelRegion(newIndividual, x, y, mutatedRegion);

        return newIndividual;
    }

    private int[,] MutateKernelRegion(int[,] region)
    {
        int[,] mutatedRegion = (int[,])region.Clone();
        for (int y = 0; y < KernelSize; y++)
        {
            for (int x = 0; x < KernelSize; x++)
            {
                mutatedRegion[y, x] = random.Next(0, 4);
            }
        }
        return mutatedRegion;
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
        List<int> subMatrixValues = new();

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

    private (double, double[,]) Fitness(int[,] matrix)
    {
        double[,] fitnessMatrix = new double[MapHeight, MapWidth];
        double totalFitness = 0.0;
        //Console.WriteLine("\nFitness calculation (matrix size - {0})\n", MapWidth);
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                int[,] region = MapUtils.ExtractKernelRegion(matrix, x, y, KernelSize);

                double regionFitness = CalculateRegionFitness(region);
                fitnessMatrix[y, x] = regionFitness;
                totalFitness += regionFitness;
            }
        }

        var (tileRatioPromotionFitness, availableTilesOnBorder) = EvaluateTileRatios(matrix);
        totalFitness += tileRatioPromotionFitness;
        totalFitness -= PenalizeSoloTiles(matrix);
        totalFitness -= PenalizeAvailableRegionsOnTheBorder(matrix, availableTilesOnBorder, 1);
        totalFitness -= PenalizeTooManyInOutPaths(matrix);
        return (totalFitness, fitnessMatrix);
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

    private List<(int[,], double, double[,])> EvaluatePopulation(List<int[,]> population)
    {
        List<(int[,], double, double[,])> EvaluatedPopulation = new();
        foreach (int[,] individual in population)
        {
            var (IndividualScore, IndividualFitnessMatrix) = Fitness(individual);
            EvaluatedPopulation.Add((individual, IndividualScore, IndividualFitnessMatrix));
        }
        return EvaluatedPopulation;
    }

    private List<int[,]> CreateNewGenerationNoElitism(List<(int[,], double, double[,])> population)
    {
        List<int[,]> newPopulation = new();

        for (int i = 0; i < PopulationSize; i++)
        {
            var (Parent1, Parent2) = SelectionStrategies[SelectionMethod](population);

            var Child = CrossoverStrategies[CrossoverMethod]((Parent1.Item1, Parent1.Item3), (Parent2.Item1, Parent2.Item3));

            if (random.NextDouble() < MutationRate)
            {
                Child = MutationStrategies[MutationMethod](Child);
            }

            newPopulation.Add(RepairStrategies[RepairMethod](Child));
        }
        return newPopulation;
    }

    private List<int[,]> CreateNewGenerationElitism(List<(int[,] map, double fitnessValue, double[,] fitnessMatrix)> population)
    {
        int numElites = (int)(PopulationSize * ElitesPercentage);
        List<int[,]> newPopulation = population
            .OrderByDescending(x => x.fitnessValue)
            .Take(numElites)
            .Select(ind => ind.map)
            .ToList();

        for (int i = 0; i < PopulationSize; i++)
        {
            var (Parent1, Parent2) = SelectionStrategies[SelectionMethod](population);

            var Child = CrossoverStrategies[CrossoverMethod]((Parent1.Item1, Parent1.Item3), (Parent2.Item1, Parent2.Item3));

            if (random.NextDouble() < MutationRate)
            {
                Child = MutationStrategies[MutationMethod](Child);
            }

            newPopulation.Add(RepairStrategies[RepairMethod](Child));
        }
        return newPopulation;
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
            List<(int[,], double, double[,])> EvaluatedPopulation = EvaluatePopulation(Population);

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
        return (new MapParameters { Width = 10, Height = 10, KernelSize = 3 },
                new StopConditionParameters { MaxGenerations = 10, TimeLimit = 60 },
                new AlgorithmParameters { PopulationSize = 500, MutationRate = 0.2, TournamentSize = 2, ElitesPercentage = 0.05 },
                new MethodSelectionParameters
                {
                    InitializationMethod = InitializationMethodType.Random,
                    SelectionMethod = SelectionMethodType.Tournament,
                    CrossoverMethod = CrossoverMethodType.OrderedCrossover,
                    MutationMethod = MutationMethodType.Swap,
                    RepairMethod = RepairMethodType.Whatever1,
                    CreatorMethod = CreatingNewGenerationType.Elitism
                }
            );
    }

    public static void ExecuteEvolutionaryAlgorithm()
    {
        var (mapParams, stopParams, algParams, metParams) = GetParameters();

        var generator = new EvolutionaryAlgorithm(mapParams, stopParams, algParams, metParams);
        generator.Run();
        //var x = generator.InitializeGreedy();

    }

}
