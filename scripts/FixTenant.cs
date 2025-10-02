using Npgsql;

var connectionString = "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.yzhqgoofrxixndfcfucz;Password=8PrqjzQegAgFHnM4;Timeout=30;SSL Mode=Require;Trust Server Certificate=true;Search Path=whatsapp_service;";

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

Console.WriteLine("Connected to database");

// Update the tenant's client_id
var updateTenantSql = @"
SET search_path TO whatsapp_service;

UPDATE tenants
SET client_id = 'test-client-001',
    updated_at = NOW()
WHERE id = 'b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d'::uuid;
";

await using (var cmd = new NpgsqlCommand(updateTenantSql, connection))
{
    var rowsAffected = await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"Updated {rowsAffected} tenant(s)");
}

// Verify the update
var verifySql = @"
SET search_path TO whatsapp_service;

SELECT id, client_id, name
FROM tenants
WHERE id = 'b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d'::uuid;
";

await using (var cmd = new NpgsqlCommand(verifySql, connection))
await using (var reader = await cmd.ExecuteReaderAsync())
{
    Console.WriteLine("\n=== Current Tenant ===");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"ID: {reader.GetGuid(0)}, ClientId: {reader.GetString(1)}, Name: {reader.GetString(2)}");
    }
}

Console.WriteLine("\n✅ Tenant fixed successfully!");
