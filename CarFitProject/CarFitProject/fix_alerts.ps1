$files = Get-ChildItem -Recurse -Filter *.cshtml
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    
    # Remove multi-line @if block with {}
    $newContent = $content -replace '(?s)@if\s*\(\s*TempData\["(Success|Error)Message"\]\s*!=\s*null\s*\)\s*\{\s*<div[^>]*>.*?</div>\s*\}', ''
    
    # Remove single-line @if block without {}
    $newContent = $newContent -replace '(?s)@if\s*\(\s*TempData\["(Success|Error)Message"\]\s*!=\s*null\s*\)\s*<div[^>]*>.*?</div>', ''
    
    if ($content -cne $newContent) {
        Set-Content -Path $file.FullName -Value $newContent -NoNewline
        Write-Host "Updated $($file.Name)"
    }
}
