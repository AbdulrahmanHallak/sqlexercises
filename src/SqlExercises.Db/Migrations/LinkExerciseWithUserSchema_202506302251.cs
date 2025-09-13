using FluentMigrator;

namespace SqlExercises.Db.Migrations;

[Migration(202507281736)]
public class LinkExerciseWithUserSchema_202507281736 : Migration
{
    public override void Up()
    {
        Create
            .Column("user_schema_id")
            .OnTable("exercise")
            .InSchema("sqlexercises")
            .AsInt64()
            .NotNullable()
            .Indexed()
            .ForeignKey("exercise_user_schema_fk", "sqlexercises", "user_schema", "id");
    }

    public override void Down()
    {
        Delete.ForeignKey("exercise_user_schema_fk").OnTable("exercise").InSchema("sqlexercises");

        Delete.Column("user_schema_id").FromTable("exercise").InSchema("sqlexercises");
    }
}
