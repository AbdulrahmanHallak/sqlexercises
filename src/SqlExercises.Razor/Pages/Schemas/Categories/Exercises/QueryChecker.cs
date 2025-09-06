using System.Text;
using K4os.Hash.xxHash;

namespace SqlExercises.Razor.Pages.Schemas.Categories.Exercises;

public class SolutionChecker
{
    public bool IsCorrect(dynamic[] submittedSolution, dynamic[] solution)
    {
        var solutionString = StringifyDynamicList(solution);
        byte[] solutionData = Encoding.UTF8.GetBytes(solutionString);
        var solutionHash = XXH64.DigestOf(solutionData);

        var resultString = StringifyDynamicList(submittedSolution);
        byte[] resultData = Encoding.UTF8.GetBytes(resultString);
        var resultHash = XXH64.DigestOf(resultData);

        return solutionHash.Equals(resultHash);
    }

    private static string StringifyDynamicList(dynamic[] solution)
    {
        var typeSafe = solution
            .Select(row => new Dictionary<string, object>((IDictionary<string, object>)row))
            .ToArray();

        StringBuilder solString = new();
        foreach (var row in typeSafe)
        {
            foreach (var column in row)
                solString.Append($"{column.Value}");
        }

        return solString.ToString();
    }
}
