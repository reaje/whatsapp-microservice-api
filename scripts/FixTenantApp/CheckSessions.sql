SET search_path TO whatsapp_service;

-- Count all sessions
SELECT COUNT(*) as total_sessions FROM whatsapp_sessions;

-- Count sessions for the tenant
SELECT COUNT(*) as tenant_sessions 
FROM whatsapp_sessions 
WHERE tenant_id = 'b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d'::uuid;

-- Count active sessions for the tenant
SELECT COUNT(*) as active_tenant_sessions 
FROM whatsapp_sessions 
WHERE tenant_id = 'b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d'::uuid 
AND is_active = true;

-- Show all sessions for the tenant
SELECT id, phone_number, provider_type, is_active, created_at
FROM whatsapp_sessions 
WHERE tenant_id = 'b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d'::uuid;
