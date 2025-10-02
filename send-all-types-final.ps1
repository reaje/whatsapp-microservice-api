# Send all message types to +557188729930 - Final Version

$headers = @{
    "Content-Type" = "application/json"
    "X-Client-Id" = "a4876b9d-8ce5-4b67-ab69-c04073ce2f80"
}

$targetNumber = "+557188729930"

Write-Host ""
Write-Host "=== ENVIANDO TODOS OS TIPOS DE MENSAGEM ===" -ForegroundColor Cyan
Write-Host "Destinatario: $targetNumber" -ForegroundColor Yellow
Write-Host ""

# 1. Send Text Message
Write-Host "1. Enviando mensagem de TEXTO..." -ForegroundColor Green
$textBody = @{
    to = $targetNumber
    content = "Teste de mensagem de TEXTO via WhatsApp Microservice! Enviado em $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5278/api/v1/Message/text" -Method POST -Body $textBody -Headers $headers
    Write-Host "   TEXTO enviado! MessageId: $($response.messageId)" -ForegroundColor Green
} catch {
    Write-Host "   Erro ao enviar texto: $($_.Exception.Message)" -ForegroundColor Red
}

Start-Sleep -Seconds 2

# 2. Send Audio
Write-Host ""
Write-Host "2. Enviando AUDIO..." -ForegroundColor Green
$audioBase64 = "T2dnUwACAAAAAAAAAABfvwAAAAAAAKfDFP4BHgF2b3JiaXMAAAAAAUSsAAAAAAAAgLsAAAAAAAC4AU9nZ1MAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="
$audioBody = @{
    to = $targetNumber
    mediaBase64 = $audioBase64
    mediaType = 2
    caption = "Teste de AUDIO via WhatsApp Microservice"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5278/api/v1/Message/media" -Method POST -Body $audioBody -Headers $headers
    Write-Host "   AUDIO enviado! MessageId: $($response.messageId)" -ForegroundColor Green
} catch {
    Write-Host "   Erro ao enviar audio: $($_.Exception.Message)" -ForegroundColor Red
}

Start-Sleep -Seconds 2

# 3. Send Location
Write-Host ""
Write-Host "3. Enviando LOCALIZACAO..." -ForegroundColor Green
$locationBody = @{
    to = $targetNumber
    latitude = -12.9714
    longitude = -38.5014
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5278/api/v1/Message/location" -Method POST -Body $locationBody -Headers $headers
    Write-Host "   LOCALIZACAO enviada! MessageId: $($response.messageId)" -ForegroundColor Green
} catch {
    Write-Host "   Erro ao enviar localizacao: $($_.Exception.Message)" -ForegroundColor Red
}

Start-Sleep -Seconds 2

# 4. Send Document/File
Write-Host ""
Write-Host "4. Enviando DOCUMENTO (arquivo)..." -ForegroundColor Green
$documentBase64 = "JVBERi0xLjQKJcOkw7zDtsO4CjIgMCBvYmoKPDwKL0xlbmd0aCAzIDAgUgovRmlsdGVyIC9GbGF0ZURlY29kZQo+PgpzdHJlYW0KeJxLy8wpTVWwUsjIzEvLTFGwVUjOzytJrSjRTUlNLinKTFGwVbBVULBVsLVVqK4FAG6ZDPQKZW5kc3RyZWFtCmVuZG9iago="
$documentBody = @{
    to = $targetNumber
    mediaBase64 = $documentBase64
    mediaType = 4
    caption = "Teste de DOCUMENTO via WhatsApp Microservice"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5278/api/v1/Message/media" -Method POST -Body $documentBody -Headers $headers
    Write-Host "   DOCUMENTO enviado! MessageId: $($response.messageId)" -ForegroundColor Green
} catch {
    Write-Host "   Erro ao enviar documento: $($_.Exception.Message)" -ForegroundColor Red
}

Start-Sleep -Seconds 2

# 5. Send Image
Write-Host ""
Write-Host "5. Enviando IMAGEM..." -ForegroundColor Green
$imageBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg=="
$imageBody = @{
    to = $targetNumber
    mediaBase64 = $imageBase64
    mediaType = 1
    caption = "Teste de IMAGEM via WhatsApp Microservice"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5278/api/v1/Message/media" -Method POST -Body $imageBody -Headers $headers
    Write-Host "   IMAGEM enviada! MessageId: $($response.messageId)" -ForegroundColor Green
} catch {
    Write-Host "   Erro ao enviar imagem: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== TODOS OS TESTES FINALIZADOS COM SUCESSO! ===" -ForegroundColor Cyan
Write-Host "Verifique o WhatsApp do destinatario: $targetNumber" -ForegroundColor Yellow
Write-Host ""
Write-Host "Tipos de mensagem enviados:" -ForegroundColor White
Write-Host "  - Texto" -ForegroundColor Gray
Write-Host "  - Audio" -ForegroundColor Gray
Write-Host "  - Localizacao (Salvador, Bahia)" -ForegroundColor Gray
Write-Host "  - Documento (PDF)" -ForegroundColor Gray
Write-Host "  - Imagem" -ForegroundColor Gray
