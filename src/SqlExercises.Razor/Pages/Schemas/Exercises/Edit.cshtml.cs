using System.ComponentModel.DataAnnotations;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;

namespace SqlExercises.Razor.Pages.Schemas.Exercises;

public class EditModel(AppDapperContext ctx, UserSqlRunner runner) : PageModel
{
    [BindProperty]
    public ExerciseDto Exercise { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string Schema { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public string Category { get; set; } = null!;

    public IReadOnlyCollection<SelectListItem> Categories { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        using var connection = ctx.CreateConnection();
        const string sql = """
            SELECT ex.id, ex.title, ex.question, ex.solution, ex.explanation, ex.hint, ex.category_id
            FROM exercise ex
            INNER JOIN category cat
              ON cat.id = ex.category_id
            INNER JOIN user_schema us
              ON us.id = ex.user_schema_id
            WHERE ex.id = @id
              AND cat.name iLIKE @category
              AND us.short_name iLIKE @schema
            """;
        var exercise = await connection.QuerySingleOrDefaultAsync<ExerciseDto>(
            sql,
            new
            {
                id,
                Category,
                Schema,
            }
        );
        if (exercise is null)
            return NotFound();

        Exercise = exercise;

        // Load available categories for dropdown
        const string categoriesSql = """
            SELECT id, name
            FROM category
            ORDER BY name
            """;
        var categories = await connection.QueryAsync<CategoryDto>(categoriesSql);
        Categories = [.. categories.Select(c => new SelectListItem(c.Name, c.Id.ToString()))];

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        // TODO: implement a validator for solution.
        if (!ModelState.IsValid)
            return Page();

        var isValid = ValidSql.TryCreate(Exercise.Solution, out var validSql);
        if (!isValid)
        {
            ModelState.TryAddModelError("", "Invalid sql");
            return Page();
        }
        Exercise.Solution = validSql!;
        try
        {
            await runner.QueryReadOnly(
                Schema,
                Exercise.Solution,
                async (connections, transaction, solution) =>
                    await connections.QueryAsync(solution, transaction: transaction)
            );
        }
        catch (PostgresException)
        {
            ModelState.TryAddModelError("", "The solution must not modify state");
            return Page();
        }

        using var connection = ctx.CreateConnection();
        const string idSql = """
            SELECT ex.id
            FROM exercise ex
            INNER JOIN category cat
              ON cat.id = ex.category_id
            INNER JOIN user_schema us
              ON us.id = ex.user_schema_id
            WHERE ex.id = @id
              AND cat.name iLIKE @category
              AND us.short_name iLIKE @schema
            """;
        var exerciseId = await connection.QuerySingleOrDefaultAsync<int?>(
            idSql,
            new
            {
                Schema,
                Category,
                Exercise.Id,
            }
        );
        if (exerciseId is null)
            return NotFound();

        Exercise.Id = (int)exerciseId;

        const string sql = """
            UPDATE exercise SET
                title = @Title,
                question = @Question,
                solution = @Solution,
                explanation = @Explanation,
                hint = @Hint,
                category_id = @CategoryId
                WHERE id = @Id
            """;
        _ = await connection.ExecuteAsync(sql, Exercise);

        // Get the new category name for redirect
        const string categorySql = "SELECT name FROM category WHERE id = @CategoryId";
        var newCategoryName = await connection.QuerySingleAsync<string>(
            categorySql,
            new { Exercise.CategoryId }
        );

        return RedirectToPage("./Index", new { category = newCategoryName, Schema });
    }

    public class ExerciseDto
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        [Required]
        public string Question { get; set; } = null!;

        [Required]
        public string Solution { get; set; } = null!;

        public string? Explanation { get; set; }

        public string? Hint { get; set; }

        [Required]
        public int CategoryId { get; set; }
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
