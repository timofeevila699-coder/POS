using Pos.Domain;

namespace Pos.Application;

public interface IGradebookRepository
{
    Gradebook Load();
    void Save(Gradebook gradebook);
}

public record CriterionResult(string Name, decimal MaxScore, decimal Weight,
    decimal? Score, string Comment);

public record ResultRow(string StudentName, decimal FinalScore,
    IReadOnlyList<CriterionResult> Criteria);

public interface IResultsExporter
{
    void Export(IReadOnlyList<ResultRow> results, string path);
}
