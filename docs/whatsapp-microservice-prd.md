# PRD - Microserviço de Comunicação WhatsApp Multi-Tenant

## Communication Language

**Portuguese (Brazilian)** - Toda a comunicação deve ser no idioma Portuguese Brazilian (pt-BR).

## Documentação

**SCALAR**

## 1. Visão Geral

### 1.1 Objetivo

Desenvolver um microserviço robusto e escalável para comunicação via WhatsApp, integrando Baileys (WhatsApp Web) e Meta WhatsApp Business API, com suporte multi-tenant e integração com agentes de IA.

### 1.2 Escopo

- Microserviço em C# .NET 9
- Suporte multi-tenant com isolamento por client_id
- Integração dupla: Baileys e Meta WhatsApp Business API
- Integração com agentes de IA especializados
- Supabase como backend (PostgreSQL, Realtime, Webhooks)
- Testes E2E completos

## 2. Arquitetura da Solução

### 2.1 Arquitetura de Alto Nível

```
┌─────────────────────────────────────────────────────────────┐
│                     Frontend React                           │
├─────────────────────────────────────────────────────────────┤
│                    API Gateway (.NET 9)                      │
├─────────────────────────────────────────────────────────────┤
│              WhatsApp Communication Service                  │
│  ┌──────────────────┬──────────────────┬─────────────────┐ │
│  │  Provider Layer  │   AI Agent Layer │  Message Layer   │ │
│  │  - Baileys       │   - Agent Router │  - Text         │ │
│  │  - Meta API      │   - Context Mgmt │  - Media        │ │
│  └──────────────────┴──────────────────┴─────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│                    Supabase Backend                          │
│  ┌──────────────┬─────────────────┬──────────────────────┐ │
│  │  PostgreSQL  │  Realtime Events │      Webhooks        │ │
│  └──────────────┴─────────────────┴──────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 Estrutura do Projeto

```
WhatsAppMicroservice/
├── src/
│   ├── WhatsApp.API/
│   │   ├── Controllers/
│   │   │   ├── MessageController.cs
│   │   │   ├── SessionController.cs
│   │   │   └── TenantController.cs
│   │   ├── Middleware/
│   │   │   ├── TenantMiddleware.cs
│   │   │   └── AuthenticationMiddleware.cs
│   │   └── Program.cs
│   │
│   ├── WhatsApp.Core/
│   │   ├── Entities/
│   │   │   ├── Tenant.cs
│   │   │   ├── Message.cs
│   │   │   ├── Session.cs
│   │   │   └── AIAgent.cs
│   │   ├── Interfaces/
│   │   │   ├── IWhatsAppProvider.cs
│   │   │   ├── IAIAgentService.cs
│   │   │   └── ITenantService.cs
│   │   └── Enums/
│   │       ├── ProviderType.cs
│   │       └── MessageType.cs
│   │
│   ├── WhatsApp.Infrastructure/
│   │   ├── Providers/
│   │   │   ├── BaileysProvider.cs
│   │   │   └── MetaApiProvider.cs
│   │   ├── Services/
│   │   │   ├── AIAgentService.cs
│   │   │   ├── MessageService.cs
│   │   │   └── TenantService.cs
│   │   ├── Data/
│   │   │   ├── SupabaseContext.cs
│   │   │   └── Repositories/
│   │   └── Realtime/
│   │       └── SupabaseRealtimeService.cs
│   │
│   └── WhatsApp.Tests/
│       ├── E2E/
│       ├── Integration/
│       └── Unit/
│
├── docker-compose.yml
└── README.md
```

## 3. Modelos de Dados

### 3.1 Schema do Banco de Dados (PostgreSQL/Supabase)

## toda a modelagem deve ser gerada por dentro do sistema através do entity framework

```sql
-- Tabela de Tenants
CREATE TABLE tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id VARCHAR(100) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    settings JSONB DEFAULT '{}',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Tabela de Sessões WhatsApp
CREATE TABLE whatsapp_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id),
    phone_number VARCHAR(20) NOT NULL,
    provider_type VARCHAR(50) NOT NULL, -- 'baileys' ou 'meta_api'
    session_data JSONB,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(tenant_id, phone_number)
);

