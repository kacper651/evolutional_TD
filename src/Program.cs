using System.Drawing.Drawing2D;
using EvolAlgorithm.Utils;

namespace evolutional_TD;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    { 
        ApplicationConfiguration.Initialize();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var (mapParams, stopParams, algParams, metParams, poolParams) = Utils.GetParameters();
        var generator = new EvolutionaryAlgorithm(mapParams, stopParams, algParams, metParams, poolParams);

        generator.Run();
        var matrix = generator.Population.Last();
        Application.Run(new Visualizer(matrix));

        //  testing pooling methods below that visualise how they work - they work as intended, but everything converges to a monotone map with only paths if CrossoverOverlayPools is chosen 

        // tests for InitializePools - works fine
        //var pop = generator.InitializePools();
        //for (int i = 0; i < 5; i++)
        //{
        //    Application.Run(new Visualizer(pop[i]));
        //}

        // tests for PenalizeIncorrectPooling - works fine
        // for better logging uncomment console logs inside the method
        //var pop = generator.InitializePools();
        //var individual = pop[0];
        //var score = FitnessUtils.PenalizeIncorrectPooling(
        //    individual, generator.UnavailableGroundPoolCount,
        //    generator.WaterPoolCount, generator.UnavailableGroundPoolMinArea,
        //    generator.UnavailableGroundPoolMaxArea, generator.WaterPoolMinArea,
        //    generator.WaterPoolMaxArea, generator.PathMinArea,
        //    generator.PathMaxArea
        //);
        //Application.Run(new Visualizer(individual));

        // tests for CrossoverOverlayPools - works fine
        //var pop = generator.InitializePools();
        //var p1 = pop[0];
        //var (p1Fit, p1FitMatrix) = generator.Fitness(p1);
        //var p2 = pop[1];
        //var (p2Fit, p2FitMatrix) = generator.Fitness(p2);
        //var child = generator.CrossoverOverlayPools((p1, p1FitMatrix), (p2, p2FitMatrix));
        //Application.Run(new Visualizer(p1));
        //Application.Run(new Visualizer(p2));
        //Application.Run(new Visualizer(child));
    }
}