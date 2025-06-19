using Microsoft.AspNetCore.Mvc;

namespace SqlExercises.Razor.Pages.Shared;

public class CategoriesDropdownViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        // TODO: Fill this list with your categories
        var categories = new List<string> { "Category 1", "Category 2", "Category 3" };
        return View(categories);
    }
}