-- Tabela de Mensagens
CREATE TABLE messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id),
    session_id UUID REFERENCES whatsapp_sessions(id),
    message_id VARCHAR(255) UNIQUE,
    from_number VARCHAR(20),
    to_number VARCHAR(20),
    message_type VARCHAR(50), -- 'text', 'image', 'audio', 'location', 'document'
    content JSONB,
    status VARCHAR(50), -- 'pending', 'sent', 'delivered', 'read', 'failed'
    ai_processed BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Tabela de Agentes IA
CREATE TABLE ai_agents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id),
    name VARCHAR(255) NOT NULL,
    type VARCHAR(100), -- 'real_estate', 'customer_support', etc
    configuration JSONB,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Tabela de Conversações com IA
CREATE TABLE ai_conversations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id),
    agent_id UUID REFERENCES ai_agents(id),
    session_id UUID REFERENCES whatsapp_sessions(id),
    context JSONB,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Índices para performance
CREATE INDEX idx_messages_tenant_id ON messages(tenant_id);
CREATE INDEX idx_messages_session_id ON messages(session_id);
CREATE INDEX idx_sessions_tenant_id ON whatsapp_sessions(tenant_id);
CREATE INDEX idx_messages_created_at ON messages(created_at DESC);
```

## 4. APIs e Endpoints

### 4.1 Autenticação e Multi-tenancy

Todas as requisições devem incluir:

- Header `X-Client-Id`: Identificador do tenant
- Header `Authorization`: Bearer token JWT

### 4.2 Endpoints Principais

```yaml
# Gestão de Sessões
POST   /api/v1/sessions/initialize
GET    /api/v1/sessions/status
DELETE /api/v1/sessions/disconnect

# Envio de Mensagens
POST   /api/v1/messages/send
POST   /api/v1/messages/send-bulk
GET    /api/v1/messages/{messageId}/status

# Tipos de Mensagem Específicos
POST   /api/v1/messages/text
POST   /api/v1/messages/media
POST   /api/v1/messages/location
POST   /api/v1/messages/audio

# Gestão de Agentes IA
POST   /api/v1/agents/create
GET    /api/v1/agents/list
PUT    /api/v1/agents/{agentId}/configure
POST   /api/v1/agents/{agentId}/assign-conversation

# Configurações do Tenant
GET    /api/v1/tenant/settings
PUT    /api/v1/tenant/settings
POST   /api/v1/tenant/provider-switch

# Webhooks
POST   /api/v1/webhooks/incoming-message
POST   /api/v1/webhooks/status-update
```

### 4.3 Exemplos de Request/Response

#### Enviar Mensagem de Texto

```json
// Request
POST /api/v1/messages/text
Headers: 
  X-Client-Id: tenant-123
  Authorization: Bearer <token>

{
  "to": "5511999999999",
  "content": "Olá! Como posso ajudar?",
  "provider": "auto", // 'baileys', 'meta_api', or 'auto'
  "enableAI": true,
  "agentId": "agent-uuid"
}

// Response
{
  "messageId": "msg-uuid",
  "status": "sent",
  "provider": "baileys",
  "timestamp": "2024-01-10T10:00:00Z"
}
```

## 5. Implementação Detalhada

### 5.1 Provider Interface

```csharp
public interface IWhatsAppProvider
{
    Task<SessionStatus> InitializeAsync(string phoneNumber, TenantConfig config);
    Task<MessageResult> SendTextAsync(string to, string content);
    Task<MessageResult> SendMediaAsync(string to, byte[] media, MediaType type);
    Task<MessageResult> SendLocationAsync(string to, double lat, double lng);
    Task<MessageResult> SendAudioAsync(string to, byte[] audio);
    Task DisconnectAsync();
    event EventHandler<IncomingMessage> OnMessageReceived;
}
```

### 5.2 AI Agent Service

```csharp
public interface IAIAgentService
{
    Task<AIResponse> ProcessMessageAsync(
        string tenantId, 
        string agentId, 
        IncomingMessage message, 
        ConversationContext context);
    
    Task<Agent> ConfigureAgentAsync(string tenantId, AgentConfig config);
    Task<List<Agent>> GetAgentsAsync(string tenantId);
}
```

### 5.3 Multi-tenancy Middleware

```csharp
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITenantService _tenantService;

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = context.Request.Headers["X-Client-Id"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(clientId))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Client-Id header is required");
            return;
        }

        var tenant = await _tenantService.GetByClientIdAsync(clientId);
        if (tenant == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid tenant");
            return;
        }

        context.Items["Tenant"] = tenant;
        await _next(context);
    }
}
```

## 6. Integração com Supabase

### 6.1 Realtime Events

```csharp
public class SupabaseRealtimeService
{
    private readonly ISupabaseClient _supabase;

