using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Schemas;

public class ViewModel(AppDapperContext ctx) : PageModel
{
    public SchemaDto Schema { get; set; } = null!;
    public IReadOnlyCollection<CategoryDto> Categories { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(string schema)
    {
        using var connection = ctx.CreateConnection();
        const string schemaSql =
            "SELECT id, schema_name, short_name FROM user_schema WHERE short_name iLIKE @schema";

        var schemaResult = await connection.QuerySingleOrDefaultAsync<SchemaDto?>(
            schemaSql,
            new { schema }
        );

        if (schemaResult is null)
            return NotFound();
        Schema = schemaResult;

        const string categoriesSql = """
            SELECT DISTINCT cat.id, cat.name
            FROM category cat
            INNER JOIN exercise ex
              ON ex.category_id = cat.id
            INNER JOIN user_schema us
              ON us.id = ex.user_schema_id
            WHERE us.short_name = @schema
            ORDER BY cat.name
            """;
        var categories = await connection.QueryAsync<CategoryDto>(categoriesSql, new { schema });
        Categories = [.. categories];
        return Page();
    }

    public class SchemaDto
    {
        public int Id { get; set; }
        public string SchemaName { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
