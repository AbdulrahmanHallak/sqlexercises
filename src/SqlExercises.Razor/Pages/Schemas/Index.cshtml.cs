using Dapper;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Schemas;

public class IndexModel(AppDapperContext ctx) : PageModel
{
    public IReadOnlyCollection<SchemaDto> Schemas { get; set; } = default!;

    public async Task OnGetAsync()
    {
        using var connection = ctx.CreateConnection();
        var sql = "SELECT id, schema_name, short_name FROM user_schema ORDER BY schema_name";
        var schemas = await connection.QueryAsync<SchemaDto>(sql);
        Schemas = [.. schemas];
    }

    public class SchemaDto
    {
        public short Id { get; set; }
        public string SchemaName { get; set; } = default!;
        public string ShortName { get; set; } = default!;
    }
}
