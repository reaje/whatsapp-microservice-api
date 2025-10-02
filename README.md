# WhatsApp Microservice - Multi-Tenant

Microserviço robusto e escalável para comunicação via WhatsApp, integrando Baileys (WhatsApp Web) e Meta WhatsApp Business API, com suporte multi-tenant e integração com agentes de IA.

## 🚀 Tecnologias

- **.NET 9** - Framework principal
- **C#** - Linguagem de programação
- **Supabase** - PostgreSQL, Realtime, Storage
- **Redis** - Cache de sessões
- **Docker** - Containerização
- **Nginx** - Proxy reverso e load balancer

## 📋 Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (versão 9.0.304 ou superior)
- [Docker](https://www.docker.com/) e Docker Compose
- [PostgreSQL](https://www.postgresql.org/) (via Supabase)
- [Redis](https://redis.io/)

## 🏗️ Arquitetura

O projeto segue os princípios de **Clean Architecture** e está organizado em camadas:

```
backend/
├── src/
│   ├── WhatsApp.API/          # API Web (Controllers, Middleware)
│   ├── WhatsApp.Core/         # Entidades, Interfaces, Enums
│   └── WhatsApp.Infrastructure/ # Providers, Services, Data Access
├── tests/
│   └── WhatsApp.Tests/        # Testes (Unit, Integration, E2E)
└── docs/                      # Documentação
```

### Camadas

- **WhatsApp.API**: Camada de apresentação (Controllers, Middleware, DTOs)
- **WhatsApp.Core**: Camada de domínio (Entities, Interfaces, Business Logic)
- **WhatsApp.Infrastructure**: Camada de infraestrutura (Providers, Services, Data Access)
- **WhatsApp.Tests**: Camada de testes

## 🔧 Configuração

### 1. Clone o repositório

```bash
git clone <repository-url>
cd whatsapp-microservice/backend
```

### 2. Configure as variáveis de ambiente

Copie o arquivo `.env.example` para `.env` e preencha com suas credenciais:

```bash
cp .env.example .env
```

Edite o arquivo `.env` com suas configurações:

```env
SUPABASE_CONNECTION_STRING=Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;...
SUPABASE_URL=https://your-project.supabase.co
SUPABASE_ANON_KEY=your-anon-key
SUPABASE_SERVICE_KEY=your-service-key
```

### 3. Configure o banco de dados

Execute o SQL do schema localizado em `docs/whatsapp-microservice-prd.md` no seu banco Supabase.

### 4. Restaure as dependências

```bash
dotnet restore
```

## 🚀 Como executar

### Desenvolvimento (Local)

```bash
# Executar a API diretamente
cd src/WhatsApp.API
dotnet run

# Ou executar toda a solução
dotnet run --project src/WhatsApp.API/WhatsApp.API.csproj
```

A API estará disponível em: `http://localhost:5000`

### Docker Compose

```bash
# Build e iniciar todos os serviços
docker-compose up --build

# Ou em modo detached
docker-compose up -d

# Parar os serviços
docker-compose down
```

Serviços disponíveis:
- **API**: http://localhost:5000
- **Redis**: localhost:6379
- **Nginx**: http://localhost:80

## 🧪 Executar testes

```bash
# Executar todos os testes
dotnet test

# Executar testes com cobertura
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Executar testes por categoria
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration
dotnet test --filter Category=E2E
```

## 📚 Documentação da API

Após iniciar a aplicação, acesse a documentação Swagger em:

- **Swagger UI**: http://localhost:5000/swagger
- **OpenAPI JSON**: http://localhost:5000/swagger/v1/swagger.json

## 🔑 Autenticação

Todas as requisições devem incluir os seguintes headers:

```http
X-Client-Id: your-tenant-id
Authorization: Bearer your-jwt-token
```

## 📁 Estrutura de Endpoints

### Sessions
- `POST /api/v1/sessions/initialize` - Inicializar sessão WhatsApp
- `GET /api/v1/sessions/status` - Status da sessão
- `DELETE /api/v1/sessions/disconnect` - Desconectar sessão

### Messages
- `POST /api/v1/messages/text` - Enviar mensagem de texto
- `POST /api/v1/messages/media` - Enviar mídia
- `POST /api/v1/messages/location` - Enviar localização
- `POST /api/v1/messages/audio` - Enviar áudio
- `GET /api/v1/messages/{messageId}/status` - Status da mensagem

### Agents (IA)
- `POST /api/v1/agents/create` - Criar agente IA
- `GET /api/v1/agents/list` - Listar agentes
- `PUT /api/v1/agents/{agentId}/configure` - Configurar agente
- `DELETE /api/v1/agents/{agentId}` - Remover agente

### Webhooks
- `POST /api/v1/webhooks/incoming-message` - Webhook para mensagens recebidas
- `POST /api/v1/webhooks/status-update` - Webhook para atualizações de status

## 🏥 Health Checks

- `GET /health` - Health check geral
- `GET /health/ready` - Readiness check
- `GET /health/live` - Liveness check

## 🛠️ Ferramentas de Desenvolvimento

### Code Analyzers

O projeto inclui analyzers configurados no `.editorconfig` para garantir qualidade de código:

- Naming conventions
- Code style rules
- Best practices enforcement

### Debugging

Para debug no VS Code, use o arquivo `.vscode/launch.json` (criar se necessário):

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/WhatsApp.API/bin/Debug/net9.0/WhatsApp.API.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/WhatsApp.API",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

## 📊 Monitoramento

O projeto está preparado para integração com ferramentas de observabilidade:

- **Logging**: Serilog (structured logging)
- **Metrics**: Application Insights / Prometheus
- **Tracing**: OpenTelemetry

## 🔒 Segurança

- Rate limiting por tenant
- Validação de webhook signatures
- Criptografia de dados sensíveis
- Isolamento multi-tenant
- HTTPS obrigatório em produção

## 🚢 Deploy

### Docker Production

```bash
# Build para produção
docker build -t whatsapp-microservice:latest .

# Run com variáveis de ambiente
docker run -d \
  -p 5000:5000 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__RulesEngineDatabase="..." \
  whatsapp-microservice:latest
```

### Kubernetes

Manifests de exemplo estarão disponíveis em `k8s/` (futuro).

## 📖 Documentação Adicional

- [PRD Completo](docs/whatsapp-microservice-prd.md)
- [Plano de Implementação](docs/plan.md)

## 🤝 Contribuindo

1. Crie um branch para sua feature (`git checkout -b feature/AmazingFeature`)
2. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
3. Push para o branch (`git push origin feature/AmazingFeature`)
4. Abra um Pull Request

## 📄 Licença

Este projeto é proprietário da Ventry.

## 👥 Equipe

- Backend Developers
- DevOps Engineers
- QA Engineers

## 📞 Suporte

Para questões e suporte, entre em contato com a equipe de desenvolvimento.

---

**Status do Projeto**: 🟡 Em Desenvolvimento (Fase 1 - MVP)