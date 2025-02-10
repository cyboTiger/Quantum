# Windows publish script for Quantum
$ErrorActionPreference = "Stop"

# Create Release directory if it doesn't exist
New-Item -ItemType Directory -Force -Path "./Release/win"

# Change to Quantum.Shell directory
Push-Location ".\Quantum.Shell"

Write-Host "Publishing for Windows..."
electronize build /target win /PublishSingleFile false
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Error "Windows publish failed"
    exit 1
}

# Move published content to Release/win with force overwrite
Write-Host "Moving Windows build to Release directory..."
if (Test-Path ".\Release\*") {
    # Clean up target directory first
    if (Test-Path "..\Release\win") {
        Get-ChildItem -Path "..\Release\win" -Recurse | Remove-Item -Force -Recurse
    }
    # Then move the files
    Move-Item -Path ".\Release\*" -Destination "..\Release\win\" -Force
} else {
    Pop-Location
    Write-Error "Windows build output not found"
    exit 1
}

# Copy Quantum.Infrastructure.dll for Windows
$winInfraPath = "../Release/win/win-unpacked/resources/bin/Quantum.Infrastructure.dll"
if (Test-Path $winInfraPath) {
    Copy-Item $winInfraPath -Destination "../Release/win/" -Force
} else {
    Pop-Location
    Write-Error "Could not find Quantum.Infrastructure.dll for Windows"
    exit 1
}

# Restore original directory
Pop-Location

Write-Host "Publishing completed successfully!"
pause
