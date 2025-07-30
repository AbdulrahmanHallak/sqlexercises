using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Schemas.Categories;

public class IndexModel(DapperContext context) : PageModel
{
    public IReadOnlyCollection<string?> Categories { get; set; } = default!;

    public bool IsAdmin { get; set; } = false;

    [BindProperty(SupportsGet = true)]
    public string? Schema { get; set; }

    public async Task<IActionResult> OnGet()
    {
        Request.Cookies.TryGetValue("is_admin", out var isAdmin);
        if (isAdmin == "true")
            IsAdmin = true;

        using var connection = context.CreateConnection();
        // this only shows categories that have exercises in this specific schema
        // since we are using INNER JOIN.
        // TODO: display the count of exercises for each category.
        var sql = """
                SELECT cat.name
                FROM category cat
                INNER JOIN exercise ex
                  ON ex.category_id = cat.id
                INNER JOIN user_schema us
                  ON us.id = ex.user_schema_id
                WHERE us.schema_name iLIKE @schema
                GROUP BY cat.id
                ORDER BY 1
            """;
        var categories = await connection.QueryAsync<string?>(sql, new { Schema });

        Categories = [.. categories];
        return Page();
    }

    // TODO: remove this
    // public async Task<IActionResult> OnPostAsync(string newCategoryName)
    // {
    //     Request.Cookies.TryGetValue("is_admin", out var isAdmin);
    //     if (isAdmin == "true")
    //         IsAdmin = true;
    //     else
    //         return NotFound();

    //     // TODO: make it asp-for tag helper and add validation.
    //     if (string.IsNullOrWhiteSpace(newCategoryName))
    //         return BadRequest();

    //     using var connection = context.CreateConnection();
    //     connection.Open();
    //     using (var trans = connection.BeginTransaction())
    //     {
    //         var categoryInsert = """
    //                 INSERT INTO category(name) VALUES(@name)
    //                 RETURNING id ;
    //             """;
    //         var categoryId = await connection.ExecuteScalarAsync(
    //             categoryInsert,
    //             new { Name = newCategoryName.Trim() }
    //         );

    //         var schemaCategoryInsert = """
    //                 INSERT INTO tenant_schema_category(tenant_schema_id, category_id)
    //                 VALUES(@schemaId, @categoryId)
    //             """;
    //         await connection.ExecuteAsync(schemaCategoryInsert, new { categoryId, SchemaId });

    //         trans.Commit();
    //     }

    //     // load categories
    //     var categoriesSql = """
    //             SELECT cat.name
    //             FROM category cat
    //             INNER JOIN tenant_schema_category tsc
    //               ON tsc.category_id = cat.id
    //             INNER JOIN tenant_schema ts
    //               ON ts.id = tsc.tenant_schema_id
    //             WHERE ts.schema_name = @schema
    //             ORDER BY 1
    //         """;
    //     var categories = await connection.QueryAsync<string>(categoriesSql, new { Schema });
    //     Categories = [.. categories];
    //     return Page();
    // }
}
