$baseUrl = "https://localhost:7114/api"

# Disable SSL validation for local development
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

Write-Host "Logging in..." -ForegroundColor Cyan
$loginBody = @{
    email = "admin_verify@aitraining.com"
    password = "password123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/Auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.data.accessToken
    Write-Host "Login successful! Token acquired." -ForegroundColor Green
    
    Write-Host "`nSending request to OpenAI (via AITrainingSystem Backend)..." -ForegroundColor Cyan
    $headers = @{
        Authorization = "Bearer $token"
    }
    
    $askBody = @{
        lessonId = "00000000-0000-0000-0000-000000000000"
        question = "I want to learn Java. What is the very first thing I should understand about object oriented programming?"
    } | ConvertTo-Json
    
    $askResponse = Invoke-RestMethod -Uri "$baseUrl/AI/tutor/00000000-0000-0000-0000-000000000000" -Method Post -Body $askBody -ContentType "application/json" -Headers $headers
    
    Write-Host "`nResponse from AI:" -ForegroundColor Yellow
    Write-Host ($askResponse | ConvertTo-Json -Depth 5)
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}
