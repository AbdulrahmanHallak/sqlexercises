using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SqlExercises.Razor.Pages.Shared.Filters;

namespace SqlExercises.Razor.Pages.Schemas.Categories.Exercises;

[SearchPath]
public class ExerciseModel(
    ILogger<ExerciseModel> logger,
    DapperContext context,
    SolutionChecker checker
) : PageModel
{
    // For GET
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Category { get; set; } = default!;

    public ExerciseDto Exercise { get; set; } = default!;
    public List<Dictionary<string, object>> ExpectedResult { get; set; } = default!;

    public async Task<IActionResult> OnGet()
    {
        using (var connection = context.CreateConnection())
        {
            var sql = """
                    SELECT id, question, explanation, hint, solution, title
                    FROM exercise
                    WHERE id = @id
                """;
            var result = await connection.QuerySingleAsync<ExerciseDto>(sql, new { id = Id });
            if (result is null)
                return NotFound();
            Exercise = result;
        }
        using var solutionConnection = context.CreateSolutionConnection();

        var expectedResults = await solutionConnection.QueryAsync(Exercise.Solution);
        var typeSafe = expectedResults
            .Select(row => new Dictionary<string, object>((IDictionary<string, object>)row))
            .ToList();
        ExpectedResult = typeSafe;

        return Page();
    }

    public async Task<IActionResult> OnPost(string postedSolution)
    {
        // TODO: fix duplicate column name.
        // TODO: refactor into service class to simplify controller.
        if (postedSolution is null)
            return new JsonResult(new { result = "No solution provided.", isEqual = false });

        string solution;
        using (var connection = context.CreateConnection())
        {
            var sql = "SELECT solution FROM exercise WHERE id = @id";
            solution = await connection.QuerySingleAsync<string>(sql, new { id = Id });
        }

        using var solutionConnection = context.CreateSolutionConnection();

        IEnumerable<dynamic> solutionResult = await solutionConnection.QueryAsync(solution);

        IEnumerable<dynamic> resultRows;
        try
        {
            logger.LogInformation("Executing sql solution:\n{solution}", postedSolution);
            resultRows = await solutionConnection.QueryAsync(postedSolution);
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

        var isEqual = checker.IsCorrect([.. resultRows], [.. solutionResult]);

        return new JsonResult(new { result = resultRows, isEqual });
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
