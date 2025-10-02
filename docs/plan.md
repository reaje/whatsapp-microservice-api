# Plano de Implementação - Microserviço WhatsApp Multi-Tenant

## Visão Geral

Este documento apresenta o plano de implementação detalhado do microserviço de comunicação WhatsApp, baseado no PRD. O projeto está dividido em 4 fases principais com duração total estimada de 7 semanas.

---

## Fase 1 - MVP

### Objetivo

Estabelecer a fundação do projeto com funcionalidades básicas de envio de mensagens via Baileys.

### 1.1 Setup Inicial do Projeto

**Prioridade: Alta**

- [ ] Criar solução .NET 9 com estrutura de projetos:
  - WhatsApp.API
  - WhatsApp.Core
  - WhatsApp.Infrastructure
  - WhatsApp.Tests
- [ ] Configurar Docker e docker-compose.yml
- [ ] Setup de CI/CD inicial (GitHub Actions ou similar)
- [ ] Configurar padrões de código (EditorConfig, analyzers)
- [ ] Documentação básica (README.md)

**Dependências:** Nenhuma

---

### 1.2 Integração com Supabase

**Prioridade: Alta**

- [ ] Instalar e configurar bibliotecas Supabase (.NET)
- [ ] Criar schema do banco de dados (executar SQL do PRD):
  - Tabela `tenants`
  - Tabela `whatsapp_sessions`
  - Tabela `messages`
  - Tabela `ai_agents`
  - Tabela `ai_conversations`
  - Índices de performance
- [ ] Implementar `SupabaseContext.cs`
- [ ] Criar repositories básicos:
  - `TenantRepository`
  - `SessionRepository`
  - `MessageRepository`
- [ ] Configurar connection strings e variáveis de ambiente
- [ ] Testes de conexão e operações CRUD

**String de Conexão (appsettings.json):**

```json
{
  "ConnectionStrings": {
    "RulesEngineDatabase": "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.yzhqgoofrxixndfcfucz;Password=8PrqjzQegAgFHnM4;Timeout=30;Command Timeout=30;SSL Mode=Require;Trust Server Certificate=true;Search Path=whatsapp_service;Pooling=true;MinPoolSize=2;MaxPoolSize=10;"
  }
}
```

**⚠️ Importante:**

- Esta connection string contém credenciais sensíveis
- NÃO commitar em repositórios públicos
- Usar variáveis de ambiente em produção
- O schema utilizado é `whatsapp_service` (definido em Search Path)

**Dependências:** Setup inicial do projeto

---

### 1.3 Sistema Multi-Tenancy

**Prioridade: Alta**

- [ ] Implementar `TenantMiddleware.cs` para interceptar X-Client-Id
- [ ] Criar `ITenantService` e `TenantService`
- [ ] Implementar validação e carregamento de tenant
- [ ] Adicionar contexto de tenant no HttpContext.Items
- [ ] Criar `TenantController` com endpoints básicos:
  - GET /api/v1/tenant/settings
  - PUT /api/v1/tenant/settings
- [ ] Testes unitários do middleware
- [ ] Testes de isolamento entre tenants

**Dependências:** Integração com Supabase

---

### 1.4 Provider Baileys

**Prioridade: Alta**

- [ ] Pesquisar e integrar biblioteca Baileys (Node.js wrapper ou alternativa .NET)
- [ ] Implementar `IWhatsAppProvider` interface
- [ ] Criar `BaileysProvider.cs`:
  - Método `InitializeAsync()`
  - Método `SendTextAsync()`
  - Método `DisconnectAsync()`
  - Event `OnMessageReceived`
- [ ] Gerenciamento de sessões (salvamento e restauração)
- [ ] Tratamento de QR Code para autenticação
- [ ] Tratamento de erros e reconexão
- [ ] Testes de integração com WhatsApp

**Dependências:** Sistema Multi-Tenancy

---

### 1.5 API de Mensagens de Texto

**Prioridade: Alta**

- [ ] Implementar `MessageController`:
  - POST /api/v1/messages/text
  - GET /api/v1/messages/{messageId}/status
