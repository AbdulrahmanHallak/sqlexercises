using System.Data;
using Dapper;
using Npgsql;

namespace SqlExercises.Razor.Pages.Schemas;

public class UserSqlRunner(
    AppDapperContext ctx,
    UserDapperContext schemaCtx,
    ILogger<UserSqlRunner> logger
)
{
    public async Task<(bool created, string? error)> CreateSchema(
        string schemaName,
        string shortName,
        string createStmts
    )
    {
        var isValid = ValidSql.TryCreate(createStmts, out var sql);
        if (!isValid)
            return (false, "there is an error in your sql");

        // this is safe because there is already a regex to make it only one word and accept only letters.
        var createRoleSql = $"""
                CREATE ROLE {shortName} NOLOGIN;
                CREATE SCHEMA AUTHORIZATION {shortName};
                GRANT USAGE, CREATE ON SCHEMA {shortName} TO {shortName};


                ALTER DEFAULT PRIVILEGES FOR ROLE {shortName} IN SCHEMA {shortName}
                    GRANT ALL ON TABLES TO {shortName};

                ALTER DEFAULT PRIVILEGES FOR ROLE {shortName} IN SCHEMA {shortName}
                    GRANT ALL ON SEQUENCES TO {shortName};

                -- Allow user to call functions in public (built-ins or pre-defined)
                GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO {shortName};

                -- Ensure future functions in public schema are also executable
                ALTER DEFAULT PRIVILEGES IN SCHEMA public
                    GRANT EXECUTE ON FUNCTIONS TO {shortName};

                ALTER ROLE {shortName} SET search_path = {shortName};

                INSERT INTO user_schema(schema_name, short_name)
                VALUES(@name, '{shortName}');
            """;

        using var connection = ctx.CreateConnection();
        connection.Open();
        using (var transaction = connection.BeginTransaction())
        {
            try
            {
                _ = await connection.ExecuteAsync(
                    createRoleSql,
                    new { name = schemaName, shortName }
                );

                var userSql = $"""
                       SET search_path =  {shortName};
                       SET ROLE {shortName};
                       {sql}
                    """;
                await connection.ExecuteAsync(userSql, transaction: transaction);
                transaction.Commit();
            }
            catch (PostgresException ex)
            {
                transaction.Rollback();
                logger.LogError(
                    "Error occurred while creating schema {SchemaName}. Error Msg: {Exception}",
                    shortName,
                    ex.Message
                );
                return (false, ex.Message);
            }
            catch (NpgsqlException ex)
            {
                transaction.Rollback();
                logger.LogError(
                    "Unexpected exception occurred while creating schema {SchemaName}. {Exception}",
                    shortName,
                    ex.Message
                );
                return (
                    false,
                    "There has been an error.\n Try again later, if the issue persists please contact site admins"
                );
            }
            // TODO: see if you can inherit from npgsql connection to make a constructor that accepts the connection string type
            return (true, default);
        }
    }

    public async Task<bool> ExecuteUserCmd(ValidSql userCmd, string schemaShortName)
    {
        var sql = $"""
               SET search_path =  {schemaShortName};
               SET ROLE {schemaShortName};
               {userCmd}
            """;
        using var connection = schemaCtx.CreateConnection(schemaShortName);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(sql);
            transaction.Commit();
        }
        catch (PostgresException ex)
        {
            transaction.Rollback();
            logger.LogError(
                "Unexpected exception occurred while creating schema {SchemaName}. {Exception}",
                schemaShortName,
                ex.Message
            );
            return false;
        }
        // TODO: error handling and result
        return true;
    }

    public async Task<IEnumerable<T>> QueryReadOnly<T>(
        string schemaShortName,
        string sql,
        Func<IDbConnection, IDbTransaction, string, Task<IEnumerable<T>>> func
    )
    {
        using var connection = schemaCtx.CreateConnection(schemaShortName);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync("SET TRANSACTION READ ONLY", transaction: transaction);

        IEnumerable<T>? result;
        try
        {
            result = await func(connection, transaction, sql);
            transaction.Rollback();
        }
        catch (PostgresException)
        {
            transaction.Rollback();
            throw;
        }

        return result;
    }
}
