namespace Pos.Domain;

// Фабрика: проверка связей и создание оценки сосредоточены в одном месте.
public sealed class EvaluationFactory
{
    public Evaluation Create(Gradebook gradebook, Guid studentId, Guid criterionId,
        decimal value, string comment = "")
    {
        gradebook.GetStudent(studentId);
        Criterion criterion = gradebook.GetCriterion(criterionId);
        var score = new Score(value);
        if (score.Value > criterion.MaxScore)
            throw new DomainException($"Балл должен быть от 0 до {criterion.MaxScore}.");
        return new Evaluation(studentId, criterionId, score, comment.Trim());
    }
}
