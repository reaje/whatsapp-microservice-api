using Npgsql;

var connectionString = "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.yzhqgoofrxixndfcfucz;Password=8PrqjzQegAgFHnM4;Timeout=30;SSL Mode=Require;Trust Server Certificate=true;Search Path=whatsapp_service;";

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

Console.WriteLine("Connected to database");

// First, check current session state
var checkSql = @"
SET search_path TO whatsapp_service;

SELECT id, tenant_id, phone_number, status, is_active, provider_type
FROM sessions
WHERE id = '4fdc6fef-8029-4267-ac86-484f6ed2e052'::uuid;
";

await using (var cmd = new NpgsqlCommand(checkSql, connection))
await using (var reader = await cmd.ExecuteReaderAsync())
{
    Console.WriteLine("\n=== Current Session State ===");
    if (await reader.ReadAsync())
    {
        Console.WriteLine($"ID: {reader.GetGuid(0)}");
        Console.WriteLine($"TenantId: {reader.GetGuid(1)}");
        Console.WriteLine($"PhoneNumber: {reader.GetString(2)}");
        Console.WriteLine($"Status: {reader.GetString(3)}");
        Console.WriteLine($"IsActive: {reader.GetBoolean(4)}");
        Console.WriteLine($"ProviderType: {reader.GetString(5)}");
    }
    else
    {
        Console.WriteLine("Session not found!");
        return;
    }
}

// Update the session to set is_active = true
Console.WriteLine("\nUpdating session is_active to true...");

var updateSessionSql = @"
SET search_path TO whatsapp_service;

UPDATE sessions
SET is_active = true,
    updated_at = NOW()
WHERE id = '4fdc6fef-8029-4267-ac86-484f6ed2e052'::uuid;
";

await using (var cmd = new NpgsqlCommand(updateSessionSql, connection))
{
    var rowsAffected = await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"Updated {rowsAffected} session(s)");
}

// Verify the update
var verifySql = @"
SET search_path TO whatsapp_service;

SELECT id, tenant_id, phone_number, status, is_active, provider_type
FROM sessions
WHERE id = '4fdc6fef-8029-4267-ac86-484f6ed2e052'::uuid;
";

await using (var cmd2 = new NpgsqlCommand(verifySql, connection))
await using (var reader2 = await cmd2.ExecuteReaderAsync())
{
    Console.WriteLine("\n=== Updated Session State ===");
    while (await reader2.ReadAsync())
    {
        Console.WriteLine($"ID: {reader2.GetGuid(0)}");
        Console.WriteLine($"TenantId: {reader2.GetGuid(1)}");
        Console.WriteLine($"PhoneNumber: {reader2.GetString(2)}");
        Console.WriteLine($"Status: {reader2.GetString(3)}");
        Console.WriteLine($"IsActive: {reader2.GetBoolean(4)}");
        Console.WriteLine($"ProviderType: {reader2.GetString(5)}");
    }
}

Console.WriteLine("\n✅ Session updated successfully!");
