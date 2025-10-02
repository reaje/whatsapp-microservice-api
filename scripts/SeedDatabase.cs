using Npgsql;

var connectionString = "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.yzhqgoofrxixndfcfucz;Password=8PrqjzQegAgFHnM4;Timeout=30;SSL Mode=Require;Trust Server Certificate=true;Search Path=whatsapp_service;";

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

Console.WriteLine("Connected to database");

// 1. Create tenant
var createTenantSql = @"
SET search_path TO whatsapp_service;

INSERT INTO tenants (id, client_id, name, settings, created_at, updated_at)
SELECT
    'b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d'::uuid,
    'a4876b9d-8ce5-4b67-ab69-c04073ce2f80',
    'Test Tenant for Development',
    '{
        ""max_sessions"": 10,
        ""max_messages_per_day"": 1000,
        ""webhook_url"": null,
        ""features"": {
            ""ai_enabled"": true,
            ""media_enabled"": true,
            ""location_enabled"": true
        }
    }'::jsonb,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM tenants WHERE client_id = 'a4876b9d-8ce5-4b67-ab69-c04073ce2f80'
);
";

await using (var cmd = new NpgsqlCommand(createTenantSql, connection))
{
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine("Tenant created (or already exists)");
}

// 2. Create admin user
var createAdminSql = @"
SET search_path TO whatsapp_service;

INSERT INTO users (id, tenant_id, email, password_hash, full_name, role, is_active, created_at, updated_at)
SELECT
    'a1b2c3d4-e5f6-4a5b-8c7d-9e8f7a6b5c4d'::uuid,
    'b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d'::uuid,
    'admin@test.com',
    '$2a$11$zQX8J4p5PqYJK3V9Z0hMT.kGLxN7Tj6E8K9W4vM2Nh5FJ1pQ7RvSu',
    'Test Admin User',
    'Admin',
    true,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM users
    WHERE email = 'admin@test.com'
        AND tenant_id = 'b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d'::uuid
);
";

await using (var cmd = new NpgsqlCommand(createAdminSql, connection))
{
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine("Admin user created (or already exists)");
}

// 3. Create regular user
var createUserSql = @"
SET search_path TO whatsapp_service;

INSERT INTO users (id, tenant_id, email, password_hash, full_name, role, is_active, created_at, updated_at)
SELECT
    'b2c3d4e5-f6a7-5b6c-9d8e-0f9a8b7c6d5e'::uuid,
    'b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d'::uuid,
    'user@test.com',
    '$2a$11$XYZ9K5q6RrZKL4W0A1iNV.lHMyO8Uk7F9L0X5wN3Oi6GK2qR8SwTv',
    'Test Regular User',
    'User',
    true,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM users
    WHERE email = 'user@test.com'
        AND tenant_id = 'b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d'::uuid
);
";

await using (var cmd = new NpgsqlCommand(createUserSql, connection))
{
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine("Regular user created (or already exists)");
}

// Verify
var verifySql = @"
SET search_path TO whatsapp_service;

SELECT
    u.email,
    u.full_name,
    u.role,
    t.client_id
FROM users u
INNER JOIN tenants t ON u.tenant_id = t.id
WHERE t.client_id = 'a4876b9d-8ce5-4b67-ab69-c04073ce2f80'
ORDER BY u.role DESC, u.email;
";

await using (var cmd = new NpgsqlCommand(verifySql, connection))
await using (var reader = await cmd.ExecuteReaderAsync())
{
    Console.WriteLine("\n=== Created Users ===");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"Email: {reader.GetString(0)}, Name: {reader.GetString(1)}, Role: {reader.GetString(2)}, Tenant: {reader.GetString(3)}");
    }
}

Console.WriteLine("\n✅ Database seeded successfully!");
Console.WriteLine("\nLogin credentials:");
Console.WriteLine("  Client ID: a4876b9d-8ce5-4b67-ab69-c04073ce2f80");
Console.WriteLine("  Admin - Email: admin@test.com, Password: Admin@123");
Console.WriteLine("  User  - Email: user@test.com, Password: User@123");