    public async Task SubscribeToMessages(string tenantId)
    {
        await _supabase
            .From<Message>()
            .On(SupabaseEventType.Insert, (sender, e) => 
            {
                // Processar nova mensagem
                HandleNewMessage(e.Response);
            })
            .Where(m => m.TenantId == tenantId)
            .Subscribe();
    }

    public async Task PublishMessageStatus(string messageId, string status)
    {
        await _supabase
            .From<Message>()
            .Update(new { status })
            .Where(m => m.Id == messageId)
            .Execute();
    }
}
```

### 6.2 Webhook Configuration

```csharp
[ApiController]
[Route("api/v1/webhooks")]
public class WebhookController : ControllerBase
{
    [HttpPost("supabase")]
    public async Task<IActionResult> HandleSupabaseWebhook(
        [FromBody] SupabaseWebhookPayload payload)
    {
        // Validar assinatura do webhook
        if (!ValidateWebhookSignature(Request))
            return Unauthorized();

        // Processar evento
        switch (payload.Type)
        {
            case "INSERT":
                await ProcessNewRecord(payload);
                break;
            case "UPDATE":
                await ProcessUpdate(payload);
                break;
        }

        return Ok();
    }
}
```

## 7. Testes E2E

### 7.1 Estrutura de Testes

```csharp
[TestClass]
public class WhatsAppE2ETests : IClassFixture<WhatsAppTestFixture>
{
    private readonly WhatsAppTestFixture _fixture;
    
    [Test]
    public async Task Should_SendMessage_WithBaileys_Successfully()
    {
        // Arrange
        var tenant = await _fixture.CreateTestTenant();
        var session = await _fixture.InitializeBaileysSession(tenant);
        
        // Act
        var result = await _fixture.Client.PostAsJsonAsync(
            "/api/v1/messages/text",
            new { 
                to = "5511999999999", 
                content = "Test message",
                provider = "baileys"
            },
            headers: new { "X-Client-Id" = tenant.ClientId });
        
        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var message = await result.Content.ReadAsAsync<MessageResult>();
        message.Status.Should().Be("sent");
    }
    
    [Test]
    public async Task Should_ProcessMessage_WithAIAgent_Successfully()
    {
        // Arrange
        var tenant = await _fixture.CreateTestTenant();
        var agent = await _fixture.CreateAIAgent(tenant, "real_estate");
        
        // Act
        var incomingMessage = new IncomingMessage 
        {
            From = "5511999999999",
            Content = "Quero ver apartamentos de 2 quartos"
        };
        
        await _fixture.SimulateIncomingMessage(tenant, incomingMessage);
        
        // Assert
        var response = await _fixture.WaitForAIResponse();
        response.Should().NotBeNull();
        response.Content.Should().Contain("apartamentos");
    }
}
```

## 8. Configuração e Deploy

### 8.1 appsettings.json

```json
{
  "ConnectionStrings": {
    "Supabase": "postgresql://user:pass@db.supabase.co:5432/postgres"
  },
  "Supabase": {
    "Url": "https://project.supabase.co",
    "AnonKey": "your-anon-key",
    "ServiceKey": "your-service-key"
  },
  "WhatsApp": {
    "Baileys": {
      "WebSocketUrl": "wss://web.whatsapp.com/ws",
      "SessionPath": "./sessions"
    },
    "MetaAPI": {
      "BaseUrl": "https://graph.facebook.com/v18.0",
      "WebhookVerifyToken": "your-verify-token"
    }
  },
  "AI": {
    "DefaultModel": "gpt-4",
    "ApiKey": "your-api-key"
  }
}
```

### 8.2 Docker Compose

```yaml
version: '3.8'
services:
  whatsapp-api:
    build: .
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__Supabase=${SUPABASE_CONNECTION}
    depends_on:
      - redis
    
  redis:
    image: redis:alpine
    ports:
      - "6379:6379"
    
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
    depends_on:
      - whatsapp-api
