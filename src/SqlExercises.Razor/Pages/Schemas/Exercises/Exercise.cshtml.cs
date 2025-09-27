using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using SqlExercises.Razor.Pages.Shared.Filters;

namespace SqlExercises.Razor.Pages.Schemas.Exercises;

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
    public string Category { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public string Schema { get; set; } = null!;

    public ExerciseDto Exercise { get; set; } = null!;
    public List<Dictionary<string, object>> ExpectedResult { get; set; } = null!;

    public async Task<IActionResult> OnGet()
    {
        using (var connection = ctx.CreateConnection())
        {
            var sql = """
                    SELECT ex.id, ex.question, ex.explanation, ex.hint, ex.solution, ex.title
                    FROM exercise ex
                    INNER JOIN user_schema us
                      ON us.id = ex.user_schema_id
                    INNER JOIN category cat
                      ON cat.id = ex.category_id

                    WHERE ex.id = @id
                      AND cat.name iLIKE @category
                      AND us.short_name iLIKE @schema
                """;
            var result = await connection.QuerySingleOrDefaultAsync<ExerciseDto?>(
                sql,
                new
                {
                    id = Id,
                    Category,
                    Schema,
                }
            );
            if (result is null)
                return NotFound();
            Exercise = result;
        }

        var expectedResults = await runner.QueryReadOnly(
            Schema,
            Exercise.Solution,
            async (connection, transaction, sql) =>
                await connection.QueryAsync(sql, transaction: transaction)
        );

        var typeSafe = expectedResults
            .Select(row => new Dictionary<string, object>((IDictionary<string, object>)row))
            .ToList();
        ExpectedResult = typeSafe;

        return Page();
    }

    public async Task<IActionResult> OnPost(string? postedSolution)
    {
        // TODO: refactor into service class to simplify controller.
        // TODO: see if you can make the await at the end of the methods.
        if (postedSolution is null)
            return new JsonResult(new { result = "No solution provided.", isEqual = false });

        var isValid = ValidSql.TryCreate(postedSolution, out var solutionSql);
        if (!isValid)
            return new JsonResult(new { Result = "restricted sql.", Error = "not allowed sql" });

        string? solution;
        using (var connection = ctx.CreateConnection())
        {
            const string sql = """
                    SELECT ex.solution
                    FROM exercise ex
                    INNER JOIN user_schema us
                      ON us.id = ex.user_schema_id
                    INNER JOIN category cat
                      ON cat.id = ex.category_id

                    WHERE ex.id = @id
                      AND cat.name iLIKE @category
                      AND us.short_name iLIKE @schema
                """;
            solution = await connection.QuerySingleOrDefaultAsync<string?>(
                sql,
                new
                {
                    id = Id,
                    Category,
                    Schema,
                }
            );
            if (solution is null)
                return NotFound();
        }

        IEnumerable<dynamic> solutionResult = await runner.QueryReadOnly(
            Schema,
            solution,
            async (connection, transaction, sql) =>
                await connection.QueryAsync(sql, transaction: transaction)
        );

        IEnumerable<dynamic> resultRows;
        try
        {
            logger.LogInformation("Executing sql solution:\n{solution}", solutionSql!);
            resultRows = await runner.QueryReadOnly(
                Schema,
                solutionSql!,
                async (connection, transaction, sql) =>
                    await connection.QueryAsync(sql, transaction: transaction)
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
        public string Title { get; set; } = null!;
        public string Question { get; set; } = null!;
        public string Explanation { get; set; } = null!;
        public string Hint { get; set; } = null!;
        public string Solution { get; set; } = null!;
    }
}
