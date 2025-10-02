# SQL Migrations - WhatsApp Microservice

Este diretório contém as migrações SQL para o banco de dados Supabase do microserviço WhatsApp.

## 📋 Ordem de Execução

Execute os scripts na seguinte ordem no **Supabase SQL Editor**:

### 1. Criar Tabela de Usuários
```sql
-- Arquivo: 001_create_users_table.sql
-- Descrição: Cria tabela users com autenticação e autorização
```

**Execute este script primeiro**. Ele cria:
- Tabela `users` com todos os campos necessários
- Índices para performance
- Trigger para atualizar `updated_at` automaticamente
- Foreign key para `tenants` com cascade delete
- Constraint única para email por tenant

### 2. Popular Usuários de Teste
```sql
-- Arquivo: 002_seed_admin_user.sql
-- Descrição: Cria usuários admin e regular para teste
```

**Execute este script depois**. Ele cria dois usuários para o tenant `test-client-001`:

#### 👤 Usuário Admin
- **Email**: `admin@test.com`
- **Senha**: `Admin@123`
- **Role**: `Admin`

#### 👤 Usuário Regular
- **Email**: `user@test.com`
- **Senha**: `User@123`
- **Role**: `User`

### 3. Popular Tenant de Teste (se ainda não executado)
```sql
-- Arquivo: seed-test-tenant.sql
-- Descrição: Cria tenant de teste
```

Se você ainda não criou o tenant `test-client-001`, execute este script também.

## 🚀 Como Executar no Supabase

1. Acesse o [Supabase Dashboard](https://supabase.com/dashboard)
2. Selecione seu projeto
3. Vá em **SQL Editor** no menu lateral
4. Clique em **New query**
5. Copie e cole o conteúdo de cada arquivo SQL na ordem acima
6. Clique em **Run** para executar

## 🔐 Credenciais de Login

Após executar as migrações, você pode fazer login no sistema com:

### Frontend (http://localhost:3000)
```
Client ID: test-client-001
Email: admin@test.com
Password: Admin@123
```

### Backend API (http://localhost:5000)
```
POST /api/v1/auth/login
Content-Type: application/json

{
  "clientId": "test-client-001",
  "email": "admin@test.com",
  "password": "Admin@123"
}
```

**Resposta esperada:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 86400,
  "user": {
    "id": "a1b2c3d4-e5f6-4a5b-8c7d-9e8f7a6b5c4d",
    "email": "admin@test.com",
    "fullName": "Test Admin User",
    "role": "Admin",
    "tenantId": "b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d",
    "tenantName": "Test Tenant for Development"
  }
}
```

## 🔑 JWT Token Claims

O token JWT gerado contém os seguintes claims:

- `sub`: User ID (Guid)
- `email`: Email do usuário
- `jti`: Token ID único
- `tenant_id`: Tenant ID (Guid)
- `client_id`: Client ID do tenant
- `role`: Role do usuário (Admin/User)
- `full_name`: Nome completo do usuário

## 🛡️ Segurança

⚠️ **IMPORTANTE**: As senhas nos scripts de seed são apenas para **desenvolvimento/teste**.

**Em produção:**
1. Altere o `Jwt:Key` no `appsettings.json` para uma chave forte e aleatória
2. Não use as senhas padrão (`Admin@123` e `User@123`)
3. Crie novos usuários via endpoint `/api/v1/auth/register`
4. Considere adicionar autenticação 2FA
5. Implemente rate limiting nos endpoints de auth

## 📊 Verificar Migrações

Para verificar se as migrações foram aplicadas corretamente, execute:

```sql
-- Verificar estrutura da tabela users
SELECT
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'whatsapp_service'
    AND table_name = 'users'
ORDER BY ordinal_position;

-- Verificar usuários criados
SELECT
    u.email,
    u.full_name,
    u.role,
    u.is_active,
    t.client_id,
    t.name as tenant_name
FROM whatsapp_service.users u
INNER JOIN whatsapp_service.tenants t ON u.tenant_id = t.id
WHERE t.client_id = 'test-client-001';
```

## 🧪 Testar Autenticação

### 1. Via curl
```bash
# Login
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "test-client-001",
    "email": "admin@test.com",
    "password": "Admin@123"
  }'

# Register
curl -X POST http://localhost:5000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "test-client-001",
    "email": "newuser@test.com",
    "password": "NewUser@123",
    "fullName": "New Test User",
    "role": "User"
  }'
```

### 2. Via Scalar API Documentation
Acesse http://localhost:5000/scalar/v1 e teste os endpoints de autenticação diretamente na interface.

## 🔄 Rollback

Para reverter as migrações (CUIDADO: isto apaga dados):

```sql
-- Remover usuários
DROP TABLE IF EXISTS whatsapp_service.users CASCADE;

-- Remover função de trigger
DROP FUNCTION IF EXISTS update_updated_at_column() CASCADE;
```

## 📝 Adicionar Novas Migrações

Ao criar novas migrações, siga o padrão de nomenclatura:

```
XXX_description.sql
```

Onde:
- `XXX` = número sequencial (003, 004, etc.)
- `description` = descrição curta em snake_case

Exemplo: `003_add_two_factor_auth.sql`
