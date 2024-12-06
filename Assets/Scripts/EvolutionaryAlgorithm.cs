using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using Random = System.Random;
using EvolutionaryAlgorithmUtils;

/*
 * Pytanie: jak puszczaæ ten skrypt z poziomu UNITY, ¿eby wyœwietla³o siê wszystko ³adnie w konsoli?
 *          Bo jak puszczam to tak normalnie tutaj, to mi zaczyna debugowaæ i w ogóle nie wyskakuje terminal przez to,
 *          ¿e to przez UNITY jest, wiêc utworzy³em sobie do tego oddzielny projekt na C# desktop app ¿eby to testowaæ jak g³upek xdd
 *          (dzia³a tam wszystko jak coœ oczywiœcie)          
*/

/*
 * TODO: 
 *  -   fitness function
 *  -   initialization: coœ bardziej sensownego ni¿ losowa - nie musi byæ greedy, to po prostu przysz³o mi pierwsze do g³owy
 *  -   selection: roulette
 *  -   crossover: ordered, cycle, coœ innego, byle mia³o sens dla problemu
 *  -   mutation: swap, inverse, coœ innego, byle mia³o sens dla problemu
 *  -   repair: parê ró¿nych metod
 *  -   automatyzacja pobierania parametrów, ¿eby nie by³y pisane z palca tak jak teraz
 *  -   automatyzacja puszczania eksperymentów 
 *  -   warstwa prezentacji: UNITY
 */

public class EvolutionaryAlgorithm
{
    private Random random;
    private readonly Dictionary<InitializationMethodType, Func<List<int[,]>>> InitializationStrategies;
    private readonly Dictionary<SelectionMethodType, Func<List<(int[,], double)>, ((int[,], double), (int[,], double))>> SelectionStrategies;
    private readonly Dictionary<CrossoverMethodType, Func<int[,], int[,], int[,]>> CrossoverStrategies;
    private readonly Dictionary<MutationMethodType, Func<int[,], int[,]>> MutationStrategies;
    private readonly Dictionary<RepairMethodType, Func<int[,], int[,]>> RepairStrategies;

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

    private double Fitness(int[,] matrix)
    {
        return random.NextDouble();
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

    private int[,] CrossoverOX(int[,] parent1, int[,] parent2)
    {
        return parent1;
    }

    private int[,] CrossoverCX(int[,] parent1, int[,] parent2)
    {
        throw new NotImplementedException();
    }

    private int[,] MutationSwap(int[,] individual)
    {
        throw new NotImplementedException();
    }
    private int[,] MutationInverse(int[,] individual)
    {
        return individual;
    }
    private int[,] RepairWhatever1(int[,] individual)
    {
        return individual;
    }
    private int[,] RepairWhatever2(int[,] individual)
    {
        throw new NotImplementedException();
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

    private List<int[,]> CreateNewGeneration(List<(int[,], double)> population)
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
            Console.WriteLine("Generation: {0}, Best Fitness: {1}", generation, BestIndividual.Item2);
            if (sw.Elapsed.TotalSeconds >= TimeLimit)
            {
                Console.WriteLine("Time limit reached. Stopping the algorithm.");
                break;
            }
            Population = CreateNewGeneration(EvaluatedPopulation);
        }
        sw.Stop();
        Console.WriteLine("Elapsed={0:F3} seconds\n", sw.Elapsed.TotalSeconds);
        MapUtils.PrintMap(Population.Last());
    }

    public static (MapParameters, StopConditionParameters, AlgorithmParameters, MethodSelectionParameters) GetParameters()
    {
        //  whatever as default, as a starting point
        return (new MapParameters { Width = 5, Height = 5 },
                new StopConditionParameters { MaxGenerations = 999999, TimeLimit = 1 },
                new AlgorithmParameters { PopulationSize = 100, MutationRate = 0.01, TournamentSize = 2 },
                new MethodSelectionParameters
                {
                    InitializationMethod = InitializationMethodType.Random,
                    SelectionMethod = SelectionMethodType.Tournament,
                    CrossoverMethod = CrossoverMethodType.OrderedCrossover,
                    MutationMethod = MutationMethodType.Inverse,
                    RepairMethod = RepairMethodType.Whatever1
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
