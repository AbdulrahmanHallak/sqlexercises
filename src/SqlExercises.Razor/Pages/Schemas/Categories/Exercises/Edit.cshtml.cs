using System.ComponentModel.DataAnnotations;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SqlExercises.Razor.Pages.Schemas.Categories.Exercises;

public class EditModel(DapperContext context) : PageModel
{
    [BindProperty]
    public ExerciseDto Exercise { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Schema { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    public IReadOnlyCollection<SelectListItem> Categories { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(Category) || string.IsNullOrWhiteSpace(Schema))
            return NotFound();

        using var connection = context.CreateConnection();
        var sql = """
            SELECT ex.id, ex.title, ex.question, ex.solution, ex.explanation, ex.hint, ex.category_id
            FROM exercise ex
            INNER JOIN category cat
              ON cat.id = ex.category_id
            WHERE cat.name iLIKE @category AND ex.id = @id
            """;
        var exercise = await connection.QuerySingleOrDefaultAsync<ExerciseDto>(
            sql,
            new { id, Category }
        );
        if (exercise == null)
            return NotFound();

        Exercise = exercise;

        // Load available categories for dropdown
        var categoriesSql = """
            SELECT id, name
            FROM category
            ORDER BY name
            """;
        var categories = await connection.QueryAsync<CategoryDto>(categoriesSql);
        Categories = [.. categories.Select(c => new SelectListItem(c.Name, c.Id.ToString()))];

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        using var connection = context.CreateConnection();
        var sql = """
            UPDATE exercise SET
                title = @Title,
                question = @Question,
                solution = @Solution,
                explanation = @Explanation,
                hint = @Hint,
                category_id = @CategoryId
                WHERE id = @Id
            """;
        await connection.ExecuteAsync(sql, Exercise);

        // Get the new category name for redirect
        var categorySql = "SELECT name FROM category WHERE id = @CategoryId";
        var newCategoryName = await connection.QuerySingleAsync<string>(
            categorySql,
            new { Exercise.CategoryId }
        );

        return RedirectToPage("./Index", new { category = newCategoryName ?? Category, Schema });
    }

    public class ExerciseDto
    {
        public int Id { get; set; }

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

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
    }
}
