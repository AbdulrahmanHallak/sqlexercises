using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SqlExercises.Razor.Pages.Categories.Exercises
{
    public class DeleteModel(DapperContext context) : PageModel
    {
        private readonly DapperContext _context = context;

        public ExerciseDto Exercise { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            using var connection = _context.CreateConnection();
            var sql =
                @"SELECT ex.id, ex.question, cat.name AS CategoryName FROM exercise ex INNER JOIN category cat ON ex.category_id = cat.id WHERE ex.id = @id;";
            Exercise = (await connection.QuerySingleOrDefaultAsync<ExerciseDto>(sql, new { id }))!;
            if (Exercise == null)
                return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id, string category)
        {
            using var connection = _context.CreateConnection();
            var sql = "DELETE FROM exercise WHERE id = @id;";
            await connection.ExecuteAsync(sql, new { id });
            return RedirectToPage("/Categories/Exercises/Index", new { category });
        }

        public class ExerciseDto
        {
            public int Id { get; set; }
            public string Question { get; set; } = default!;
            public string CategoryName { get; set; } = default!;
        }
    }
}
