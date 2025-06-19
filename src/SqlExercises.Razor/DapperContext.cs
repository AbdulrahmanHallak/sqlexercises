using System.Data;
using Npgsql;

namespace SqlExercises.Razor;

public class DapperContext
{
    private readonly ConnectionString _connectionString;

    public DapperContext(ConnectionString connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}
