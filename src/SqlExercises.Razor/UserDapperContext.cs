using System.Data;
using Dapper;
using Npgsql;

namespace SqlExercises.Razor;

public class UserDapperContext(SchemaConnectionString connString)
{
    public IDbConnection CreateConnection(string schemaShortName)
    {
        var connection = new NpgsqlConnection(connString);
        connection.Execute(
            $"""
                SET search_path =  {schemaShortName};
                SET ROLE {schemaShortName};
            """
        );
        return connection;
    }
}
