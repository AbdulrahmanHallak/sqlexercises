using System.ComponentModel.DataAnnotations;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SqlExercises.Razor.Pages.Categories.Exercises;

public class CreateModel(DapperContext context) : PageModel
{
    [BindProperty]
    public ExerciseDto Exercise { get; set; } = default!;

    [BindProperty]
    public string? NewCategory { get; set; }

    public List<SelectListItem> CategoryOptions { get; set; } = [];

    public async Task OnGetAsync()
    {
        await LoadCategories();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!string.IsNullOrWhiteSpace(NewCategory))
        {
            using var connection = context.CreateConnection();
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

        using (var connection = context.CreateConnection())
        {
            var sql =
                @"INSERT INTO exercise (question, solution, answer, hint, category_id) VALUES (@Question, @Solution, @Answer, @Hint, @CategoryId);";
            await connection.ExecuteAsync(sql, Exercise);
        }

        string categoryName;
        if (!string.IsNullOrWhiteSpace(NewCategory))
        {
            categoryName = NewCategory;
        }
        else
        {
            using var connection = context.CreateConnection();
            var sql = "SELECT name FROM category WHERE id = @id;";
            categoryName = await connection.QuerySingleAsync<string>(
                sql,
                new { id = Exercise.CategoryId }
            );
        }

        return RedirectToPage("/Categories/Exercises/Index", new { category = categoryName });
    }

    private async Task LoadCategories()
    {
        using var connection = context.CreateConnection();
        var sql = "SELECT id, name FROM category ORDER BY name;";
        var categories = await connection.QueryAsync<CategoryDto>(sql);
        CategoryOptions = new List<SelectListItem>();
        foreach (var cat in categories)
        {
            CategoryOptions.Add(new SelectListItem { Value = cat.Id.ToString(), Text = cat.Name });
        }
    }

    public class ExerciseDto
    {
        [Required]
        public string Question { get; set; } = default!;

        [Required]
        public string Solution { get; set; } = default!;

        [Required]
        public string Answer { get; set; } = default!;

        public string? Hint { get; set; }

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
