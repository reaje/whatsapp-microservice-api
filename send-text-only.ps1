$headers = @{
    "Content-Type" = "application/json"
    "X-Client-Id" = "a4876b9d-8ce5-4b67-ab69-c04073ce2f80"
}

$body = @{
    to = "+557188729930"
    content = "Teste de mensagem de texto via WhatsApp Microservice para +557188729930!"
} | ConvertTo-Json

Write-Host "Sending text message to +557188729930..."

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5278/api/v1/Message/text" -Method POST -Body $body -Headers $headers
    Write-Host "Success! MessageId: $($response.messageId)" -ForegroundColor Green
    $response | ConvertTo-Json
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody"
    }
}
