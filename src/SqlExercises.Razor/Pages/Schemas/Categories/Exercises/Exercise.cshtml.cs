using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using SqlExercises.Razor.Pages.Shared.Filters;

namespace SqlExercises.Razor.Pages.Schemas.Categories.Exercises;

[SearchPath]
public class ExerciseModel(
    ILogger<ExerciseModel> logger,
    AppDapperContext ctx,
    UserSqlRunner runner,
    SolutionChecker checker
) : PageModel
{
    // For GET
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Category { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string Schema { get; set; } = default!;

    public ExerciseDto Exercise { get; set; } = default!;
    public List<Dictionary<string, object>> ExpectedResult { get; set; } = default!;

    public async Task<IActionResult> OnGet()
    {
        using (var connection = ctx.CreateConnection())
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

        var expectedResults = await runner.QueryReadOnly(
            Schema,
            Exercise.Solution,
            async (connection, transaction, sql) =>
            {
                return await connection.QueryAsync(sql, transaction: transaction);
            }
        );
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
        // TODO: see if you can make the await at the end of the methods.
        if (postedSolution is null)
            return new JsonResult(new { result = "No solution provided.", isEqual = false });

        var isValid = ValidSql.TryCreate(postedSolution, out var validSql);
        if (!isValid)
            return new JsonResult(new { Result = "restricted sql.", Error = "not allowed sql" });

        string solution;
        using (var connection = ctx.CreateConnection())
        {
            var sql = "SELECT solution FROM exercise WHERE id = @id";
            solution = await connection.QuerySingleAsync<string>(sql, new { id = Id });
        }

        IEnumerable<dynamic> solutionResult = await runner.QueryReadOnly(
            Schema,
            solution,
            async (connection, transaction, solution) => await connection.QueryAsync(solution)
        );

        IEnumerable<dynamic> resultRows;
        try
        {
            logger.LogInformation("Executing sql solution:\n{solution}", postedSolution);
            resultRows = await runner.QueryReadOnly(
                Schema,
                postedSolution,
                async (connection, transaction, sql) => await connection.QueryAsync(sql)
            );
        }
        catch (PostgresException ex)
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
