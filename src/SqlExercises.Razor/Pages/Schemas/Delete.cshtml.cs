// TODO: implement it
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Schemas;

public class DeleteModel(DapperContext context) : PageModel
{
    public SchemaDto Schema { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(short id)
    {
        return NotFound();

        using var connection = context.CreateConnection();
        var sql = "SELECT id, name FROM schema WHERE id = @id";
        var schema = await connection.QuerySingleOrDefaultAsync<SchemaDto>(sql, new { id });
        if (schema == null)
            return NotFound();
        Schema = schema;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(short id)
    {
        return NotFound();
        using var connection = context.CreateConnection();
        var sql = "DELETE FROM schema WHERE id = @id";
        await connection.ExecuteAsync(sql, new { id });
        return RedirectToPage("./Index");
    }

    public class SchemaDto
    {
        public short Id { get; set; }
        public string Name { get; set; } = default!;
    }
}
