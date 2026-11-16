namespace Pos.Domain;

// Strategy: одно правило можно заменить другим без изменения доменного сервиса.
public interface IFinalScoreStrategy
{
    decimal Calculate(IReadOnlyList<Criterion> criteria, IReadOnlyList<Evaluation> evaluations);
}

public sealed class WeightedScoreStrategy : IFinalScoreStrategy
{
    public decimal Calculate(IReadOnlyList<Criterion> criteria, IReadOnlyList<Evaluation> evaluations)
    {
        if (criteria.Count == 0) return 0;
        decimal weightSum = criteria.Sum(c => c.Weight);
        return criteria.Sum(c =>
            (evaluations.FirstOrDefault(e => e.CriterionId == c.Id)?.Score.Value ?? 0)
            / c.MaxScore * (c.Weight / weightSum)) * 10;
    }
}

// Альтернатива для сравнения: у всех критериев одинаковый вклад.
public sealed class EqualWeightScoreStrategy : IFinalScoreStrategy
{
    public decimal Calculate(IReadOnlyList<Criterion> criteria, IReadOnlyList<Evaluation> evaluations)
    {
        if (criteria.Count == 0) return 0;
        return criteria.Average(c =>
            (evaluations.FirstOrDefault(e => e.CriterionId == c.Id)?.Score.Value ?? 0)
            / c.MaxScore) * 10;
    }
}

public sealed class FinalScoreService(IFinalScoreStrategy strategy)
{
    public decimal Calculate(Gradebook gradebook, Guid studentId)
    {
        gradebook.GetStudent(studentId);
        var evaluations = gradebook.Evaluations.Where(e => e.StudentId == studentId).ToList();
        return decimal.Round(strategy.Calculate(gradebook.Criteria, evaluations), 2,
            MidpointRounding.AwayFromZero);
    }
}
