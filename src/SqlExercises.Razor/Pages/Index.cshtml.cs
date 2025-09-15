using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages;

public class IndexModel(DapperContext context) : PageModel
{
    public bool LoginFailed { get; set; } = false;

    public IReadOnlyCollection<SchemaDto> Schemas { get; set; } = default!;

    public async Task<IActionResult> OnGet()
    {
        using var connection = context.CreateConnection();
        var sql = "SELECT id, schema_name, short_name FROM user_schema ORDER BY schema_name";
        var schemas = await connection.QueryAsync<SchemaDto>(sql);
        Schemas = [.. schemas];
        return Page();
    }

    public class SchemaDto
    {
        public short Id { get; set; }
        public string SchemaName { get; set; } = default!;
        public string ShortName { get; set; } = default!;
    }
}
