using Pos.Domain;
using Xunit;

namespace Pos.Tests;

public sealed class DomainTests
{
    private readonly Gradebook book = new();
    private readonly EvaluationFactory factory = new();

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void EvaluationOutsideRangeIsRejected(int value)
    {
        var student = book.AddStudent("Анна");
        var criterion = book.AddCriterion("Экзамен", 10, 1);
        Assert.Throws<DomainException>(() => book.SetEvaluation(student.Id, criterion.Id, value, factory));
        Assert.Empty(book.Evaluations);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void BoundaryScoresAreAccepted(int value)
    {
        var student = book.AddStudent("Анна");
        var criterion = book.AddCriterion("Экзамен", 10, 1);
        book.SetEvaluation(student.Id, criterion.Id, value, factory);
        Assert.Equal((decimal)value, Assert.Single(book.Evaluations).Score.Value);
    }

    [Fact]
    public void CriterionFromAnotherRubricIsRejected()
    {
        var student = book.AddStudent("Анна");
        var otherBook = new Gradebook();
        var foreignCriterion = otherBook.AddCriterion("Экзамен", 10, 1);
        Assert.Throws<DomainException>(() =>
            book.SetEvaluation(student.Id, foreignCriterion.Id, 5, factory));
    }

    [Fact]
    public void UnknownStudentIsRejected()
    {
        var criterion = book.AddCriterion("Экзамен", 10, 1);
        Assert.Throws<DomainException>(() => book.SetEvaluation(Guid.NewGuid(), criterion.Id, 5, factory));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(10, 0)]
    [InlineData(10, -1)]
    public void InvalidCriterionIsRejected(int max, int weight)
    {
        Assert.Throws<DomainException>(() => book.AddCriterion("Экзамен", max, weight));
    }

    [Fact]
    public void DuplicateCriterionIsRejected()
    {
        book.AddCriterion("Экзамен", 10, 1);
        Assert.Throws<DomainException>(() => book.AddCriterion(" экзамен ", 20, 2));
    }

    [Fact]
    public void StudentNeedsName()
    {
        Assert.Throws<DomainException>(() => book.AddStudent("  "));
    }

    [Fact]
    public void ScoresAreValueObjects()
    {
        Assert.Equal(new Score(5), new Score(5));
        Assert.NotEqual(new Score(5), new Score(6));
    }

    [Fact]
    public void WeightedTotalUsesNormalizedScoresAndWeights()
    {
        var student = book.AddStudent("Анна");
        var homework = book.AddCriterion("ДЗ", 20, 40);
        var exam = book.AddCriterion("Экзамен", 10, 60);
        book.SetEvaluation(student.Id, homework.Id, 16, factory);
        book.SetEvaluation(student.Id, exam.Id, 9, factory);
        Assert.Equal(8.6m, Total(student.Id));
        var equal = new FinalScoreService(new EqualWeightScoreStrategy());
        Assert.Equal(8.5m, equal.Calculate(book, student.Id));
    }

    [Fact]
    public void MissingEvaluationCountsAsZero()
    {
        var student = book.AddStudent("Анна");
        var homework = book.AddCriterion("ДЗ", 20, 40);
        book.AddCriterion("Экзамен", 10, 60);
        book.SetEvaluation(student.Id, homework.Id, 20, factory);
        Assert.Equal(4m, Total(student.Id));
    }

    [Fact]
    public void TotalIsRecalculatedAfterCorrectionAndCancellation()
    {
        var student = book.AddStudent("Анна");
        var criterion = book.AddCriterion("Экзамен", 10, 1);
        book.SetEvaluation(student.Id, criterion.Id, 5, factory);
        book.SetEvaluation(student.Id, criterion.Id, 9, factory);
        Assert.Single(book.Evaluations);
        Assert.Equal(9m, Total(student.Id));
        book.CancelEvaluation(student.Id, criterion.Id);
        Assert.Equal(0m, Total(student.Id));
    }

    [Fact]
    public void InvalidCorrectionKeepsPreviousEvaluation()
    {
        var student = book.AddStudent("Анна");
        var criterion = book.AddCriterion("Экзамен", 10, 1);
        book.SetEvaluation(student.Id, criterion.Id, 8, factory);
        Assert.Throws<DomainException>(() => book.SetEvaluation(student.Id, criterion.Id, 12, factory));
        Assert.Equal(8m, Total(student.Id));
    }

    [Fact]
    public void EmptyRubricHasZeroTotal()
    {
        var student = book.AddStudent("Анна");
        Assert.Equal(0m, Total(student.Id));
    }

    [Fact]
    public void EvaluationsOfOtherStudentsDoNotAffectTotal()
    {
        var anna = book.AddStudent("Анна");
        var ivan = book.AddStudent("Иван");
        var criterion = book.AddCriterion("Экзамен", 10, 1);
        book.SetEvaluation(anna.Id, criterion.Id, 10, factory);
        Assert.Equal(0m, Total(ivan.Id));
    }

    private decimal Total(Guid studentId) =>
        new FinalScoreService(new WeightedScoreStrategy()).Calculate(book, studentId);
}
