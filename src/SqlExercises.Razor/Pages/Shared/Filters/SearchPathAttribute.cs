using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Npgsql;

namespace SqlExercises.Razor.Pages.Shared.Filters;

public class SearchPathAttribute : ServiceFilterAttribute
{
    public SearchPathAttribute()
        : base(typeof(SearchPathFilter)) { }
}

public class SearchPathFilter(SchemaConnectionString connectionString) : IPageFilter
{
    public void OnPageHandlerExecuted(PageHandlerExecutedContext context) { }

    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        context.HttpContext.Request.RouteValues.TryGetValue("schema", out var value);
        var schema = value as string;
        if (string.IsNullOrWhiteSpace(schema))
            return;

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        builder.SearchPath += $",{schema}";
        connectionString.Value = builder.ToString();
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context) { }
}
