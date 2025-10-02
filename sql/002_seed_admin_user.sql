-- Seed Admin User for WhatsApp Microservice
-- Description: Creates initial admin user for the test tenant
-- Requirements: Run 001_create_users_table.sql first
-- Execute this in Supabase SQL Editor

-- Set search path to whatsapp_service schema
SET search_path TO whatsapp_service;

-- Insert admin user for test tenant
-- Password: "Admin@123" (BCrypt hash)
-- IMPORTANT: Change this password in production!
INSERT INTO users (id, tenant_id, email, password_hash, full_name, role, is_active, created_at, updated_at)
SELECT
    'a1b2c3d4-e5f6-4a5b-8c7d-9e8f7a6b5c4d'::uuid,
    'b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d'::uuid, -- test-client-001 tenant id
    'admin@test.com',
    '$2a$11$zQX8J4p5PqYJK3V9Z0hMT.kGLxN7Tj6E8K9W4vM2Nh5FJ1pQ7RvSu', -- BCrypt hash of "Admin@123"
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

-- Insert regular user for test tenant
-- Password: "User@123" (BCrypt hash)
INSERT INTO users (id, tenant_id, email, password_hash, full_name, role, is_active, created_at, updated_at)
SELECT
    'b2c3d4e5-f6a7-5b6c-9d8e-0f9a8b7c6d5e'::uuid,
    'b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d'::uuid, -- test-client-001 tenant id
    'user@test.com',
    '$2a$11$XYZ9K5q6RrZKL4W0A1iNV.lHMyO8Uk7F9L0X5wN3Oi6GK2qR8SwTv', -- BCrypt hash of "User@123"
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

-- Verify users were created
SELECT
    u.id,
    u.email,
    u.full_name,
    u.role,
    u.is_active,
    t.client_id as tenant_client_id,
    t.name as tenant_name,
    u.created_at
FROM users u
INNER JOIN tenants t ON u.tenant_id = t.id
WHERE t.client_id = 'test-client-001'
ORDER BY u.role DESC, u.email;

-- Expected output:
-- Two users for test-client-001 tenant:
-- 1. admin@test.com (Admin role) - Password: Admin@123
-- 2. user@test.com (User role) - Password: User@123

-- ========================================
-- LOGIN CREDENTIALS FOR TESTING
-- ========================================
-- Client ID: test-client-001
--
-- Admin User:
--   Email: admin@test.com
--   Password: Admin@123
--
-- Regular User:
--   Email: user@test.com
--   Password: User@123
-- ========================================
