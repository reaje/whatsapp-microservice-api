-- Migration: Create Users Table
-- Description: Adds user authentication table for multi-tenant WhatsApp microservice
-- Schema: whatsapp_service
-- Execute this in Supabase SQL Editor

-- Set search path to whatsapp_service schema
SET search_path TO whatsapp_service;

-- Create users table
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    email VARCHAR(255) NOT NULL,
    password_hash TEXT NOT NULL,
    full_name VARCHAR(255) NOT NULL,
    role VARCHAR(50) NOT NULL DEFAULT 'User',
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_login_at TIMESTAMPTZ,

    -- Foreign key constraint to tenants table
    CONSTRAINT fk_users_tenant FOREIGN KEY (tenant_id)
        REFERENCES tenants(id) ON DELETE CASCADE,

    -- Unique constraint: one email per tenant
    CONSTRAINT uq_users_tenant_email UNIQUE(tenant_id, email)
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_users_tenant_id ON users(tenant_id);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_is_active ON users(is_active);

-- Create or replace function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger to automatically update updated_at column
DROP TRIGGER IF EXISTS set_users_updated_at ON users;
CREATE TRIGGER set_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Add comments for documentation
COMMENT ON TABLE users IS 'User authentication and authorization table for multi-tenant system';
COMMENT ON COLUMN users.id IS 'Unique identifier for the user';
COMMENT ON COLUMN users.tenant_id IS 'Reference to the tenant this user belongs to';
COMMENT ON COLUMN users.email IS 'User email address (unique within tenant)';
COMMENT ON COLUMN users.password_hash IS 'BCrypt hashed password';
COMMENT ON COLUMN users.full_name IS 'User full name';
COMMENT ON COLUMN users.role IS 'User role: Admin or User';
COMMENT ON COLUMN users.is_active IS 'Whether the user account is active';
COMMENT ON COLUMN users.created_at IS 'Timestamp when user was created';
COMMENT ON COLUMN users.updated_at IS 'Timestamp when user was last updated';
COMMENT ON COLUMN users.last_login_at IS 'Timestamp of last successful login';

-- Verify table was created
SELECT
    table_name,
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'whatsapp_service'
    AND table_name = 'users'
ORDER BY ordinal_position;

-- Show indexes
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'whatsapp_service'
    AND tablename = 'users';
