$files = Get-ChildItem -Recurse -Filter *.cshtml
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $newContent = $content -replace 'onsubmit="return confirm\(''([^'']+?)''\);?"', 'data-confirm="$1"'
    if ($content -cne $newContent) {
        Set-Content -Path $file.FullName -Value $newContent -NoNewline
        Write-Host "Updated $($file.Name)"
    }
}
