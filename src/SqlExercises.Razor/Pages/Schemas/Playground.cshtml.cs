using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using SqlExercises.Razor.Pages.Shared.Filters;

namespace SqlExercises.Razor.Pages.Schemas;

[SearchPath]
public class PlaygroundModel(
    ILogger<PlaygroundModel> logger,
    AppDapperContext defaultCtx,
    UserSqlRunner runner
) : PageModel
{
    [BindProperty]
    public string Sql { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Schema { get; set; } = null!;

    public async Task<IActionResult> OnGet()
    {
        using var connection = defaultCtx.CreateConnection();
        var sql = "SELECT EXISTS(SELECT 1 FROM user_schema WHERE short_name iLIKE @schema)";
        var schemaExists = await connection.QuerySingleAsync<bool>(sql, new { Schema });
        if (!schemaExists)
            return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Sql))
                return new JsonResult(new SqlResult { Results = [], Error = "No SQL provided" });

            var isValid = ValidSql.TryCreate(Sql, out var validSql);
            if (!isValid)
                return new JsonResult(new SqlResult { Results = [], Error = "not allowed sql" });

            logger.LogInformation("Executing sql on schema {schema}:\n{sql}", Sql, Schema);
            var results = await runner.QueryReadOnly(
                Schema,
                validSql!,
                async (connection, transaction, sql) =>
                    await connection.QueryAsync(sql, transaction: transaction)
            );

            return new JsonResult(new SqlResult { Results = [.. results] });
        }
        catch (PostgresException ex)
        {
            logger.LogInformation(
                "sql error on {Schema}:\n{Exception}:{Message}",
                ex,
                ex.Message,
                Schema
            );
            return new JsonResult(new SqlResult { Results = [], Error = ex.Message });
        }
    }

    public class SqlResult
    {
        public object[] Results { get; set; } = null!;
        public string? Error { get; set; }
    }
}
