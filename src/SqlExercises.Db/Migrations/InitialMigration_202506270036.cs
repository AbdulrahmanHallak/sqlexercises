using FluentMigrator;

namespace SqlExercises.Db.Migrations;

[Migration(202506270036)]
public class InitialMigration_202506270036 : Migration
{
    public override void Down()
    {
        Delete.Table("exercise");
        Delete.Table("category");
    }

    public override void Up()
    {
        Create
            .Table("category")
            .WithColumn("id")
            .AsInt64()
            .Identity()
            .PrimaryKey()
            .WithColumn("name")
            .AsString(30)
            .NotNullable()
            .Indexed();

        Create
            .Table("exercise")
            .WithColumn("id")
            .AsInt64()
            .Identity()
            .PrimaryKey()
            //
            .WithColumn("title")
            .AsString(50)
            .NotNullable()
            .Unique()
            //
            .WithColumn("question")
            .AsString()
            .NotNullable()
            //
            .WithColumn("solution")
            .AsString()
            .NotNullable()
            //
            .WithColumn("explanation")
            .AsString()
            .Nullable()
            //
            .WithColumn("hint")
            .AsString()
            .Nullable()
            //
            .WithColumn("category_id")
            .AsInt64()
            .NotNullable()
            .Indexed()
            .ForeignKey("exercise_category_fk", "category", "id");
    }
}