```

## 9. Monitoramento e Observabilidade

### 9.1 Métricas Importantes

- Taxa de sucesso de envio de mensagens
- Tempo de resposta da IA
- Número de sessões ativas por tenant
- Taxa de uso Baileys vs Meta API
- Latência de processamento de mensagens

### 9.2 Logging Estruturado

```csharp
public class MessageService
{
    private readonly ILogger<MessageService> _logger;
    
    public async Task<MessageResult> SendMessage(MessageRequest request)
    {
        using var activity = Activity.StartActivity("SendMessage");
        activity?.SetTag("tenant.id", request.TenantId);
        activity?.SetTag("provider", request.Provider);
        
        _logger.LogInformation("Sending message {MessageType} to {Recipient} via {Provider}",
            request.Type, request.To, request.Provider);
        
        try
        {
            var result = await ProcessMessage(request);
            
            _logger.LogInformation("Message sent successfully {MessageId}",
                result.MessageId);
                
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to {Recipient}",
                request.To);
            throw;
        }
    }
}
```

## 10. Segurança

### 10.1 Principais Considerações

- Criptografia de dados sensíveis (session_data, tokens)
- Rate limiting por tenant
- Validação de webhook signatures
- Isolamento completo entre tenants
- Auditoria de todas as operações
- Sanitização de inputs para prevenir injeções

### 10.2 Rate Limiting

```csharp
services.AddRateLimiter(options =>
{
    options.AddPolicy("tenant-rate-limit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Request.Headers["X-Client-Id"].ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

## 11. Roadmap de Implementação

### Fase 1 - MVP (2 semanas)

- [ ] Setup inicial do projeto .NET 9
- [ ] Integração básica com Supabase
- [ ] Implementação do provider Baileys
- [ ] Sistema de multi-tenancy
- [ ] Envio de mensagens de texto
- [ ] Testes unitários básicos

### Fase 2 - Features Core (2 semanas)

- [ ] Integração Meta WhatsApp Business API
- [ ] Chaveamento entre providers
- [ ] Envio de mídia, localização e áudio
- [ ] Sistema de webhooks
- [ ] Integração Supabase Realtime

### Fase 3 - IA e Automação (2 semanas)

- [ ] Integração com agentes de IA
- [ ] Sistema de contexto de conversação
- [ ] Templates de agentes especializados
- [ ] Dashboard de métricas

### Fase 4 - Produção (1 semana)

- [ ] Testes E2E completos
- [ ] Documentação da API
- [ ] Setup de CI/CD
- [ ] Monitoramento e alertas
- [ ] Deploy em produção

## 12. Considerações Finais

### 12.1 Escalabilidade

- Uso de cache Redis para sessões ativas
- Processamento assíncrono de mensagens
- Horizontal scaling com Kubernetes
- Particionamento de dados por tenant

### 12.2 Manutenibilidade

- Clean Architecture principles
- SOLID principles
- Comprehensive logging
- Automated testing
- Documentation as code

### 12.3 Performance

- Connection pooling para PostgreSQL
- Bulk operations quando possível
- Lazy loading de dados de sessão
- Compressão de payloads grandes

## 13. Anexos

### A. Exemplo de Configuração de Agente IA

```json
{
  "agentId": "real-estate-specialist",
  "name": "Especialista em Imóveis",
  "systemPrompt": "Você é um especialista em imóveis...",
  "capabilities": [
    "search_properties",
    "schedule_visits",
    "provide_documentation",
    "calculate_financing"
  ],
  "knowledgeBase": {
    "type": "vector_db",
    "connectionString": "..."
  },
  "responseSettings": {
    "maxTokens": 500,
    "temperature": 0.7,
    "language": "pt-BR"
  }
}
```

### B. Estrutura de Mensagem Completa

```json
{
  "id": "msg-uuid",
  "tenantId": "tenant-uuid",
  "sessionId": "session-uuid",
  "messageId": "whatsapp-msg-id",
  "from": "5511999999999",
  "to": "5511888888888",
  "type": "text",
  "content": {
    "text": "Mensagem de texto",
    "mediaUrl": null,
    "location": null,
    "audio": null
  },
  "metadata": {
    "provider": "baileys",
    "aiProcessed": true,
    "agentId": "agent-uuid",
    "timestamp": "2024-01-10T10:00:00Z",
    "deliveryStatus": "delivered"
  }
}
```
