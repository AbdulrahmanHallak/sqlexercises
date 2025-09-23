using System.ComponentModel.DataAnnotations;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Schemas;

public class EditModel(AppDapperContext ctx, UserSqlRunner runner) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Schema { get; set; } = default!;

    [BindProperty]
    public EditSchemaDto EditSchema { get; set; } = default!;

    public async Task<IActionResult> OnGet()
    {
        using var connection = ctx.CreateConnection();
        var sql = "SELECT schema_name FROM user_schema WHERE short_name = @shortName";
        var schemaName = await connection.QuerySingleOrDefaultAsync<string>(
            sql,
            new { shortName = Schema }
        );

        if (string.IsNullOrWhiteSpace(schemaName))
            return NotFound();

        EditSchema = new EditSchemaDto { Name = schemaName };
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        using (var connection = ctx.CreateConnection())
        {
            var sql = "UPDATE user_schema SET schema_name = @newName WHERE short_name = @shortName";
            await connection.ExecuteAsync(
                sql,
                new { newName = EditSchema.Name, shortName = Schema }
            );
        }

        if (EditSchema.Sql is { })
        {
            var isValid = ValidSql.TryCreate(EditSchema.Sql, out var validSql);

            if (!isValid)
            {
                ModelState.AddModelError(string.Empty, "there is an error in the provided sql");
                return Page();
            }

            var executed = await runner.ExecuteUserCmd(validSql!, Schema);
            if (!executed)
            {
                ModelState.AddModelError(string.Empty, "there is an error in the provided sql");
                return Page();
            }

            TempData["SuccessMessage"] = "Commands executed successfully.";
            return RedirectToPage("Edit", new { schema = Schema });
        }

        return Page();
    }

    public class EditSchemaDto
    {
        [Required]
        [StringLength(20, ErrorMessage = "Name cannot be longer than 20 characters.")]
        [RegularExpression(
            @"^[a-zA-Z0-9_\-\s]+$",
            ErrorMessage = "Only letters, numbers, underscores, dashes, and spaces are allowed"
        )]
        public string Name { get; set; } = default!;

        public string? Sql { get; set; }
    }
}
