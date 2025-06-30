using System.ComponentModel.DataAnnotations;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Categories.Exercises;

public class CreateModel(DapperContext context) : PageModel
{
    [BindProperty]
    public ExerciseDto Exercise { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string Category { get; set; } = default!;

    public int CategoryId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var sql = "SELECT id FROM category WHERE name like @category";
        using var connection = context.CreateConnection();
        var id = await connection.QueryFirstAsync<int?>(sql, new { Category });
        if (id is null)
            return NotFound();

        CategoryId = (int)id;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        using (var connection = context.CreateConnection())
        {
            var sql = """
                INSERT INTO exercise (title, question, solution, Explanation, hint, category_id)
                VALUES (@Title, @Question, @Solution, @Explanation, @Hint, @CategoryId)
                """;
            await connection.ExecuteAsync(sql, Exercise);
        }

        return RedirectToPage("/Categories/Exercises/Index", new { category = Category });
    }

    public class ExerciseDto
    {
        [Required]
        public string Title { get; set; } = default!;

        [Required]
        public string Question { get; set; } = default!;

        [Required]
        public string Solution { get; set; } = default!;

        public string? Explanation { get; set; }

        public string? Hint { get; set; }

        [Required]
        public int CategoryId { get; set; }
    }
}
