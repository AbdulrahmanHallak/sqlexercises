using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SqlExercises.Razor.Pages.Shared.Filters;

namespace SqlExercises.Razor.Pages.Schemas;

[SearchPath]
public class PlaygroundModel(ILogger<PlaygroundModel> logger, DapperContext context) : PageModel
{
    [BindProperty]
    public string Sql { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Schema { get; set; } = default!;

    public async Task<IActionResult> OnGet()
    {
        using var connection = context.CreateConnection();
        var sql = "SELECT EXISTS(SELECT 1 FROM user_schema WHERE schema_name iLIKE @schema)";
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

            logger.LogInformation("Executing sql:\n{sql}", Sql);

            using var connection = context.CreateSolutionConnection();
            var results = (await connection.QueryAsync(Sql)).ToArray();
            return new JsonResult(new SqlResult { Results = results });
        }
        catch (Exception ex)
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
        public object[] Results { get; set; } = default!;
        public string? Error { get; set; }
    }
}
