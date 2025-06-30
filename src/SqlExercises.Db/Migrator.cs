using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace SqlExercises.Db;

public static class Migrator
{
    public static IServiceCollection RegisterFluentMigrator(
        this IServiceCollection services,
        string connectionString
    )
    {
        return services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb =>
                rb.AddPostgres15_0()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(Migrator).Assembly)
                    .For.All()
            )
            .AddLogging(lb => lb.AddFluentMigratorConsole());
    }
}
