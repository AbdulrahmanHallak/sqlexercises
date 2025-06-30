using FluentMigrator;

[Migration(202506302139)]
public class AddTenantsRelations_202506302139 : Migration
{
    public override void Up()
    {
        Create
            .Table("user_schema")
            .InSchema("public")
            .WithColumn("id")
            .AsInt32()
            .PrimaryKey()
            .Identity()
            //
            .WithColumn("schema_name")
            .AsString()
            .NotNullable()
            .Unique()
            //
            .WithColumn("created_at")
            .AsDateTime()
            .NotNullable()
            .WithDefault(SystemMethods.CurrentDateTime)
            //
            .WithColumn("is_active")
            .AsBoolean()
            .NotNullable()
            .WithDefaultValue(true);
    }

    public override void Down()
    {
        Delete.Table("user_schema").InSchema("public");
    }
}
