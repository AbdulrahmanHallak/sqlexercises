using FluentMigrator.Runner;
using Microsoft.AspNetCore.DataProtection;
using Serilog;
using SqlExercises.Db;
using SqlExercises.Razor;
using SqlExercises.Razor.Pages.Schemas.Categories.Exercises;
using SqlExercises.Razor.Pages.Shared.Filters;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog(
        (context, config) => config.ReadFrom.Configuration(context.Configuration)
    );

    // Add services to the container.
    builder.Services.AddRazorPages();

    var connString =
        builder.Configuration.GetConnectionString("Postgres:Default")
        ?? throw new InvalidOperationException("Connection string cannot be null");

    var solutionString =
        builder.Configuration.GetConnectionString("Postgres:Solution")
        ?? throw new InvalidOperationException("Connection string cannot be null");

    builder.Services.Configure<RouteOptions>(opts =>
    {
        opts.LowercaseQueryStrings = true;
        opts.LowercaseUrls = true;
    });

    // this is to persist keys across docker containers.
    builder
        .Services.AddDataProtection()
        .PersistKeysToFileSystem(
            new DirectoryInfo(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".aspnet",
                    "DataProtection-Keys"
                )
            )
        )
        .SetApplicationName("MyApp");

    builder.Services.AddSingleton(_ => new ConnectionString(connString));
    builder.Services.AddSingleton<SolutionChecker>();
    builder.Services.AddScoped<DapperContext>();
    builder.Services.AddScoped(_ => new SolutionConnectionString(solutionString));
    builder.Services.RegisterFluentMigrator(connString);
    builder.Services.AddScoped<SearchPathFilter>();

    Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseRouting();

    app.UseAuthorization();

    app.MapStaticAssets();
    app.MapRazorPages().WithStaticAssets();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Server terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
