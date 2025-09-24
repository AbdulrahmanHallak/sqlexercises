import argparse
from eralchemy2 import render_er
from sqlalchemy import (
    create_engine,
    MetaData,
    Table,
    inspect,
    ForeignKeyConstraint,
    Column,
)
import warnings
import subprocess
import os


def generate_pg_erd(
    connection_string,
    schema,
    output_file,
    excluded_tables=None,
    width=10,
    height=8,
    dpi=300,
):
    """
    Generate WEBP ERD for a specific schema with eralchemy2
    """
    if excluded_tables is None:
        excluded_tables = []
    try:
        warnings.filterwarnings("ignore", category=UserWarning)
        engine = create_engine(connection_string)
        metadata = MetaData()
        inspector = inspect(engine)
        table_names = [
            t
            for t in inspector.get_table_names(schema=schema)
            if t.lower() not in [x.lower() for x in excluded_tables]
        ]
        if not table_names:
            print(f"No tables found in schema '{schema}'.")
            return

        bare_metadata = MetaData()
        metadata.reflect(bind=engine, schema=schema, only=table_names)

        table_mapping = {}
        for table_name in table_names:
            original_table = metadata.tables[f"{schema}.{table_name}"]

            new_table = Table(table_name, bare_metadata)

            for column in original_table.columns:
                new_col = Column(
                    column.name,
                    column.type,
                    nullable=column.nullable,
                    default=column.default,
                    primary_key=column.primary_key,
                    unique=column.unique,
                    index=column.index,
                    autoincrement=column.autoincrement,
                )
                new_table.append_column(new_col)

            table_mapping[f"{schema}.{table_name}"] = new_table

        for table_name in table_names:
            original_table = metadata.tables[f"{schema}.{table_name}"]
            new_table = table_mapping[f"{schema}.{table_name}"]

            for fk in original_table.foreign_key_constraints:
                try:
                    local_cols = []
                    remote_cols = []

                    for element in fk.elements:
                        local_col_name = element.parent.name
                        if local_col_name in new_table.c:
                            local_cols.append(new_table.c[local_col_name])

                        target_table_name = element.column.table.name
                        target_col_name = element.column.name

                        target_table = None
                        for clean_table in bare_metadata.tables.values():
                            if clean_table.name == target_table_name:
                                target_table = clean_table
                                break

                        if (
                            target_table is not None
                            and target_col_name in target_table.c
                        ):
                            remote_cols.append(target_table.c[target_col_name])

                    if (
                        len(local_cols) > 0
                        and len(remote_cols) > 0
                        and len(local_cols) == len(remote_cols)
                    ):
                        new_fk = ForeignKeyConstraint(
                            local_cols, remote_cols, name=fk.name
                        )
                        new_table.append_constraint(new_fk)
                except Exception as e:
                    # Skip foreign keys that can't be processed
                    print(f"Warning: Skipping foreign key {fk.name}: {str(e)}")
                    continue

        # Temporary DOT file
        temp_dot = "temp_erd.dot"

        # Generate DOT with eralchemy2
        render_er(bare_metadata, temp_dot)

        # Post-process the DOT file to ensure no schema prefixes remain
        with open(temp_dot, "r") as f:
            dot_content = f.read()

        # This regex replaces "schema.tablename" with just "tablename"
        import re

        dot_content = re.sub(rf"\b{re.escape(schema)}\.(\w+)", r"\1", dot_content)

        with open(temp_dot, "w") as f:
            f.write(dot_content)

        # Ensure old file is removed if it exists
        if os.path.exists(output_file):
            os.remove(output_file)

        # Convert DOT → WEBP with Graphviz
        subprocess.run(
            [
                "dot",
                f"-Gsize={width},{height}",
                f"-Gdpi={dpi}",
                "-Twebp",
                temp_dot,
                "-o",
                output_file,
            ],
            check=True,
        )
        os.remove(temp_dot)
        print(f"✅ ERD saved to {output_file}")
        print(f"Schema: {schema}")
        print(f"Excluded: {', '.join(excluded_tables) if excluded_tables else 'None'}")
    except subprocess.CalledProcessError:
        print("Error: Graphviz not found. Please install it.")
    except Exception as e:
        print(f"Error: {str(e)}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate ERD from PostgreSQL schema.")
    parser.add_argument("connection", help="Full PostgreSQL connection string")
    parser.add_argument("schema", help="Schema name to generate ERD for")
    parser.add_argument("output", help="Output path for the ERD image (WEBP)")
    args = parser.parse_args()
    generate_pg_erd(
        connection_string=args.connection,
        schema=args.schema,
        output_file=args.output,
        excluded_tables=[],
        width=8,
        height=8,
        dpi=200,
    )
