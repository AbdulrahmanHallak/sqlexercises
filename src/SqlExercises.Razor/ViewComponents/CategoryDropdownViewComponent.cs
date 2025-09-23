using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace SqlExercises.Razor.ViewComponents;

public class CategoryDropdownViewComponent(AppDapperContext ctx) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        using var connection = ctx.CreateConnection();
        var sql = "SELECT name FROM category";
        var categories = await connection.QueryAsync<string>(sql);

        return View(categories);
    }
}