- [ ] Criar `MessageService` para orquestrar envio
- [ ] Implementar DTOs (Request/Response)
- [ ] Validação de inputs
- [ ] Persistência de mensagens no banco
- [ ] Documentação Swagger/OpenAPI
- [ ] Testes de integração end-to-end

**Dependências:** Provider Baileys

---

### 1.6 Testes Unitários MVP

**Prioridade: Média**

- [ ] Configurar framework de testes (xUnit/NUnit)
- [ ] Testes para TenantMiddleware
- [ ] Testes para TenantService
- [ ] Testes para MessageService
- [ ] Testes para BaileysProvider (mocks)
- [ ] Configurar code coverage (>80%)

**Dependências:** Todas as anteriores da Fase 1

---

## Fase 2 - Features Core

### Objetivo

Adicionar provider Meta API, envio de múltiplos tipos de mensagens e sistema de webhooks.

### 2.1 Provider Meta WhatsApp Business API

**Prioridade: Alta**

- [ ] Configurar credenciais Meta Business
- [ ] Implementar `MetaApiProvider.cs`:
  - Método `InitializeAsync()`
  - Método `SendTextAsync()`
  - Métodos para outros tipos de mensagem
  - Método `DisconnectAsync()`
- [ ] Implementar webhook receiver para Meta
- [ ] Processar callbacks de status de mensagem
- [ ] Validação de assinaturas de webhook
- [ ] Testes de integração com Meta API

**Dependências:** Fase 1 completa

---

### 2.2 Chaveamento entre Providers

**Prioridade: Alta**

- [ ] Criar `ProviderFactory` para seleção dinâmica
- [ ] Implementar estratégia de fallback automático
- [ ] Endpoint POST /api/v1/tenant/provider-switch
- [ ] Configuração por tenant de provider preferencial
- [ ] Métricas de uso por provider
- [ ] Testes de chaveamento dinâmico

**Dependências:** Provider Meta API

---

### 2.3 Envio de Múltiplos Tipos de Mensagem

**Prioridade: Alta**

- [ ] Implementar endpoints específicos:
  - POST /api/v1/messages/media (imagens, vídeos, documentos)
  - POST /api/v1/messages/location
  - POST /api/v1/messages/audio
- [ ] Upload e armazenamento de mídia (Supabase Storage)
- [ ] Compressão e otimização de mídia
- [ ] Validação de formatos e tamanhos
- [ ] Implementar métodos nos providers (Baileys e Meta)
- [ ] Testes para cada tipo de mensagem

**Dependências:** Chaveamento entre Providers

---

### 2.4 Sistema de Webhooks

**Prioridade: Alta**

- [ ] Implementar `WebhookController`:
  - POST /api/v1/webhooks/incoming-message
  - POST /api/v1/webhooks/status-update
  - POST /api/v1/webhooks/supabase
- [ ] Validação de assinaturas de webhooks
- [ ] Processamento assíncrono de eventos
- [ ] Retry policy para falhas
- [ ] Logging estruturado de eventos
- [ ] Testes de webhooks

**Dependências:** Envio de Múltiplos Tipos

---

### 2.5 Integração Supabase Realtime

**Prioridade: Média**

- [ ] Implementar `SupabaseRealtimeService.cs`
- [ ] Subscrição a eventos de INSERT/UPDATE em messages
- [ ] Broadcast de status de mensagens em tempo real
- [ ] Gerenciamento de conexões realtime por tenant
- [ ] Tratamento de reconexão
- [ ] Testes de eventos realtime

**Dependências:** Sistema de Webhooks

---

### 2.6 Session Management Avançado

**Prioridade: Média**

- [ ] Implementar `SessionController` completo:
  - POST /api/v1/sessions/initialize
  - GET /api/v1/sessions/status
  - DELETE /api/v1/sessions/disconnect
- [ ] Gerenciamento de múltiplas sessões por tenant
- [ ] Monitoramento de saúde de sessões
- [ ] Reconexão automática
- [ ] Cache de sessões (Redis)
- [ ] Dashboard de status de sessões

