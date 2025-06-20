using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages;

[IgnoreAntiforgeryToken]
public class TestingModel(DapperContext context) : PageModel
{
    [BindProperty]
    public string Sql { get; set; } = string.Empty;

    public void OnGet() { }

    public class SqlResult
    {
        public object[] Results { get; set; }
        public string? Error { get; set; }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Sql))
                return new JsonResult(
                    new SqlResult { Results = Array.Empty<object>(), Error = "No SQL provided." }
                );
            using var connection = context.CreateConnection();
            var results = (await connection.QueryAsync(Sql)).ToArray();
            return new JsonResult(new SqlResult { Results = results });
        }
        catch (Exception ex)
        {
            return new JsonResult(
                new SqlResult { Results = Array.Empty<object>(), Error = ex.Message }
            );
        }
    }
}
