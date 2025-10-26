using Microsoft.Data.SqlClient;

var conn = "Server=localhost,1433;Database=OutboxDb;User=sa;Password=Admin@123;Encrypt=False;TrustServerCertificate=True;";

if (args.Length > 0) conn = args[0];

await using var connObj = new SqlConnection(conn);

try
{
    await connObj.OpenAsync();
    Console.WriteLine("Connected to DB\n");

    // First check migrations
    await using var cmd1 = connObj.CreateCommand();
    cmd1.CommandText = "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId;";
    await using var reader1 = await cmd1.ExecuteReaderAsync();

    Console.WriteLine("Migrations:");
    Console.WriteLine("-------------------------------");
    while (await reader1.ReadAsync())
    {
        Console.WriteLine($"{reader1.GetString(0)} - {reader1.GetString(1)}");
    }
    Console.WriteLine("-------------------------------\n");
    await reader1.CloseAsync();

    // Then check tables
    var tables = new[] { "OutboxMessage", "OutboxMessages", "OutboxState", "InboxState" };
    foreach (var table in tables)
    {
        await using var cmd2 = connObj.CreateCommand();
        cmd2.CommandText = $@"
IF OBJECT_ID(N'{table}') IS NULL 
    SELECT '{table}' as TableName, CAST(-1 as int) as [RowCount], 'Table does not exist' as Status, NULL as Columns, NULL as TableDefinition
ELSE 
BEGIN
    SELECT '{table}' as TableName,
           (SELECT COUNT(*) FROM [{table}]) as [RowCount],
           'Exists' as Status,
           OBJECT_DEFINITION(OBJECT_ID(N'{table}')) as TableDefinition,
           (SELECT STRING_AGG(COLUMN_NAME + ' ' + DATA_TYPE + 
                    CASE 
                        WHEN CHARACTER_MAXIMUM_LENGTH IS NOT NULL THEN '(' + CAST(CHARACTER_MAXIMUM_LENGTH AS varchar) + ')'
                        ELSE ''
                    END, ', ')
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = '{table}') as Columns;
END;";

        await using var reader2 = await cmd2.ExecuteReaderAsync();
        while (await reader2.ReadAsync())
        {
            var tableName = reader2.GetString(0);
            var rowCount = reader2.GetInt32(1);
            var status = reader2.GetString(2);
            Console.WriteLine($"{tableName}: {rowCount} rows - {status}");
            
            if (!reader2.IsDBNull(3))
            {
                Console.WriteLine($"Table Definition:\n{reader2.GetString(3)}\n");
            }
            if (!reader2.IsDBNull(4))
            {
                Console.WriteLine($"Columns: {reader2.GetString(4)}\n");
            }
            else
            {
                Console.WriteLine();
            }
        }
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}

return 0;