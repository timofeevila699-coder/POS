using System.Globalization;
using Pos.Application;
using Pos.Domain;

namespace Pos.ConsoleApp;

public sealed class ConsoleMenu(PosFacade app)
{
    public void Run()
    {
        Console.WriteLine("ПОС — преподавательская оценка студентов");
        while (true)
        {
            Console.WriteLine("\n1. Добавить студента  2. Студенты  3. Добавить критерий  4. Критерии");
            Console.WriteLine("5. Выставить/исправить оценку  6. Оценки  7. Отменить оценку");
            Console.WriteLine("8. Итоги и аналитика  9. Экспорт CSV  0. Выход");
            Console.Write("> ");
            string? command = Console.ReadLine();
            if (command is null or "0") return;
            try
            {
                switch (command)
                {
                    case "1": app.AddStudent(Read("Имя: ")); break;
                    case "2": ShowStudents(); break;
                    case "3":
                        app.AddCriterion(Read("Название: "), ReadDecimal("Максимум: "), ReadDecimal("Вес: "));
                        break;
                    case "4": ShowCriteria(); break;
                    case "5":
                        SetEvaluation();
                        break;
                    case "6": ShowEvaluations(); break;
                    case "7": app.CancelEvaluation(SelectStudent(), SelectCriterion()); break;
                    case "8": ShowResults(); break;
                    case "9":
                        string path = Read("Путь CSV (Enter — results.csv): ");
                        app.ExportResults(string.IsNullOrWhiteSpace(path) ? "results.csv" : path);
                        Console.WriteLine("Итоги экспортированы.");
                        break;
                    default: Console.WriteLine("Выберите пункт от 0 до 9."); break;
                }
            }
            catch (EndOfStreamException) { return; }
            catch (Exception ex) when (ex is DomainException or FormatException or IOException
                or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }

    public void RunDemo()
    {
        Console.WriteLine("Демо: студенты → критерии → оценки → итог → CSV.");
        // Для повторного запуска используем уже созданные записи демо-ведомости.
        Student anna = app.Students.FirstOrDefault(s => s.Name == "Анна Иванова") ?? app.AddStudent("Анна Иванова");
        Student ivan = app.Students.FirstOrDefault(s => s.Name == "Иван Петров") ?? app.AddStudent("Иван Петров");
        Criterion homework = app.Criteria.FirstOrDefault(c => c.Name == "Домашняя работа")
            ?? app.AddCriterion("Домашняя работа", 20, 40);
        Criterion exam = app.Criteria.FirstOrDefault(c => c.Name == "Экзамен")
            ?? app.AddCriterion("Экзамен", 10, 60);
        ShowStudents();
        ShowCriteria();
        app.SetEvaluation(anna.Id, homework.Id, 16, "Хорошая работа");
        app.SetEvaluation(anna.Id, exam.Id, 9);
        app.SetEvaluation(ivan.Id, homework.Id, 10);
        app.SetEvaluation(ivan.Id, exam.Id, 6);
        ShowEvaluations();
        ShowResults();
        app.ExportResults("demo-results.csv");
        Console.WriteLine("Экспорт: demo-results.csv; данные: data/demo.json");
    }

    private void ShowStudents()
    {
        for (int i = 0; i < app.Students.Count; i++)
            Console.WriteLine($"{i + 1}. {app.Students[i].Name}");
        if (app.Students.Count == 0) Console.WriteLine("Студентов пока нет.");
    }

    private void SetEvaluation()
    {
        Guid studentId = SelectStudent();
        Guid criterionId = SelectCriterion();
        decimal maxScore = app.Criteria.First(c => c.Id == criterionId).MaxScore;
        decimal score = ReadDecimal($"Балл (от 0 до {maxScore}): ");
        // Используем ту же доменную проверку до запроса комментария.
        app.ValidateEvaluation(studentId, criterionId, score);
        string comment = Read("Комментарий: ");
        app.SetEvaluation(studentId, criterionId, score, comment);
        Console.WriteLine("Оценка сохранена.");
    }

    private void ShowCriteria()
    {
        for (int i = 0; i < app.Criteria.Count; i++)
        {
            var c = app.Criteria[i];
            Console.WriteLine($"{i + 1}. {c.Name}: максимум {c.MaxScore}, вес {c.Weight}");
        }
        if (app.Criteria.Count == 0) Console.WriteLine("Критериев пока нет.");
    }

    private Guid SelectStudent()
    {
        ShowStudents();
        return app.Students[ReadIndex("Номер студента: ", app.Students.Count)].Id;
    }

    private Guid SelectCriterion()
    {
        ShowCriteria();
        return app.Criteria[ReadIndex("Номер критерия: ", app.Criteria.Count)].Id;
    }

    private void ShowEvaluations()
    {
        foreach (var e in app.Evaluations)
        {
            string name = app.Students.First(s => s.Id == e.StudentId).Name;
            var c = app.Criteria.First(c => c.Id == e.CriterionId);
            Console.WriteLine($"{name} | {c.Name}: {e.Score.Value}/{c.MaxScore} | {e.Comment}");
        }
        if (app.Evaluations.Count == 0) Console.WriteLine("Оценок пока нет.");
    }

    private void ShowResults()
    {
        var results = app.GetResults();
        foreach (var row in results) Console.WriteLine($"{row.StudentName}: {row.FinalScore:0.00}/10");
        if (results.Count == 0) { Console.WriteLine("Студентов пока нет."); return; }
        Console.WriteLine($"Средний итог: {results.Average(r => r.FinalScore):0.00}; " +
            $"минимум: {results.Min(r => r.FinalScore):0.00}; максимум: {results.Max(r => r.FinalScore):0.00}");
    }

    private static string Read(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine() ?? throw new EndOfStreamException();
    }

    private static decimal ReadDecimal(string prompt)
    {
        string text = Read(prompt).Trim().Replace(',', '.');
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out decimal value))
            throw new FormatException("Введите число, например 7,5 или 7.5.");
        return value;
    }

    private static int ReadIndex(string prompt, int count)
    {
        if (count == 0) throw new DomainException("Сначала заполните список.");
        if (!int.TryParse(Read(prompt), out int index) || index < 1 || index > count)
            throw new FormatException("Такого номера в списке нет.");
        return index - 1;
    }
}
