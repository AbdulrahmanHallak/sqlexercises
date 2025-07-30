// TODO: implement it
using System.ComponentModel.DataAnnotations;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Schemas;

public class CreateModel(DapperContext context) : PageModel
{
    [BindProperty]
    public SchemaDto Schema { get; set; } = default!;

    public IActionResult OnGetAsync()
    {
        return NotFound();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return NotFound();
        if (!ModelState.IsValid)
            return Page();

        using var connection = context.CreateConnection();
        var sql = "INSERT INTO schema (name) VALUES (@Name)";
        await connection.ExecuteAsync(sql, Schema);

        return RedirectToPage("./Index");
    }

    public class SchemaDto
    {
        [Required]
        [StringLength(10, ErrorMessage = "Name cannot be longer than 10 characters.")]
        public string Name { get; set; } = default!;
    }
}