**Dependências:** Integração Realtime

---

## Fase 3 - IA e Automação

### Objetivo

Integrar agentes de IA para automação de conversas e resposta inteligente.

### 3.1 Integração com Agentes de IA

**Prioridade: Alta**

- [ ] Definir arquitetura de integração (OpenAI, Azure OpenAI, etc)
- [ ] Implementar `IAIAgentService` interface
- [ ] Criar `AIAgentService.cs`:
  - Método `ProcessMessageAsync()`
  - Método `ConfigureAgentAsync()`
  - Método `GetAgentsAsync()`
- [ ] Integração com API de LLM
- [ ] Tratamento de rate limits e erros
- [ ] Custos e métricas de uso
- [ ] Testes com agentes reais

**Dependências:** Fase 2 completa

---

### 3.2 Sistema de Contexto de Conversação

**Prioridade: Alta**

- [ ] Implementar `ConversationContextService`
- [ ] Armazenamento de histórico de conversação
- [ ] Gerenciamento de janela de contexto
- [ ] Recuperação de contexto relevante
- [ ] Expiração e limpeza de contextos antigos
- [ ] Testes de continuidade de conversação

**Dependências:** Integração com Agentes de IA

---

### 3.3 Templates de Agentes Especializados

**Prioridade: Média**

- [ ] Criar template base de agente
- [ ] Implementar agente especializado em imóveis
- [ ] Implementar agente de suporte ao cliente
- [ ] Sistema de plugins/capabilities
- [ ] Configuração via JSON/YAML
- [ ] Documentação de criação de novos agentes
- [ ] Testes com diferentes tipos de agentes

**Dependências:** Sistema de Contexto

---

### 3.4 Endpoints de Gestão de Agentes

**Prioridade: Alta**

- [ ] Implementar controller de agentes:
  - POST /api/v1/agents/create
  - GET /api/v1/agents/list
  - PUT /api/v1/agents/{agentId}/configure
  - POST /api/v1/agents/{agentId}/assign-conversation
  - DELETE /api/v1/agents/{agentId}
- [ ] Validação de configurações
- [ ] Ativação/desativação de agentes
- [ ] Testes de CRUD de agentes

**Dependências:** Templates de Agentes

---

### 3.5 Dashboard de Métricas

**Prioridade: Média**

- [ ] Implementar endpoints de métricas:
  - GET /api/v1/metrics/messages
  - GET /api/v1/metrics/sessions
  - GET /api/v1/metrics/ai-usage
- [ ] Agregação de dados estatísticos
- [ ] Relatórios por tenant
- [ ] Export de métricas (CSV, JSON)
- [ ] Integração com ferramentas de observabilidade

**Dependências:** Endpoints de Gestão de Agentes

---

## Fase 4 - Produção

### Objetivo

Preparar o sistema para deploy em produção com testes completos, documentação e monitoramento.

### 4.1 Testes E2E Completos

**Prioridade: Alta**

- [ ] Setup de ambiente de testes E2E
- [ ] Implementar `WhatsAppTestFixture`
- [ ] Testes de fluxos completos:
  - Envio de mensagem com Baileys
  - Envio de mensagem com Meta API
  - Processamento com agente IA
  - Fallback entre providers
  - Recebimento de mensagens
  - Múltiplos tenants simultâneos
- [ ] Testes de carga e performance
- [ ] Relatórios de testes automatizados

**Dependências:** Fase 3 completa

---

### 4.2 Documentação da API

**Prioridade: Alta**

- [ ] Documentação Swagger/OpenAPI completa
- [ ] Guia de início rápido (Quick Start)
- [ ] Exemplos de uso para cada endpoint
- [ ] Postman collection
- [ ] Documentação de webhooks
- [ ] Guia de configuração de agentes IA
- [ ] Troubleshooting guide

**Dependências:** Nenhuma (pode ser paralelo)

---

### 4.3 CI/CD e Automação

**Prioridade: Alta**

