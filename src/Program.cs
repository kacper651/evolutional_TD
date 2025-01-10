
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

        var (mapParams, stopParams, algParams, metParams) = EvolutionaryAlgorithm.GetParameters();
        var generator = new EvolutionaryAlgorithm(mapParams, stopParams, algParams, metParams);
        generator.Run();

        var matrix = generator.Population.Last();

        Application.Run(new Visualizer(matrix));
    }    
}