using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SqlExercises.Razor;

namespace SqlExercises.Razor.Pages.Categories.Exercises
{
    public class EditModel : PageModel
    {
        private readonly DapperContext _context;

        public EditModel(DapperContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ExerciseViewModel Exercise { get; set; } = new();

        [BindProperty]
        public string? NewCategory { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Category { get; set; }

        public List<SelectListItem> CategoryOptions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            using var connection = _context.CreateConnection();
            var sql =
                "SELECT id, question, solution, answer, hint, category_id FROM exercise WHERE id = @id;";
            var exercise = await connection.QuerySingleOrDefaultAsync<ExerciseViewModel>(
                sql,
                new { id }
            );
            if (exercise == null)
                return NotFound();
            Exercise = exercise;
            await LoadCategories();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!string.IsNullOrWhiteSpace(NewCategory))
            {
                using var connection = _context.CreateConnection();
                var sql = "INSERT INTO category (name) VALUES (@name) RETURNING id;";
                var categoryId = await connection.ExecuteScalarAsync<int>(
                    sql,
                    new { name = NewCategory }
                );
                Exercise.CategoryId = categoryId;
            }

            if (!ModelState.IsValid)
            {
                await LoadCategories();
                return Page();
            }

            using (var connection = _context.CreateConnection())
            {
                var sql =
                    @"UPDATE exercise SET question = @Question, solution = @Solution, answer = @Answer, hint = @Hint, category_id = @CategoryId WHERE id = @Id;";
                await connection.ExecuteAsync(sql, Exercise);
            }
            return RedirectToPage(
                "/Categories/Exercises/Index",
                new { category = NewCategory ?? Category }
            );
        }

        private async Task LoadCategories()
        {
            using var connection = _context.CreateConnection();
            var sql = "SELECT id, name FROM category ORDER BY name;";
            var categories = await connection.QueryAsync<CategoryDto>(sql);
            CategoryOptions = new List<SelectListItem>();
            foreach (var cat in categories)
            {
                CategoryOptions.Add(
                    new SelectListItem { Value = cat.Id.ToString(), Text = cat.Name }
                );
            }
        }

        public class ExerciseViewModel
        {
            public int Id { get; set; }

            [Required]
            public string Question { get; set; }

            [Required]
            public string Solution { get; set; }
            public string Answer { get; set; }
            public string Hint { get; set; }

            [Required]
            [Display(Name = "Category")]
            public int? CategoryId { get; set; }
        }

        public class CategoryDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