- [ ] Pipeline de build automatizado
- [ ] Testes automáticos em PRs
- [ ] Deploy automático para staging
- [ ] Deploy manual para produção
- [ ] Rollback automático em falhas
- [ ] Versionamento semântico
- [ ] Release notes automatizadas

**Dependências:** Testes E2E

---

### 4.4 Monitoramento e Alertas

**Prioridade: Alta**

- [ ] Configurar Application Insights / Datadog / New Relic
- [ ] Implementar health checks:
  - GET /health
  - GET /health/ready
  - GET /health/live
- [ ] Alertas para:
  - Taxa de erro > 5%
  - Latência > 2s
  - Sessões desconectadas
  - Falhas de IA
- [ ] Dashboard de observabilidade
- [ ] Logging estruturado em produção

**Dependências:** CI/CD

---

### 4.5 Segurança e Hardening

**Prioridade: Alta**

- [ ] Implementar rate limiting por tenant
- [ ] Criptografia de dados sensíveis (session_data)
- [ ] Sanitização de inputs
- [ ] Auditoria de operações
- [ ] Scan de vulnerabilidades
- [ ] Configuração de CORS adequada
- [ ] HTTPS obrigatório
- [ ] Rotação de secrets

**Dependências:** Monitoramento

---

### 4.6 Deploy em Produção

**Prioridade: Alta**

- [ ] Provisionar infraestrutura (AWS/Azure/GCP)
- [ ] Configurar DNS e certificados SSL
- [ ] Deploy inicial em produção
- [ ] Smoke tests em produção
- [ ] Configurar backup do banco de dados
- [ ] Documentação de runbook operacional
- [ ] Treinamento da equipe de operações

**Dependências:** Todas as anteriores

---

## Cronograma Resumido

| Fase | Duração | Data Início | Data Fim |
|------|---------|-------------|----------|
| Fase 1 - MVP | 2 semanas | Semana 1 | Semana 2 |
| Fase 2 - Features Core | 2 semanas | Semana 3 | Semana 4 |
| Fase 3 - IA e Automação | 2 semanas | Semana 5 | Semana 6 |
| Fase 4 - Produção | 1 semana | Semana 7 | Semana 7 |

**Duração Total: 7 semanas**

---

## Recursos Necessários

### Equipe

- 2 Desenvolvedores Backend (.NET)
- 1 Desenvolvedor Full Stack (integração Node.js/Baileys)
- 1 DevOps Engineer
- 1 QA Engineer
- 1 Tech Lead

### Infraestrutura

- Supabase (PostgreSQL, Realtime, Storage)
- Redis (cache de sessões)
- Servidor para aplicação (.NET 9)
- Meta WhatsApp Business API (credenciais)
- Serviço de IA (OpenAI)
- Ferramenta de monitoramento (Datadog, New Relic, etc)

---

## Riscos e Mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|-------|---------------|---------|-----------|
| Dificuldade de integração Baileys em .NET | Média | Alto | Considerar wrapper Node.js ou biblioteca alternativa |
| Rate limits da Meta API | Alta | Médio | Implementar circuit breaker e fallback para Baileys |
| Custos de IA elevados | Média | Médio | Monitorar uso, implementar cache, limitar tokens |
| Complexidade de multi-tenancy | Baixa | Alto | Testes extensivos de isolamento |
| Escalabilidade de sessões | Média | Alto | Usar Redis para cache, horizontal scaling |

---

## Próximos Passos

1. **Aprovação do plano** pela equipe e stakeholders
2. **Setup do repositório** e estrutura inicial
3. **Kickoff da Fase 1** com toda a equipe
4. **Daily standups** para acompanhamento
5. **Reviews semanais** de progresso e ajustes

---

## Referências

- [PRD - whatsapp-microservice-prd.md](./whatsapp-microservice-prd.md)
- [Documentação Baileys](https://github.com/WhiskeySockets/Baileys)
- [Meta WhatsApp Business API](https://developers.facebook.com/docs/whatsapp)
- [Supabase Docs](https://supabase.com/docs)
- [.NET 9 Documentation](https://learn.microsoft.com/en-us/dotnet/)
