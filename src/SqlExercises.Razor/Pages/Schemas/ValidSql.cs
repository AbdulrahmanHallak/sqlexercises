using System.Diagnostics.CodeAnalysis;
using Microsoft.IdentityModel.Tokens;
using SqlParser;
using SqlParser.Ast;
using ColumnType = SqlParser.Ast.DataType;

namespace SqlExercises.Razor.Pages.Schemas;

public record ValidSql
{
    public string Value { get; private set; }

    public static implicit operator string(ValidSql sql) => sql.Value;

    private ValidSql(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static bool TryCreate(string statements, [NotNullWhen(true)] out ValidSql? sql)
    {
        var validator = new Validator();
        var isValid = validator.Validate(statements);
        if (!isValid)
        {
            sql = default!;
            return false;
        }
        sql = new(statements);
        return true;
    }

    private class Validator
    {
        public bool Validate(string statements)
        {
            Sequence<Statement> stmts;
            try
            {
                stmts = new SqlQueryParser().Parse(
                    statements,
                    new ParserOptions { TrailingCommas = false }
                );
            }
            catch (ParserException ex)
            {
                // TODO: result pattern.
                _ = ex.Message;
                return false;
            }
            foreach (var stmt in stmts)
            {
                bool isAllowed;
                switch (stmt)
                {
                    case Statement.CreateTable create:
                        isAllowed = AreAllowedCreateStatements(create);
                        if (!isAllowed)
                            return false;

                        if (!create.Element.Constraints.IsNullOrEmpty())
                            isAllowed = AreAllowedTableConstraints(create.Element.Constraints!);
                        if (!isAllowed)
                            return false;

                        isAllowed = AreAllowedColumns(create.Element.Columns);
                        if (!isAllowed)
                            return false;
                        break;

                    case Statement.AlterTable alter:
                        isAllowed = AreAllowedAlterTableOperations(alter.Operations);
                        if (!isAllowed)
                            return false;
                        break;
                    case Statement.CreateSequence seq:
                        isAllowed = AreAllowedSequences(seq);
                        if (!isAllowed)
                            return false;
                        break;
                    case Statement.Drop drop:
                        isAllowed = AreAllowedDrop(drop);
                        if (!isAllowed)
                            return false;
                        break;
                    // whitelisted.
                    case Statement.Commit:
                    case Statement.Delete:
                    case Statement.Update:
                    case Statement.Insert:
                    case Statement.Rollback:
                    case Statement.Savepoint:
                    case Statement.Select:
                        break;

                    default:
                        return false;
                }
            }
            return true;
        }

        private bool AreAllowedDrop(Statement.Drop drop) =>
            drop.ObjectType switch
            {
                ObjectType.Table => true,
                ObjectType.Sequence => true,
                _ => false,
            };

        private bool AreAllowedSequences(Statement.CreateSequence seq)
        {
            return !seq.Temporary;
        }

        private bool AreAllowedAlterTableOperations(AlterTableOperation[] ops)
        {
            string[] whitelist =
            [
                nameof(AlterTableOperation.AddColumn),
                nameof(AlterTableOperation.AddConstraint),
                nameof(AlterTableOperation.AlterColumn),
                nameof(AlterTableOperation.ChangeColumn),
                nameof(AlterTableOperation.DropColumn),
                nameof(AlterTableOperation.DropConstraint),
                nameof(AlterTableOperation.ModifyColumn),
                nameof(AlterTableOperation.RenameConstraint),
                nameof(AlterTableOperation.RenameTable),
            ];
            foreach (var op in ops)
            {
                if (!whitelist.Contains(op.GetType().Name))
                    return false;
            }
            return true;
        }

        private bool AreAllowedTableConstraints(TableConstraint[] constraints)
        {
            foreach (var con in constraints)
            {
                if (
                    con
                    is TableConstraint.FulltextOrSpatial
                        or TableConstraint.PostgresAlterTableIndex
                )
                    return false;
            }
            return true;
        }

        private bool AreAllowedCreateStatements(Statement.CreateTable create)
        {
            string[] whitelist = ["Name", "Columns", "Constraints", "IfNotExists", "Comment"];
            var props = create.Element.GetType().GetProperties();

            foreach (var prop in props)
            {
                if (!whitelist.Contains(prop.Name))
                {
                    var val = prop.GetValue(create.Element);
                    var defaultVal = CreateDefaultInstance(prop.PropertyType);
                    if (
                        val is null or HiveDistributionStyle.None or FileFormat.None
                        && defaultVal is null
                    )
                        continue;
                    if (!val!.Equals(defaultVal))
                        return false;
                }
            }
            return true;

            static object? CreateDefaultInstance(Type t)
            {
                if (t.IsValueType)
                    return Activator.CreateInstance(t)!;
                return null;
            }
        }

        private bool AreAllowedColumns(ColumnDef[] columns)
        {
            string[] whitelist =
            [
                nameof(ColumnType.Array),
                nameof(ColumnType.BigInt),
                nameof(ColumnType.BigNumeric),
                nameof(ColumnType.Binary),
                nameof(ColumnType.Boolean),
                nameof(ColumnType.Char),
                nameof(ColumnType.Character),
                nameof(ColumnType.CharacterVarying),
                nameof(ColumnType.Varchar),
                nameof(ColumnType.Uuid),
                nameof(ColumnType.Date),
                nameof(ColumnType.Datetime),
                nameof(ColumnType.DoublePrecision),
                nameof(ColumnType.Interval),
                nameof(ColumnType.Numeric),
                nameof(ColumnType.Real),
                nameof(ColumnType.SmallInt),
                nameof(ColumnType.Text),
                nameof(ColumnType.Time),
                nameof(ColumnType.Timestamp),
                nameof(ColumnType.Integer),
                nameof(ColumnType.Decimal),
                nameof(ColumnType.Int),
            ];
            foreach (var col in columns)
            {
                if (col.DataType is DataType.Custom cus)
                {
                    if (cus.Name.Equals("SERIAL"))
                        continue;
                }
                // because there are so many variants of int
                if (col.DataType.GetType().Name.Contains("int", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!whitelist.Contains(col.DataType.GetType().Name))
                    return false;

                if (col.Collation is not null)
                    return false;

                if (!col.Options.IsNullOrEmpty())
                {
                    foreach (var opt in col.Options!)
                    {
                        switch (opt.Option)
                        {
                            case ColumnOption.NotNull:
                            case ColumnOption.Check:
                            case ColumnOption.Identity:
                            case ColumnOption.Default:
                            case ColumnOption.Unique:
                            case ColumnOption.OnUpdate:
                            case ColumnOption.ForeignKey:
                                continue;
                            default:
                                return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
