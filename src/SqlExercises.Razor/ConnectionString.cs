namespace SqlExercises.Razor;

// * This will be used for everything else.
public record ConnectionString(string Value)
{
    public static implicit operator string(ConnectionString c) => c.Value;

    public static implicit operator ConnectionString(string s) => new(s);
}

// * This will be used only to run user submitted solutions.
public record SolutionConnectionString
{
    public string Value { get; set; }

    public SolutionConnectionString(string value)
    {
        Value = value;
    }

    public static implicit operator string(SolutionConnectionString c) => c.Value;

    public static implicit operator SolutionConnectionString(string s) => new(s);
}
