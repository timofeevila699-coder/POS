using Pos.Application;
using Pos.Domain;
using Pos.Infrastructure;
using Xunit;

namespace Pos.Tests;

public sealed class AdapterTests
{
    [Fact]
    public void JsonRoundTripPreservesStudentsCriteriaAndEvaluations()
    {
        string directory = Path.Combine(Path.GetTempPath(), "pos-tests-" + Guid.NewGuid());
        try
        {
            var factory = new EvaluationFactory();
            var repository = new JsonGradebookRepository(Path.Combine(directory, "book.json"), factory);
            var book = repository.Load();
            var student = book.AddStudent("Анна");
            var criterion = book.AddCriterion("Экзамен", 10, 1);
            book.SetEvaluation(student.Id, criterion.Id, 8, factory, "Хорошо");
            repository.Save(book);
            var restored = repository.Load();
            Assert.Equal(student.Id, Assert.Single(restored.Students).Id);
            Assert.Equal(criterion.Id, Assert.Single(restored.Criteria).Id);
            Assert.Equal("Хорошо", Assert.Single(restored.Evaluations).Comment);
            Assert.Equal(8m, new FinalScoreService(new WeightedScoreStrategy()).Calculate(restored, student.Id));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void CsvEscapesQuotesSeparatorsAndNewlines()
    {
        string path = Path.Combine(Path.GetTempPath(), "pos-test-" + Guid.NewGuid() + ".csv");
        try
        {
            new CsvResultsExporter().Export([new ResultRow("Анна; \"А\"\nИванова", 8.6m,
                [new CriterionResult("Экзамен; \"1\"", 10, 60, 9, "Хорошо; да")])], path);
            string csv = File.ReadAllText(path);
            Assert.Contains("\"Анна; \"\"А\"\"\nИванова\";\"Экзамен; \"\"1\"\"\";9;10;60;\"Хорошо; да\";8,60", csv);
        }
        finally { File.Delete(path); }
    }
}
