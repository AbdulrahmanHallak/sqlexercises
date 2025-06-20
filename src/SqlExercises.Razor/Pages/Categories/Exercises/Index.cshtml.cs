using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Categories.Exercises;

public class ExercisesModel(DapperContext context) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }
    public IReadOnlyCollection<ExerciseDto> Exercises { get; set; } = default!;

    public async Task<IActionResult> OnGet(string category)
    {
        if (category is null)
            return NotFound();

        using var connection = context.CreateConnection();
        var sql = """
                SELECT ex.id, ex.question
                FROM exercise ex
                INNER JOIN category cat
                ON cat.id = ex.category_id
                WHERE cat.name = @name
            """;
        var exercises = await connection.QueryAsync<ExerciseDto>(sql, new { name = category });
        Exercises = [.. exercises];
        return Page();
    }

    public class ExerciseDto
    {
        public int Id { get; set; }
        public string Question { get; set; } = default!;
    }
}
