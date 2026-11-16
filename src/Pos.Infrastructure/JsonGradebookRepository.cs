using System.Text.Json;
using Pos.Application;
using Pos.Domain;

namespace Pos.Infrastructure;

public sealed class JsonGradebookRepository(string path, EvaluationFactory factory) : IGradebookRepository
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public Gradebook Load()
    {
        if (!File.Exists(path)) return new Gradebook();
        var data = JsonSerializer.Deserialize<GradebookData>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException("Пустое JSON-хранилище.");
        if (data.Students is null || data.Criteria is null || data.Evaluations is null)
            throw new InvalidDataException("В JSON отсутствуют списки студентов, критериев или оценок.");
        if (data.Students.Any(s => s is null) || data.Criteria.Any(c => c is null)
            || data.Evaluations.Any(e => e is null || e.Comment is null))
            throw new InvalidDataException("В JSON есть пустые записи.");
        if (data.Evaluations.GroupBy(e => (e.StudentId, e.CriterionId)).Any(g => g.Count() > 1))
            throw new InvalidDataException("В JSON есть несколько оценок для одной пары студент/критерий.");
        var book = new Gradebook();
        // Восстановление тоже проходит через инварианты домена.
        foreach (var student in data.Students)
            book.AddStudent(student.Name, student.Id);
        foreach (var criterion in data.Criteria)
            book.AddCriterion(criterion.Name, criterion.MaxScore, criterion.Weight, criterion.Id);
        foreach (var evaluation in data.Evaluations)
            book.SetEvaluation(evaluation.StudentId, evaluation.CriterionId,
                evaluation.Score, factory, evaluation.Comment);
        return book;
    }

    public void Save(Gradebook gradebook)
    {
        var data = new GradebookData(
            gradebook.Students.Select(s => new StudentData(s.Id, s.Name)).ToList(),
            gradebook.Criteria.Select(c => new CriterionData(c.Id, c.Name, c.MaxScore, c.Weight)).ToList(),
            gradebook.Evaluations.Select(e => new EvaluationData(e.StudentId, e.CriterionId,
                e.Score.Value, e.Comment)).ToList());
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        string temporaryPath = fullPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(data, Options));
        File.Move(temporaryPath, fullPath, overwrite: true);
    }

    public sealed record GradebookData(List<StudentData> Students,
        List<CriterionData> Criteria, List<EvaluationData> Evaluations);
    public sealed record StudentData(Guid Id, string Name);
    public sealed record CriterionData(Guid Id, string Name, decimal MaxScore, decimal Weight);
    public sealed record EvaluationData(Guid StudentId, Guid CriterionId, decimal Score, string Comment);
}
