using System.Data;
using Npgsql;

namespace SqlExercises.Razor;

public class AppDapperContext(DefaultConnectionString connString)
{
    public IDbConnection CreateConnection() => new NpgsqlConnection(connString);
}
