namespace SqlExercises.Razor;

// * This will be used for everything else.
public record DefaultConnectionString(string Value)
{
    public static implicit operator string(DefaultConnectionString c) => c.Value;

    public static implicit operator DefaultConnectionString(string s) => new(s);
}

// * This will be used only to run user submitted sql.
public record SchemaConnectionString
{
    public string Value { get; set; }

    public SchemaConnectionString(string value)
    {
        Value = value;
    }

    public static implicit operator string(SchemaConnectionString c) => c.Value;

    public static implicit operator SchemaConnectionString(string s) => new(s);
}
