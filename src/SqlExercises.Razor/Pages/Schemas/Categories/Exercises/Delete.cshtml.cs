using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Schemas.Categories.Exercises;

public class DeleteModel(DapperContext context) : PageModel
{
    public ExerciseDto Exercise { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string Category { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string Schema { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // TODO: implement security checks and soft deletes.
        using var connection = context.CreateConnection();
        var sql = """
            SELECT ex.id, ex.question, cat.name AS CategoryName, us.schema_name AS SchemaName
            FROM exercise ex
            INNER JOIN category cat
              ON cat.id = ex.category_id
            INNER JOIN user_schema us
              ON us.id = ex.user_schema_id
            WHERE
              ex.id = @id AND cat.name iLIKE @category AND us.schema_name iLIKE @schema
            """;
        Exercise = (
            await connection.QuerySingleOrDefaultAsync<ExerciseDto>(
                sql,
                new
                {
                    id,
                    Schema,
                    Category,
                }
            )
        )!;
        if (Exercise == null)
            return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        using var connection = context.CreateConnection();
        var sql = "DELETE FROM exercise WHERE id = @id;";
        await connection.ExecuteAsync(sql, new { id });
        return RedirectToPage("./Index", new { Category, Schema });
    }

    public class ExerciseDto
    {
        public int Id { get; set; }
        public string Question { get; set; } = default!;
        public string CategoryName { get; set; } = default!;
        public string SchemaName { get; set; } = default!;
    }
}
