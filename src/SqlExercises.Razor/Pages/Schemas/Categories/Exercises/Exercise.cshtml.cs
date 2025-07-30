using System.Data;
using System.Text;
using Dapper;
using K4os.Hash.xxHash;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Schemas.Categories.Exercises;

// todo: fix this
[IgnoreAntiforgeryToken]
public class ExerciseModel(ILogger<ExerciseModel> logger, DapperContext context) : PageModel
{
    // For GET
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Category { get; set; } = default!;

    public ExerciseDto Exercise { get; set; } = default!;
    public List<Dictionary<string, object>> ExpectedResult { get; set; } = default!;

    // For POST
    [BindProperty]
    public string PostedSolution { get; set; } = default!;

    public async Task<IActionResult> OnGet()
    {
        using var connection = context.CreateConnection();
        var sql = """
                SELECT id, question, explanation, hint, solution, title
                FROM exercise
                WHERE id = @id
            """;
        var result = await connection.QuerySingleAsync<ExerciseDto>(sql, new { id = Id });
        if (result is null)
            return NotFound();
        Exercise = result;

        var expectedResults = await connection.QueryAsync(result.Solution);
        var typeSafe = expectedResults
            .Select(row => new Dictionary<string, object>((IDictionary<string, object>)row))
            .ToList();
        ExpectedResult = typeSafe;

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        // TODO: fix duplicate column name.
        if (PostedSolution is null)
            return new JsonResult(new { result = "No solution provided.", isEqual = false });

        using var connection = context.CreateConnection();
        var sql = "SELECT solution FROM exercise WHERE id = @id";
        var solution = await connection.QuerySingleAsync<string>(sql, new { id = Id });
        var solutionResult = await connection.QueryAsync(solution);
        var solutionString = StringifyDynamicList(solutionResult);
        byte[] solutionData = Encoding.UTF8.GetBytes(solutionString);
        var solutionHash = XXH64.DigestOf(solutionData);

        IEnumerable<dynamic> resultRows;
        try
        {
            logger.LogInformation("Executing sql solution:\n{solution}", PostedSolution);
            resultRows = await connection.QueryAsync(PostedSolution);
        }
        catch (Exception ex)
        {
            logger.LogInformation(
                "sql solution error for exercise {ExerciseId}:\n{Exception}:{Message}",
                ex,
                ex.Message,
                Id
            );
            return new JsonResult(new { result = ex.Message, isEqual = false });
        }
        var resultString = StringifyDynamicList(resultRows);
        byte[] resultData = Encoding.UTF8.GetBytes(resultString);
        var resultHash = XXH64.DigestOf(resultData);

        var isEqual = solutionHash.Equals(resultHash);

        return new JsonResult(new { result = resultRows, isEqual });
    }

    private static string StringifyDynamicList(IEnumerable<dynamic> dynamics)
    {
        var typeSafe = dynamics
            .Select(row => new Dictionary<string, object>((IDictionary<string, object>)row))
            .ToList();

        StringBuilder solString = new();
        foreach (var row in typeSafe)
        {
            foreach (var column in row)
                solString.Append($"{column.Value}");
        }

        return solString.ToString();
    }

    public class ExerciseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string Question { get; set; } = default!;
        public string Explanation { get; set; } = default!;
        public string Hint { get; set; } = default!;
        public string Solution { get; set; } = default!;
    }
}
