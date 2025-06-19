using SqlExercises.Razor;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Configuration.AddEnvironmentVariables();
var connString =
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string cannot be null");

builder.Services.AddSingleton<ConnectionString>(_ => new ConnectionString(connString));
builder.Services.AddSingleton<DapperContext>();

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

var app = builder.Build();

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
