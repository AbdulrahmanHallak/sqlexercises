using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly DapperContext _context;
    private readonly string _adminSecret;
    public bool LoginFailed { get; set; } = false;

    public IReadOnlyCollection<SchemaDto> Schemas { get; set; } = default!;

    public IndexModel(
        ILogger<IndexModel> logger,
        IConfiguration configuration,
        DapperContext context
    )
    {
        _logger = logger;
        _adminSecret =
            configuration.GetValue<string>("admin_secret")
            ?? throw new Exception("the secret cannot be null");
        _context = context;
    }

    public async Task<IActionResult> OnGet()
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT id, schema_name FROM user_schema ORDER BY schema_name";
        var schemas = await connection.QueryAsync<SchemaDto>(sql);
        Schemas = [.. schemas];
        return Page();
    }

    public IActionResult OnPost()
    {
        var adminSecret = Request.Form["adminSecret"].ToString();
        if (string.IsNullOrEmpty(adminSecret))
        {
            _logger.LogInformation(
                "failed attempt to login as admin from {ip}",
                Request.HttpContext.Connection.RemoteIpAddress
            );
            return Page();
        }

        if (adminSecret != _adminSecret)
        {
            _logger.LogInformation(
                "failed attempt to login as admin from {ip}",
                Request.HttpContext.Connection.RemoteIpAddress
            );
            return Page();
        }

        Response.Cookies.Append(
            "is_admin",
            "true",
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
            }
        );
        return Page();
    }

    public class SchemaDto
    {
        public short Id { get; set; }
        public string SchemaName { get; set; } = default!;
    }
}
