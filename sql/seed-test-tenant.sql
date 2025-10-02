-- Seed Test Tenant for WhatsApp Microservice
-- Execute this in Supabase SQL Editor

-- Set search path to whatsapp_service schema
SET search_path TO whatsapp_service;

-- Insert test tenant (idempotent - only inserts if not exists)
INSERT INTO tenants (id, client_id, name, settings, created_at, updated_at)
SELECT
    'b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d'::uuid,
    'test-client-001',
    'Test Tenant for Development',
    '{
        "max_sessions": 10,
        "max_messages_per_day": 1000,
        "webhook_url": null,
        "features": {
            "ai_enabled": true,
            "media_enabled": true,
            "location_enabled": true
        }
    }'::jsonb,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM tenants WHERE client_id = 'test-client-001'
);

-- Verify tenant was created
SELECT
    id,
    client_id,
    name,
    created_at,
    settings->'max_sessions' as max_sessions
FROM tenants
WHERE client_id = 'test-client-001';

-- Expected output:
-- id: b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d
-- client_id: test-client-001
-- name: Test Tenant for Development
-- max_sessions: 10
