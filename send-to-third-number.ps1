# Test all message types to third number

$headers = @{
    "Content-Type" = "application/json"
    "X-Client-Id" = "a4876b9d-8ce5-4b67-ab69-c04073ce2f80"
}

$targetNumber = "+557188729930"

Write-Host ""
Write-Host "=== Testing WhatsApp Microservice - Third Number ===" -ForegroundColor Cyan
Write-Host "Target: $targetNumber" -ForegroundColor Yellow
Write-Host ""

# 1. Send Text Message
Write-Host "1. Sending text message..." -ForegroundColor Green
$textBody = @{
    to = $targetNumber
    content = "Teste de mensagem de texto via WhatsApp Microservice!"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5278/api/v1/Message/text" -Method POST -Body $textBody -Headers $headers
    Write-Host "   Text message sent! MessageId: $($response.messageId)" -ForegroundColor Green
} catch {
    Write-Host "   Error sending text: $($_.Exception.Message)" -ForegroundColor Red
}

Start-Sleep -Seconds 2

# 2. Send Image
Write-Host ""
Write-Host "2. Sending image..." -ForegroundColor Green
$imageBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg=="
$imageBody = @{
    to = $targetNumber
    mediaBase64 = $imageBase64
    mediaType = 1
    caption = "Teste de imagem via WhatsApp Microservice"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5278/api/v1/Message/media" -Method POST -Body $imageBody -Headers $headers
    Write-Host "   Image sent! MessageId: $($response.messageId)" -ForegroundColor Green
} catch {
    Write-Host "   Error sending image: $($_.Exception.Message)" -ForegroundColor Red
}

Start-Sleep -Seconds 2

# 3. Send Audio
Write-Host ""
Write-Host "3. Sending audio..." -ForegroundColor Green
$audioBase64 = "T2dnUwACAAAAAAAAAABfvwAAAAAAAKfDFP4BHgF2b3JiaXMAAAAAAUSsAAAAAAAAgLsAAAAAAAC4AU9nZ1MAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="
$audioBody = @{
    to = $targetNumber
    mediaBase64 = $audioBase64
    mediaType = 2
    caption = "Teste de audio via WhatsApp Microservice"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5278/api/v1/Message/media" -Method POST -Body $audioBody -Headers $headers
    Write-Host "   Audio sent! MessageId: $($response.messageId)" -ForegroundColor Green
} catch {
    Write-Host "   Error sending audio: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== All tests completed! ===" -ForegroundColor Cyan
