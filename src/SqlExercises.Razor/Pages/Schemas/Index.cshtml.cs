using Dapper;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Schemas;

public class IndexModel(DapperContext context) : PageModel
{
    public IReadOnlyCollection<SchemaDto> Schemas { get; set; } = default!;

    public async Task OnGetAsync()
    {
        using var connection = context.CreateConnection();
        var sql = "SELECT id, schema_name FROM user_schema ORDER BY schema_name";
        var schemas = await connection.QueryAsync<SchemaDto>(sql);
        Schemas = [.. schemas];
    }

    public class SchemaDto
    {
        public short Id { get; set; }
        public string SchemaName { get; set; } = default!;
    }
}
