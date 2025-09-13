using FluentMigrator.Runner.VersionTableInfo;

namespace SqlExercises.Db;

public class VersionInfo : IVersionTableMetaData
{
    public bool OwnsSchema => true;

    public string SchemaName => "sqlexercises";

    public string TableName => "VersionInfo";

    public string ColumnName => "Version";

    public string UniqueIndexName => $"{SchemaName}_{TableName}_{ColumnName}_uidx";

    public string AppliedOnColumnName => "AppliedOn";

    public string DescriptionColumnName => "Description";

    public bool CreateWithPrimaryKey => false;
}
