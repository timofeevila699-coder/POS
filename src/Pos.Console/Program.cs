using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Pos.Application;
using Pos.Domain;
using Pos.Infrastructure;
using Pos.ConsoleApp;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;
System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");

// Composition root: только здесь выбираются реализации и собирается DI-контейнер.
var services = new ServiceCollection();
services.AddSingleton<EvaluationFactory>();
if (args.Contains("--equal"))
    services.AddSingleton<IFinalScoreStrategy, EqualWeightScoreStrategy>();
else
    services.AddSingleton<IFinalScoreStrategy, WeightedScoreStrategy>();
services.AddSingleton<FinalScoreService>();
string dataPath = args.Contains("--demo") ? "data/demo.json" : "data/gradebook.json";
services.AddSingleton<IGradebookRepository>(provider =>
    new JsonGradebookRepository(dataPath, provider.GetRequiredService<EvaluationFactory>()));
services.AddSingleton<IResultsExporter, CsvResultsExporter>();
services.AddSingleton<PosFacade>();
services.AddSingleton<ConsoleMenu>();

try
{
    using var provider = services.BuildServiceProvider();
    var menu = provider.GetRequiredService<ConsoleMenu>();
    Console.WriteLine(args.Contains("--equal") ? "Расчёт: равные веса." : "Расчёт: веса рубрикатора.");
    if (args.Contains("--demo")) menu.RunDemo();
    else menu.Run();
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
    or System.Text.Json.JsonException or DomainException or InvalidDataException)
{
    Console.Error.WriteLine($"Не удалось запустить приложение: {ex.Message}");
    Environment.ExitCode = 1;
}
