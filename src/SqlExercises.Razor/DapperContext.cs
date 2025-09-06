using System.Data;
using Npgsql;

namespace SqlExercises.Razor;

public class DapperContext
{
    private readonly ConnectionString _connectionString;
    private readonly SolutionConnectionString _solutionString;

    public DapperContext(ConnectionString connectionString, SolutionConnectionString solutionString)
    {
        _connectionString = connectionString;
        _solutionString = solutionString;
    }

    public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

    public IDbConnection CreateSolutionConnection() => new NpgsqlConnection(_solutionString);
}
