using System.Globalization;
using System.Text;
using Pos.Application;

namespace Pos.Infrastructure;

public sealed class CsvResultsExporter : IResultsExporter
{
    public void Export(IReadOnlyList<ResultRow> results, string path)
    {
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        using var writer = new StreamWriter(fullPath, false, new UTF8Encoding(true));
        writer.WriteLine("Студент;Критерий;Балл;Максимум;Вес;Комментарий;Итог из 10");
        foreach (var row in results)
        {
            string total = row.FinalScore.ToString("0.00", CultureInfo.GetCultureInfo("ru-RU"));
            if (row.Criteria.Count == 0)
                writer.WriteLine($"{Escape(row.StudentName)};{Escape("Нет критериев") };;;;;{total}");
            foreach (var criterion in row.Criteria)
            {
                string score = criterion.Score.HasValue ? Number(criterion.Score.Value) : "Не выставлена";
                writer.WriteLine($"{Escape(row.StudentName)};{Escape(criterion.Name)};{score};" +
                    $"{Number(criterion.MaxScore)};{Number(criterion.Weight)};{Escape(criterion.Comment)};{total}");
            }
        }
    }

    private static string Number(decimal value) => value.ToString(CultureInfo.GetCultureInfo("ru-RU"));

    private static string Escape(string value)
    {
        // Excel не должен воспринимать имя как формулу.
        if (value.Length > 0 && "=+-@".Contains(value[0])) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
