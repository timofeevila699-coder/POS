namespace Pos.Domain;

// Корень агрегата: ведомость одного курса. Коллекции меняются только его методами.
public sealed class Gradebook
{
    private readonly List<Student> students = [];
    private readonly List<Criterion> criteria = [];
    private readonly List<Evaluation> evaluations = [];

    public IReadOnlyList<Student> Students => students.AsReadOnly();
    public IReadOnlyList<Criterion> Criteria => criteria.AsReadOnly();
    public IReadOnlyList<Evaluation> Evaluations => evaluations.AsReadOnly();

    public Student AddStudent(string name, Guid? id = null)
    {
        var student = new Student(id ?? Guid.NewGuid(), name);
        if (students.Any(s => s.Id == student.Id))
            throw new DomainException("Студент с таким идентификатором уже есть.");
        students.Add(student);
        return student;
    }

    public Criterion AddCriterion(string name, decimal maxScore, decimal weight, Guid? id = null)
    {
        var criterion = new Criterion(id ?? Guid.NewGuid(), name, maxScore, weight);
        if (criteria.Any(c => c.Id == criterion.Id ||
            c.Name.Equals(criterion.Name, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("Такой критерий уже есть в рубрикаторе.");
        criteria.Add(criterion);
        return criterion;
    }

    public Student GetStudent(Guid id) => students.FirstOrDefault(s => s.Id == id)
        ?? throw new DomainException("Студент не найден.");

    public Criterion GetCriterion(Guid id) => criteria.FirstOrDefault(c => c.Id == id)
        ?? throw new DomainException("Критерия нет в рубрикаторе.");

    public void SetEvaluation(Guid studentId, Guid criterionId, decimal score,
        EvaluationFactory factory, string comment = "")
    {
        // Сначала проверяем новую оценку: ошибочный ввод не удалит прежнюю.
        Evaluation evaluation = factory.Create(this, studentId, criterionId, score, comment);
        evaluations.RemoveAll(e => e.StudentId == studentId && e.CriterionId == criterionId);
        evaluations.Add(evaluation);
    }

    public void CancelEvaluation(Guid studentId, Guid criterionId)
    {
        GetStudent(studentId);
        GetCriterion(criterionId);
        if (evaluations.RemoveAll(e => e.StudentId == studentId && e.CriterionId == criterionId) == 0)
            throw new DomainException("Эта оценка ещё не выставлена.");
    }
}
