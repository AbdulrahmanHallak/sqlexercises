using System.ComponentModel.DataAnnotations;
using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;

namespace SqlExercises.Razor.Pages.Schemas.Exercises;

public class CreateModel(AppDapperContext ctx, UserSqlRunner runner) : PageModel
{
    [BindProperty]
    public ExerciseDto Exercise { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public string Schema { get; set; } = null!;

    public int UserSchemaId { get; set; }

    public List<SelectListItem> Categories { get; set; } = [];

    [BindProperty]
    public string? NewCategoryName { get; set; }

    public async Task<IActionResult> OnGet()
    {
        using var connection = ctx.CreateConnection();
        const string schemaSql = "SELECT id FROM user_schema WHERE short_name iLIKE @schema";
        var schemaId = await connection.QueryFirstOrDefaultAsync<int?>(schemaSql, new { Schema });

        if (schemaId is null)
            return NotFound();

        UserSchemaId = schemaId.Value;

        await ReloadCategoriesAsync(connection);

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName) && !Exercise.CategoryId.HasValue)
        {
            ModelState.AddModelError(
                "Exercise.CategoryId",
                "Select a category or create a new one."
            );
        }

        if (!ModelState.IsValid)
            return Page();

        using var connection = ctx.CreateConnection();

        const string schemaSql = "SELECT id FROM user_schema WHERE short_name iLIKE @schema";
        var userSchemaId = await connection.QueryFirstOrDefaultAsync<int?>(
            schemaSql,
            new { Schema }
        );
        if (userSchemaId is null)
            return NotFound();

        Exercise.UserSchemaId = (int)userSchemaId;

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

        if (!string.IsNullOrWhiteSpace(NewCategoryName))
        {
            const string insertCategorySql =
                "INSERT INTO category (name) VALUES (@Name) RETURNING id";

            var newCategoryId = await connection.QuerySingleAsync<int>(
                insertCategorySql,
                new { Name = NewCategoryName }
            );
            Exercise.CategoryId = newCategoryId;
        }

        const string sql = """
            INSERT INTO exercise (title, question, solution, explanation, hint, category_id, user_schema_id)
            VALUES (@Title, @Question, @Solution, @Explanation, @Hint, @CategoryId, @UserSchemaId)
            RETURNING id
            """;
        var id = await connection.QuerySingleAsync<int>(sql, Exercise);

        return RedirectToPage(
            "./Exercise",
            new
            {
                id,
                schema = Schema,
                category = NewCategoryName
                    ?? Categories
                        .FirstOrDefault(c => c.Value == Exercise.CategoryId.ToString())
                        ?.Text,
            }
        );
    }

    private async Task ReloadCategoriesAsync(IDbConnection connection)
    {
        var categories = await connection.QueryAsync<CategoryDto>(
            "SELECT id, name FROM category ORDER BY name"
        );
        Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
    }

    public class ExerciseDto
    {
        [Required]
        public string Title { get; set; } = null!;

        [Required]
        public string Question { get; set; } = null!;

        [Required]
        public string Solution { get; set; } = null!;

        public string? Explanation { get; set; }

        public string? Hint { get; set; }

        public int? CategoryId { get; set; }

        public int UserSchemaId { get; set; }
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
