using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private string AdminSecret;

    public bool LoginFailed { get; set; } = false;

    public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
    {
        _logger = logger;
        AdminSecret =
            configuration.GetValue<string>("admin_secret")
            ?? throw new Exception("the secret cannot be null");
    }

    public void OnGet() { }

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

        if (adminSecret != AdminSecret)
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
}
