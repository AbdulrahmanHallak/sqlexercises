using System.ComponentModel.DataAnnotations;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SqlExercises.Razor.Pages.Categories.Exercises
{
    public class EditModel(DapperContext context) : PageModel
    {
        private readonly DapperContext _context = context;

        [BindProperty]
        public ExerciseDto Exercise { get; set; } = new();

        [BindProperty]
        public string? NewCategory { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Category { get; set; } = default!;

        public IReadOnlyCollection<SelectListItem> Categories { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            using var connection = _context.CreateConnection();
            var sql = """
                SELECT id, title, question, solution, answer, hint, category_id 
                FROM exercise
                WHERE id = @id
                """;
            var exercise = await connection.QuerySingleOrDefaultAsync<ExerciseDto>(sql, new { id });
            if (exercise == null)
                return NotFound();
            Exercise = exercise;

            // Load available categories for dropdown
            var categoriesSql = "SELECT id, name FROM category ORDER BY name";
            var categories = await connection.QueryAsync<CategoryDto>(categoriesSql);
            Categories = [.. categories.Select(c => new SelectListItem(c.Name, c.Id.ToString()))];

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            using var connection = _context.CreateConnection();
            var sql =
                @"UPDATE exercise SET
                    title = @Title,
                    question = @Question,
                    solution = @Solution,
                    answer = @Answer,
                    hint = @Hint,
                    category_id = @CategoryId
                    WHERE id = @Id;";
            await connection.ExecuteAsync(sql, Exercise);

            // Get the new category name for redirect
            var categorySql = "SELECT name FROM category WHERE id = @CategoryId";
            var newCategoryName = await connection.QuerySingleOrDefaultAsync<string>(
                categorySql,
                new { Exercise.CategoryId }
            );

            return RedirectToPage(
                "/Categories/Exercises/Index",
                new { category = newCategoryName ?? Category }
            );
        }

        public class ExerciseDto
        {
            public int Id { get; set; }

            [Required]
            public string Title { get; set; } = default!;

            [Required]
            public string Question { get; set; } = default!;

            [Required]
            public string Solution { get; set; } = default!;

            public string? Answer { get; set; }

            public string? Hint { get; set; }

            [Required]
            public int CategoryId { get; set; }
        }

        public class CategoryDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = default!;
        }
    }
}
