using FluentMigrator.Runner;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using SqlExercises.Db;
using SqlExercises.Razor;
using SqlExercises.Razor.Pages.Schemas;
using SqlExercises.Razor.Pages.Schemas.Exercises;

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

    var pythonConnString =
        builder.Configuration.GetConnectionString("Postgres:Python")
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

    // TODO: edit two commit messages
    // TODO: fix docker
    // TODO:refactor
    // TODO: add the ability to regenerate erd from edit
    // TODO: add auth


    // since there is no dynamic search_path manipulation.
    builder.Services.AddSingleton(_ => new DefaultConnectionString(connString));
    builder.Services.AddSingleton(_ => new PythonConnectionString(pythonConnString));
    builder.Services.AddSingleton<SolutionChecker>();
    builder.Services.AddSingleton<IErdGenerator, ErdGenerator>();
    builder.Services.AddSingleton<AppDapperContext>();

    builder.Services.AddScoped<UserDapperContext>();
    builder.Services.AddScoped(_ => new SchemaConnectionString(solutionString));
    builder.Services.RegisterFluentMigrator(connString);
    builder.Services.AddScoped<UserSqlRunner>();

    builder.Services.AddHealthChecks();

    Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }

    app.UseForwardedHeaders(
        new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        }
    );
    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthorization();

    app.MapStaticAssets();
    app.MapRazorPages().WithStaticAssets();
    app.MapHealthChecks("/health");

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
