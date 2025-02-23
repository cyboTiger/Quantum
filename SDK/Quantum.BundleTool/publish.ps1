if (Test-Path "bin") {
    Remove-Item -Path "bin" -Recurse -Force
}

$platforms = @("win-x64", "osx-x64", "linux-x64")

foreach ($rid in $platforms) {
    Write-Host "Publishing for $rid..."
    $outputPath = "bin\$rid"
    dotnet publish -c Release -r $rid --self-contained true -o $outputPath
}

Write-Host "Publish completed!"
