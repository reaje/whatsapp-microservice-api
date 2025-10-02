# Initialize WhatsApp session

$headers = @{
    "Content-Type" = "application/json"
    "X-Client-Id" = "a4876b9d-8ce5-4b67-ab69-c04073ce2f80"
}

# Use um número de telefone válido para inicializar a sessão
$phoneNumber = "+5571991776091"

$body = @{
    phoneNumber = $phoneNumber
    providerType = 0  # 0 = Baileys
} | ConvertTo-Json

Write-Host ""
Write-Host "=== Inicializando Sessão WhatsApp ===" -ForegroundColor Cyan
Write-Host "Número: $phoneNumber" -ForegroundColor Yellow
Write-Host "Provider: Baileys" -ForegroundColor Yellow
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5278/api/v1/Session/initialize" -Method POST -Body $body -Headers $headers
    
    Write-Host "✅ Sessão inicializada!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Status da sessão:" -ForegroundColor Yellow
    Write-Host "   Conectado: $($response.isConnected)" -ForegroundColor White
    Write-Host "   Status: $($response.status)" -ForegroundColor White
    Write-Host "   Número: $($response.phoneNumber)" -ForegroundColor White
    
    if ($response.metadata.qrCode) {
        Write-Host ""
        Write-Host "🔲 QR CODE PARA ESCANEAR:" -ForegroundColor Yellow
        Write-Host "   $($response.metadata.qrCode)" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "📱 Abra o WhatsApp no seu celular e escaneie o QR code acima" -ForegroundColor Green
        Write-Host "   1. Vá em WhatsApp > Configurações > Dispositivos conectados" -ForegroundColor Gray
        Write-Host "   2. Toque em 'Conectar um dispositivo'" -ForegroundColor Gray
        Write-Host "   3. Escaneie o QR code" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "Resposta completa:" -ForegroundColor Gray
    $response | ConvertTo-Json -Depth 3 | Write-Host
    
} catch {
    Write-Host "❌ Erro ao inicializar sessão:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host ""
            Write-Host "Detalhes do erro:" -ForegroundColor Yellow
            Write-Host $responseBody -ForegroundColor Yellow
        } catch {
            Write-Host "Não foi possível ler os detalhes do erro." -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "=== Inicialização finalizada ===" -ForegroundColor Cyan
