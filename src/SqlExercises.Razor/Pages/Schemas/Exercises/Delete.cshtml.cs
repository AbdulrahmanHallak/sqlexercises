using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Schemas.Exercises;

public class DeleteModel(AppDapperContext ctx) : PageModel
{
    public ExerciseDto? Exercise { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Category { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public string Schema { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // TODO: implement security checks and soft deletes.
        using var connection = ctx.CreateConnection();
        const string sql = """
            SELECT ex.id, ex.question, cat.name AS CategoryName, us.short_name AS SchemaName
            FROM exercise ex
            INNER JOIN category cat
              ON cat.id = ex.category_id
            INNER JOIN user_schema us
              ON us.id = ex.user_schema_id
            WHERE ex.id = @id
              AND cat.name iLIKE @category
              AND us.short_name iLIKE @schema
            """;
        Exercise = (
            await connection.QuerySingleOrDefaultAsync<ExerciseDto?>(
                sql,
                new
                {
                    id,
                    Schema,
                    Category,
                }
            )
        )!;
        if (Exercise is null)
            return NotFound();

        return Page();
    }

    // public async Task<IActionResult> OnPostAsync(int id)
    // {
    //     using var connection = ctx.CreateConnection();
    //     var sql = "DELETE FROM exercise WHERE id = @id;";
    //     await connection.ExecuteAsync(sql, new { id });
    //     return RedirectToPage("./Index", new { Category, Schema });
    // }

    public class ExerciseDto
    {
        public int Id { get; set; }
        public string Question { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string SchemaName { get; set; } = null!;
    }
}
