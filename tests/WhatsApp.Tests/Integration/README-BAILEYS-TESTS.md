# Baileys Integration Tests

Testes de integração reais com o serviço Baileys para o número **+5571991776091**.

## Pré-requisitos

1. **Baileys Service rodando**
   ```bash
   cd baileys-service
   npm install
   npm run dev
   ```

2. **WhatsApp instalado no celular** com o número +5571991776091

3. **.NET 9 SDK** instalado

## Estrutura dos Testes

### Testes Disponíveis

1. **`Should_Initialize_WhatsApp_Session_With_Real_Number`**
   - Inicializa sessão WhatsApp
   - Gera QR Code para escaneamento
   - Valida conexão estabelecida

2. **`Should_Send_Text_Message_Via_Baileys`**
   - Envia mensagem de texto real
   - Valida envio com sucesso
   - Retorna messageId

3. **`Should_Complete_Full_Flow_Initialize_And_Send`**
   - Fluxo completo: inicializa + envia mensagens
   - Testa texto + localização
   - Validação end-to-end

4. **`Should_Get_Session_Status_From_Baileys_Service`**
   - Verifica status da sessão
   - Valida estados: qr_required, connecting, connected, disconnected

5. **`Should_Handle_Disconnection_Gracefully`**
   - Testa desconexão controlada
   - Valida limpeza de recursos

## Como Executar

### 1. Iniciar Baileys Service

```bash
# Terminal 1
cd baileys-service
npm run dev
```

Aguarde até ver:
```
🚀 Baileys WhatsApp Service running on port 3000
```

### 2. Executar Teste de Inicialização

```bash
# Terminal 2
cd backend
dotnet test --filter "FullyQualifiedName~Should_Initialize_WhatsApp_Session_With_Real_Number" --logger "console;verbosity=detailed"
```

**Output esperado:**
```
=== QR CODE FOR SCANNING ===
2@abcdef1234567890...
============================
Scan this QR code with WhatsApp to continue the test
```

### 3. Escanear QR Code

1. Abra o WhatsApp no celular (+5571991776091)
2. Vá em: **Configurações → Aparelhos Conectados → Conectar um Aparelho**
3. Escaneie o QR Code exibido no terminal
4. Aguarde a mensagem "Conectado com sucesso"

### 4. Executar Teste de Envio de Mensagem

```bash
dotnet test --filter "FullyQualifiedName~Should_Send_Text_Message_Via_Baileys" --logger "console;verbosity=detailed"
```

**Output esperado:**
```
✅ Message sent successfully! Message ID: 3EB0XXXXX
```

### 5. Executar Fluxo Completo

```bash
dotnet test --filter "FullyQualifiedName~Should_Complete_Full_Flow_Initialize_And_Send" --logger "console;verbosity=detailed"
```

**Output esperado:**
```
Step 1: Initializing session...
Session status: connected

Step 2: Checking session status...
✅ Session is connected: connected

Step 3: Sending text message...
✅ Text message sent! ID: 3EB0XXXXX

Step 4: Sending location...
✅ Location sent! ID: 3EB0YYYYY

✅ Full flow completed successfully!
- Session initialized: connected
- Text message sent: 3EB0XXXXX
- Location sent: 3EB0YYYYY
```

### 6. Executar Todos os Testes Baileys

```bash
dotnet test --filter "FullyQualifiedName~BaileysIntegrationTests" --logger "console;verbosity=detailed"
```

## Remover Skip dos Testes

Os testes estão marcados com `Skip` por padrão para não rodarem automaticamente.

Para habilitar um teste, edite o arquivo e remova o parâmetro `Skip`:

**Antes:**
```csharp
[Fact(Skip = "Requires baileys-service running and active WhatsApp session")]
public async Task Should_Send_Text_Message_Via_Baileys()
```

**Depois:**
```csharp
[Fact]
public async Task Should_Send_Text_Message_Via_Baileys()
```

## Scripts Úteis

### Script PowerShell: `run-baileys-tests.ps1`

```powershell
# Salve este arquivo em: backend/tests/run-baileys-tests.ps1

Write-Host "=== Baileys Integration Tests ===" -ForegroundColor Cyan

# 1. Check if baileys-service is running
Write-Host "`n1. Checking baileys-service..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000/health" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Baileys service is running" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Baileys service is NOT running" -ForegroundColor Red
    Write-Host "Start it with: cd baileys-service && npm run dev" -ForegroundColor Yellow
    exit 1
}

# 2. Run initialization test
Write-Host "`n2. Running initialization test..." -ForegroundColor Yellow
dotnet test --filter "FullyQualifiedName~Should_Initialize_WhatsApp_Session_With_Real_Number" --logger "console;verbosity=detailed"

# 3. Ask user if session is connected
Write-Host "`n3. Is the WhatsApp session connected? (y/n)" -ForegroundColor Yellow
$connected = Read-Host
if ($connected -ne "y") {
    Write-Host "Please scan QR code and run this script again" -ForegroundColor Yellow
    exit 0
}

# 4. Run send message test
Write-Host "`n4. Running send message test..." -ForegroundColor Yellow
dotnet test --filter "FullyQualifiedName~Should_Send_Text_Message_Via_Baileys" --logger "console;verbosity=detailed"

# 5. Run full flow test
Write-Host "`n5. Running full flow test..." -ForegroundColor Yellow
dotnet test --filter "FullyQualifiedName~Should_Complete_Full_Flow_Initialize_And_Send" --logger "console;verbosity=detailed"

Write-Host "`n✅ All Baileys tests completed!" -ForegroundColor Green
```

### Executar Script PowerShell

```powershell
cd backend/tests
.\run-baileys-tests.ps1
```

## Verificar Sessão Persistida

A sessão ficará salva em `baileys-service/sessions/session-{tenant-id}-5571991776091/`

```bash
# Ver arquivos da sessão
ls baileys-service/sessions/

# Verificar sessão específica
ls baileys-service/sessions/session-*-5571991776091/
```

Arquivos esperados:
- `creds.json` - Credenciais da sessão
- `app-state-sync-*.json` - Estado sincronizado do WhatsApp

## Troubleshooting

### Erro: "Cannot access Baileys service"

**Causa:** Baileys service não está rodando

**Solução:**
```bash
cd baileys-service
npm run dev
```

### Erro: "Session not connected"

**Causa:** QR Code não foi escaneado ou sessão expirou

**Solução:**
1. Execute o teste de inicialização novamente
2. Escaneie o novo QR Code
3. Execute os outros testes

### Erro: "Failed to send message"

**Causa:** Sessão desconectada ou número inválido

**Solução:**
1. Verifique status da sessão: `GET http://localhost:3000/api/sessions/session-{id}/status`
2. Reconecte se necessário
3. Valide formato do número: `+5571991776091`

### Mensagem não chegou no WhatsApp

**Possíveis causas:**
1. Número bloqueado
2. WhatsApp temporariamente banido
3. Sessão desconectada após envio
4. Problema na rede

**Solução:**
1. Verifique logs do baileys-service: `docker-compose logs -f baileys-service`
2. Tente enviar novamente
3. Verifique se o WhatsApp no celular está funcionando

## Limpar Sessões de Teste

```bash
# Limpar todas as sessões
rm -rf baileys-service/sessions/session-*

# Ou via Docker
docker-compose exec baileys-service rm -rf /app/sessions/session-*
```

## Referências

- [Baileys Documentation](https://github.com/WhiskeySockets/Baileys)
- [WhatsApp Web Protocol](https://github.com/sigalor/whatsapp-web-reveng)
- [xUnit Documentation](https://xunit.net/)
