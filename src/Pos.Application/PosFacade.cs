using Pos.Domain;

namespace Pos.Application;

// Facade: консоль вызывает готовые сценарии, а не собирает их из репозитория и сервисов.
public sealed class PosFacade
{
    private readonly IGradebookRepository repository;
    private readonly IResultsExporter exporter;
    private readonly EvaluationFactory factory;
    private readonly FinalScoreService finalScoreService;
    private Gradebook gradebook;

    public PosFacade(IGradebookRepository repository, IResultsExporter exporter,
        EvaluationFactory factory, FinalScoreService finalScoreService)
    {
        this.repository = repository;
        this.exporter = exporter;
        this.factory = factory;
        this.finalScoreService = finalScoreService;
        gradebook = repository.Load();
    }

    public IReadOnlyList<Student> Students => gradebook.Students;
    public IReadOnlyList<Criterion> Criteria => gradebook.Criteria;
    public IReadOnlyList<Evaluation> Evaluations => gradebook.Evaluations;

    public Student AddStudent(string name)
    {
        Student? result = null;
        Change(book => result = book.AddStudent(name));
        return result!;
    }

    public Criterion AddCriterion(string name, decimal maxScore, decimal weight)
    {
        Criterion? result = null;
        Change(book => result = book.AddCriterion(name, maxScore, weight));
        return result!;
    }

    public void SetEvaluation(Guid studentId, Guid criterionId, decimal score, string comment = "") =>
        Change(book => book.SetEvaluation(studentId, criterionId, score, factory, comment));

    public void ValidateEvaluation(Guid studentId, Guid criterionId, decimal score) =>
        factory.Create(gradebook, studentId, criterionId, score);

    public void CancelEvaluation(Guid studentId, Guid criterionId) =>
        Change(book => book.CancelEvaluation(studentId, criterionId));

    public IReadOnlyList<ResultRow> GetResults() => gradebook.Students
        .Select(s => new ResultRow(s.Name, finalScoreService.Calculate(gradebook, s.Id),
            gradebook.Criteria.Select(c =>
            {
                var evaluation = gradebook.Evaluations.FirstOrDefault(e =>
                    e.StudentId == s.Id && e.CriterionId == c.Id);
                return new CriterionResult(c.Name, c.MaxScore, c.Weight,
                    evaluation?.Score.Value, evaluation?.Comment ?? "");
            }).ToList()))
        .OrderByDescending(r => r.FinalScore).ToList();

    public void ExportResults(string path) => exporter.Export(GetResults(), path);

    private void Change(Action<Gradebook> action)
    {
        // При ошибке записи возвращаемся к сохранённому состоянию.
        try
        {
            action(gradebook);
            repository.Save(gradebook);
        }
        catch
        {
            gradebook = repository.Load();
            throw;
        }
    }
}
