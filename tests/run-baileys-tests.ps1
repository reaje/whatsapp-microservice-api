#!/usr/bin/env pwsh
# Baileys Integration Tests Runner
# Usage: .\run-baileys-tests.ps1

$ErrorActionPreference = "Stop"

function Write-ColorOutput {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Test-BaileysService {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:3000/health" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        return $response.StatusCode -eq 200
    } catch {
        return $false
    }
}

# Banner
Write-ColorOutput "`n╔══════════════════════════════════════════╗" "Cyan"
Write-ColorOutput "║   Baileys Integration Tests Runner      ║" "Cyan"
Write-ColorOutput "║   Phone: +5571991776091                  ║" "Cyan"
Write-ColorOutput "╚══════════════════════════════════════════╝`n" "Cyan"

# Step 1: Check Baileys Service
Write-ColorOutput "Step 1: Checking Baileys Service..." "Yellow"
if (Test-BaileysService) {
    Write-ColorOutput "✅ Baileys service is running on http://localhost:3000" "Green"
} else {
    Write-ColorOutput "❌ Baileys service is NOT running" "Red"
    Write-ColorOutput "`nTo start the service, run:" "Yellow"
    Write-ColorOutput "  cd baileys-service" "White"
    Write-ColorOutput "  npm install" "White"
    Write-ColorOutput "  npm run dev`n" "White"
    exit 1
}

# Step 2: Build project
Write-ColorOutput "`nStep 2: Building project..." "Yellow"
Push-Location "$PSScriptRoot\.."
try {
    dotnet build --configuration Release | Out-Null
    Write-ColorOutput "✅ Build successful" "Green"
} catch {
    Write-ColorOutput "❌ Build failed" "Red"
    exit 1
} finally {
    Pop-Location
}

# Step 3: Ask which test to run
Write-ColorOutput "`nStep 3: Select test to run:" "Yellow"
Write-ColorOutput "  1. Initialize Session (scan QR code)" "White"
Write-ColorOutput "  2. Send Text Message" "White"
Write-ColorOutput "  3. Full Flow (Initialize + Send messages)" "White"
Write-ColorOutput "  4. Get Session Status" "White"
Write-ColorOutput "  5. Test Disconnection" "White"
Write-ColorOutput "  6. Run ALL tests" "White"
Write-ColorOutput "  0. Exit" "White"
Write-Host ""

$choice = Read-Host "Enter your choice (0-6)"

Push-Location "$PSScriptRoot"
try {
    switch ($choice) {
        "1" {
            Write-ColorOutput "`n🔄 Running: Initialize Session Test..." "Cyan"
            Write-ColorOutput "This will generate a QR code for scanning`n" "Yellow"
            dotnet test --filter "FullyQualifiedName~Should_Initialize_WhatsApp_Session_With_Real_Number" `
                --logger "console;verbosity=detailed" `
                --configuration Release
        }
        "2" {
            Write-ColorOutput "`n🔄 Running: Send Text Message Test..." "Cyan"
            Write-ColorOutput "Make sure your session is already connected!`n" "Yellow"
            dotnet test --filter "FullyQualifiedName~Should_Send_Text_Message_Via_Baileys" `
                --logger "console;verbosity=detailed" `
                --configuration Release
        }
        "3" {
            Write-ColorOutput "`n🔄 Running: Full Flow Test..." "Cyan"
            Write-ColorOutput "This will initialize session AND send messages`n" "Yellow"
            dotnet test --filter "FullyQualifiedName~Should_Complete_Full_Flow_Initialize_And_Send" `
                --logger "console;verbosity=detailed" `
                --configuration Release
        }
        "4" {
            Write-ColorOutput "`n🔄 Running: Get Session Status Test..." "Cyan"
            dotnet test --filter "FullyQualifiedName~Should_Get_Session_Status_From_Baileys_Service" `
                --logger "console;verbosity=detailed" `
                --configuration Release
        }
        "5" {
            Write-ColorOutput "`n🔄 Running: Test Disconnection..." "Cyan"
            dotnet test --filter "FullyQualifiedName~Should_Handle_Disconnection_Gracefully" `
                --logger "console;verbosity=detailed" `
                --configuration Release
        }
        "6" {
            Write-ColorOutput "`n🔄 Running: ALL Baileys Tests..." "Cyan"
            Write-ColorOutput "Note: Some tests may be skipped if prerequisites are not met`n" "Yellow"
            dotnet test --filter "FullyQualifiedName~BaileysIntegrationTests" `
                --logger "console;verbosity=detailed" `
                --configuration Release
        }
        "0" {
            Write-ColorOutput "`nExiting..." "Yellow"
            exit 0
        }
        default {
            Write-ColorOutput "`n❌ Invalid choice. Please run the script again." "Red"
            exit 1
        }
    }
} finally {
    Pop-Location
}

# Final message
Write-ColorOutput "`n╔══════════════════════════════════════════╗" "Cyan"
Write-ColorOutput "║   Test execution completed!              ║" "Cyan"
Write-ColorOutput "╚══════════════════════════════════════════╝`n" "Cyan"

Write-ColorOutput "📝 Notes:" "Yellow"
Write-ColorOutput "  • Tests marked with [Skip] won't run automatically" "White"
Write-ColorOutput "  • To enable a test, edit BaileysIntegrationTests.cs" "White"
Write-ColorOutput "  • Remove the Skip parameter from [Fact(Skip = '...')]" "White"
Write-ColorOutput "  • Session data is stored in: baileys-service/sessions/`n" "White"
