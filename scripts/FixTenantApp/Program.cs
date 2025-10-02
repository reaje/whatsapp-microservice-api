using Npgsql;

var connectionString = "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.yzhqgoofrxixndfcfucz;Password=8PrqjzQegAgFHnM4;Timeout=30;SSL Mode=Require;Trust Server Certificate=true;Search Path=whatsapp_service;";

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

Console.WriteLine("Connected to database\n");

// Test the EXACT query that EF Core is running
Console.WriteLine("=== Testing EF Core Query ===");
var efQuerySql = @"
SELECT w.id, w.created_at, w.is_active, w.phone_number, w.provider_type, w.session_data, w.tenant_id, w.updated_at
FROM whatsapp_service.whatsapp_sessions AS w
INNER JOIN whatsapp_service.tenants AS t ON w.tenant_id = t.id
WHERE w.tenant_id = 'b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d'::uuid AND w.is_active;
";

await using (var cmd = new NpgsqlCommand(efQuerySql, connection))
await using (var reader = await cmd.ExecuteReaderAsync())
{
    Console.WriteLine("\nResults from EF Core query:");
    var rowCount = 0;
    while (await reader.ReadAsync())
    {
        rowCount++;
        Console.WriteLine($"Row {rowCount}:");
        Console.WriteLine($"  ID: {reader.GetGuid(0)}");
        Console.WriteLine($"  Phone: {reader.GetString(3)}");
        Console.WriteLine($"  IsActive: {reader.GetBoolean(2)}");
        Console.WriteLine($"  Provider: {reader.GetString(4)}");
    }

    if (rowCount == 0)
    {
        Console.WriteLine("NO ROWS RETURNED!");
    }
    else
    {
        Console.WriteLine($"\nTotal rows returned: {rowCount}");
    }
}

// Test without the join
Console.WriteLine("\n\n=== Testing Without Join ===");
var noJoinSql = @"
SELECT w.id, w.phone_number, w.is_active, w.provider_type, w.tenant_id
FROM whatsapp_service.whatsapp_sessions AS w
WHERE w.tenant_id = 'b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d'::uuid AND w.is_active;
";

await using (var cmd = new NpgsqlCommand(noJoinSql, connection))
await using (var reader = await cmd.ExecuteReaderAsync())
{
    Console.WriteLine("\nResults without join:");
    var rowCount = 0;
    while (await reader.ReadAsync())
    {
        rowCount++;
        Console.WriteLine($"Row {rowCount}:");
        Console.WriteLine($"  ID: {reader.GetGuid(0)}");
        Console.WriteLine($"  Phone: {reader.GetString(1)}");
        Console.WriteLine($"  IsActive: {reader.GetBoolean(2)}");
        Console.WriteLine($"  TenantId: {reader.GetGuid(4)}");
    }

    if (rowCount == 0)
    {
        Console.WriteLine("NO ROWS RETURNED!");
    }
    else
    {
        Console.WriteLine($"\nTotal rows returned: {rowCount}");
    }
}
