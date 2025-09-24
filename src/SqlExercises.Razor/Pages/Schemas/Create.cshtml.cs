using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Schemas;

public class CreateModel(UserSqlRunner runner, IErdGenerator erdGen) : PageModel
{
    [BindProperty]
    public SchemaDto Schema { get; set; } = default!;

    public IActionResult OnGetAsync()
    {
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
            return Page();

        var (created, error) = await runner.CreateSchema(
            Schema.Name,
            Schema.ShortName,
            Schema.CreateStatements
        );
        if (!created)
        {
            ModelState.AddModelError(string.Empty, error!);
            return Page();
        }
        erdGen.GenerateErd(Schema.ShortName);

        return RedirectToPage("Edit", new { schema = Schema.ShortName });
    }

    public class SchemaDto
    {
        [Required]
        [StringLength(
            50,
            MinimumLength = 3,
            ErrorMessage = "Name cannot be longer than 50 characters."
        )]
        [RegularExpression(
            @"^[a-zA-Z0-9_\-\s]+$",
            ErrorMessage = "Only letters, numbers, underscores, dashes, and spaces are allowed"
        )]
        public string Name { get; set; } = default!;

        [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "only letters are allowed")]
        [StringLength(10, MinimumLength = 3)]
        [Required]
        public string ShortName { get; set; } = default!;

        [Required]
        public string CreateStatements { get; set; } = default!;
    }
}
