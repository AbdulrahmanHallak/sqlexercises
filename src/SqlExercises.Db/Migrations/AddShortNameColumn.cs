using FluentMigrator;

namespace SqlExercises.Db.Migrations;

[Migration(202509091912)]
public class AddShortnameColumn_202509091912 : Migration
{
    public override void Down()
    {
        Delete.Column("short_name").FromTable("user_schema").InSchema("sqlexercises");
    }

    public override void Up()
    {
        Alter
            .Table("user_schema")
            .InSchema("sqlexercises")
            .AddColumn("short_name")
            .AsString(12)
            .NotNullable()
            .Unique();
    }
}
