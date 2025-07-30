using System.ComponentModel.DataAnnotations;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Schemas.Categories.Exercises;

public class CreateModel(DapperContext context) : PageModel
{
    [BindProperty]
    public ExerciseDto Exercise { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string Category { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string Schema { get; set; } = default!;

    public int CategoryId { get; set; }
    public int UserSchemaId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var sql = """
                SELECT (SELECT id FROM category WHERE name iLIKE @category) AS category_id,
                (SELECT id FROM user_schema WHERE schema_name iLIKE @schema) AS schema_id
            """;
        using var connection = context.CreateConnection();
        (var categoryId, var schemaId) = await connection.QueryFirstAsync<(
            int? CategoryId,
            int? SchemaId
        )>(sql, new { Category, Schema });

        if (categoryId is null || schemaId is null)
            return NotFound();

        UserSchemaId = (int)schemaId;
        CategoryId = (int)categoryId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        using (var connection = context.CreateConnection())
        {
            var sql = """
                INSERT INTO exercise (title, question, solution, explanation, hint, category_id, user_schema_id)
                VALUES (@Title, @Question, @Solution, @Explanation, @Hint, @CategoryId, @UserSchemaId)
                """;
            await connection.ExecuteAsync(sql, Exercise);
        }

        return RedirectToPage("./Index", new { category = Category });
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

        [Required]
        public int UserSchemaId { get; set; }
    }
}
