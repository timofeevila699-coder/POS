using Pos.Application;
using Pos.ConsoleApp;
using Pos.Domain;
using Pos.Infrastructure;
using Xunit;

namespace Pos.Tests;

public sealed class RegressionTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "pos-regression-" + Guid.NewGuid());
    private readonly PosFacade app;
    private readonly JsonGradebookRepository repository;

    public RegressionTests()
    {
        var factory = new EvaluationFactory();
        repository = new JsonGradebookRepository(Path.Combine(directory, "book.json"), factory);
        app = new PosFacade(repository, new CsvResultsExporter(), factory,
            new FinalScoreService(new WeightedScoreStrategy()));
    }

    [Theory]
    [InlineData("12")]
    [InlineData("-1")]
    public void InvalidScoreIsReportedBeforeCommentAndDoesNotReplaceSavedScore(string score)
    {
        var student = app.AddStudent("Анна");
        var criterion = app.AddCriterion("Экзамен", 10, 1);
        app.SetEvaluation(student.Id, criterion.Id, 8);
        TextReader originalInput = Console.In;
        TextWriter originalOutput = Console.Out;
        using var input = new StringReader($"5\n1\n1\n{score}\n0\n");
        using var output = new StringWriter();
        try
        {
            Console.SetIn(input);
            Console.SetOut(output);
            new ConsoleMenu(app).Run();
        }
        finally
        {
            Console.SetIn(originalInput);
            Console.SetOut(originalOutput);
        }
        Assert.Contains("Ошибка:", output.ToString());
        Assert.DoesNotContain("Комментарий:", output.ToString());
        Assert.Equal(8m, Assert.Single(app.Evaluations).Score.Value);
        Assert.Equal(8m, Assert.Single(repository.Load().Evaluations).Score.Value);
    }

    [Fact]
    public void ExportContainsEachCriterionAndDistinguishesMissingScoreFromZero()
    {
        var student = app.AddStudent("Анна");
        var homework = app.AddCriterion("ДЗ", 20, 40);
        var exam = app.AddCriterion("Экзамен", 10, 60);
        app.SetEvaluation(student.Id, homework.Id, 16, "Хорошо");
        string path = Path.Combine(directory, "results.csv");
        app.ExportResults(path);
        string csv = File.ReadAllText(path);
        Assert.Contains("Студент;Критерий;Балл;Максимум;Вес;Комментарий;Итог из 10", csv);
        Assert.Contains("\"Анна\";\"ДЗ\";16;20;40;\"Хорошо\";3,20", csv);
        Assert.Contains("\"Анна\";\"Экзамен\";Не выставлена;10;60;\"\";3,20", csv);
        app.SetEvaluation(student.Id, exam.Id, 0);
        app.ExportResults(path);
        Assert.Contains("\"Экзамен\";0;10;60", File.ReadAllText(path));
        app.SetEvaluation(student.Id, exam.Id, 9);
        app.ExportResults(path);
        Assert.Contains("\"Экзамен\";9;10;60;\"\";8,60", File.ReadAllText(path));
        app.CancelEvaluation(student.Id, exam.Id);
        app.ExportResults(path);
        Assert.Contains("\"Экзамен\";Не выставлена;10;60;\"\";3,20", File.ReadAllText(path));
    }

    [Fact]
    public void ExportWithoutCriteriaStillContainsStudentAndTotal()
    {
        app.AddStudent("Анна");
        string path = Path.Combine(directory, "results.csv");
        app.ExportResults(path);
        Assert.Contains("\"Анна\";\"Нет критериев\";;;;;0,00", File.ReadAllText(path));
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
