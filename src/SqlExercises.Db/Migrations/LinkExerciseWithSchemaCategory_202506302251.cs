using FluentMigrator;

namespace SqlExercises.Db.Migrations;

public class LinkExerciseWithUserSchema_202506302251 : Migration
{
    public override void Up()
    {
        Create
            .Column("user_schema_id")
            .OnTable("exercise")
            .AsInt64()
            .NotNullable()
            .Indexed()
            .ForeignKey("exercise_user_schema_fk", "user_schema", "id");
    }

    public override void Down()
    {
        Delete.ForeignKey("exercise_user_schema_fk").OnTable("exercise");

        Delete.Column("user_schema_id").FromTable("exercise");
    }
}
