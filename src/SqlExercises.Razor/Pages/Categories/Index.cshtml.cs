using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Categories;

public class IndexModel(DapperContext context) : PageModel
{
    public IReadOnlyCollection<string> Categories { get; set; } = default!;

    public async Task<IActionResult> OnGet()
    {
        using var connection = context.CreateConnection();
        var sql = "SELECT name FROM category";
        var categories = await connection.QueryAsync<string>(sql);
        Categories = [.. categories];
        return Page();
    }
}
