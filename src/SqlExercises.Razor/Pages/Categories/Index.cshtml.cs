using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Categories;

public class IndexModel(DapperContext context) : PageModel
{
    public IReadOnlyCollection<string> Categories { get; set; } = default!;

    public bool IsAdmin { get; set; } = false;

    public async Task<IActionResult> OnGet()
    {
        Request.Cookies.TryGetValue("is_admin", out var isAdmin);
        if (isAdmin == "true")
            IsAdmin = true;
        using var connection = context.CreateConnection();
        var sql = "SELECT name FROM category";
        var categories = await connection.QueryAsync<string>(sql);
        Categories = [.. categories];
        return Page();
    }
}
