using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Categories;

public class IndexModel(DapperContext context) : PageModel
{
    public IReadOnlyCollection<string> Categories { get; set; } = default!;

    public bool IsAdmin { get; set; } = false;

    [BindProperty]
    public string? NewCategoryName { get; set; }

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

    public async Task<IActionResult> OnPostAsync()
    {
        Request.Cookies.TryGetValue("is_admin", out var isAdmin);
        if (isAdmin == "true")
            IsAdmin = true;

        if (!string.IsNullOrWhiteSpace(NewCategoryName))
        {
            using var connection = context.CreateConnection();
            var sql = "INSERT INTO category (name) VALUES (@Name) ON CONFLICT DO NOTHING;";
            await connection.ExecuteAsync(sql, new { Name = NewCategoryName.Trim() });
        }

        // Reload categories
        using (var connection = context.CreateConnection())
        {
            var sql = "SELECT name FROM category";
            var categories = await connection.QueryAsync<string>(sql);
            Categories = [.. categories];
        }
        return Page();
    }
}
