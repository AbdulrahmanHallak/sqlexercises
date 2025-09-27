using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;

namespace SqlExercises.Razor.Pages.Schemas.Exercises;

public class IndexModel(AppDapperContext ctx) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Schema { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    public IReadOnlyCollection<ExerciseDto> Exercises { get; set; } = null!;

    public async Task<IActionResult> OnGet()
    {
        using var connection = ctx.CreateConnection();
        var sql = """
                SELECT ex.id, ex.Title
                FROM exercise ex
                INNER JOIN category cat
                  ON cat.id = ex.category_id
                INNER JOIN user_schema us
                  ON us.id = ex.user_schema_id
                WHERE us.short_name iLIKE @schema
                  AND cat.name iLIKE @category
            """;
        var param = new DynamicParameters();
        param.Add("schema", Schema);
        if (!string.IsNullOrWhiteSpace(Category))
        {
            sql += " AND cat.name iLIKE @category";
            param.Add("category", Category);
        }
        sql += " ORDER BY 1";

        var exercises = await connection.QueryAsync<ExerciseDto>(sql, param: param);
        Exercises = [.. exercises];
        return Page();
    }

    public class ExerciseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
    }
}
