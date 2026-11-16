namespace Pos.Domain;

public sealed class DomainException(string message) : Exception(message);

// Value Object: у балла нет идентичности, сравниваем по значению.
public readonly record struct Score
{
    public decimal Value { get; }

    public Score(decimal value)
    {
        if (value < 0)
            throw new DomainException("Балл не может быть отрицательным.");
        Value = value;
    }
}

public sealed class Student
{
    public Guid Id { get; }
    public string Name { get; }

    internal Student(Guid id, string name)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(name))
            throw new DomainException("У студента должны быть идентификатор и имя.");
        Id = id;
        Name = name.Trim();
    }
}

public sealed class Criterion
{
    public Guid Id { get; }
    public string Name { get; }
    public decimal MaxScore { get; }
    public decimal Weight { get; }

    internal Criterion(Guid id, string name, decimal maxScore, decimal weight)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(name))
            throw new DomainException("У критерия должны быть идентификатор и название.");
        if (maxScore <= 0 || weight <= 0)
            throw new DomainException("Максимум и вес критерия должны быть больше нуля.");
        Id = id;
        Name = name.Trim();
        MaxScore = maxScore;
        Weight = weight;
    }
}

public sealed class Evaluation
{
    public Guid StudentId { get; }
    public Guid CriterionId { get; }
    public Score Score { get; }
    public string Comment { get; }

    internal Evaluation(Guid studentId, Guid criterionId, Score score, string comment)
    {
        StudentId = studentId;
        CriterionId = criterionId;
        Score = score;
        Comment = comment;
    }
}
