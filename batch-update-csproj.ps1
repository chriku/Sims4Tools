# Update all remaining .csproj files to .NET Framework 4.8
$count = 0
Get-ChildItem -Recurse -Filter "*.csproj" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    if ($content -match '<TargetFrameworkVersion>v4\.0</TargetFrameworkVersion>') {
        $newContent = $content -replace '<TargetFrameworkVersion>v4\.0</TargetFrameworkVersion>', '<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>'
        Set-Content -Path $_.FullName -Value $newContent -NoNewline
        Write-Host "Updated: $($_.Name)"
        $count++
    }
}
Write-Host "Total files updated: $count"
