using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages;

[IgnoreAntiforgeryToken]
public class TestingModel(ILogger<TestingModel> logger, DapperContext context) : PageModel
{
    [BindProperty]
    public string Sql { get; set; } = string.Empty;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Sql))
                return new JsonResult(new SqlResult { Results = [], Error = "No SQL provided" });

            logger.LogInformation("Executing sql:\n{sql}", Sql);

            using var connection = context.CreateConnection();
            var results = (await connection.QueryAsync(Sql)).ToArray();
            return new JsonResult(new SqlResult { Results = results });
        }
        catch (Exception ex)
        {
            logger.LogInformation("sql error:\n{Exception}:{Message}", ex, ex.Message);
            return new JsonResult(new SqlResult { Results = [], Error = ex.Message });
        }
    }

    public class SqlResult
    {
        public object[] Results { get; set; }
        public string? Error { get; set; }
    }
}
