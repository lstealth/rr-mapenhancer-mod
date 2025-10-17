# FindDecompiledType.ps1
# Quick search utility for finding decompiled types

param(
    [Parameter(Mandatory=$true)]
    [string]$TypeName,
    
    [switch]$Open
)

$decompiledDir = ".\Decompiled\Assembly-CSharp"

if (-not (Test-Path $decompiledDir)) {
    Write-Host "Decompiled directory not found: $decompiledDir" -ForegroundColor Red
    Write-Host "Run decompilation first: ilspycmd -p -o ""$decompiledDir"" ""C:\games\Steam\steamapps\common\Railroader\Railroader_Data\Managed\Assembly-CSharp.dll""" -ForegroundColor Yellow
    exit 1
}

Write-Host "Searching for type: $TypeName" -ForegroundColor Cyan
Write-Host ""

# Search for files
$files = Get-ChildItem $decompiledDir -Recurse -Filter "*$TypeName*.cs"

if ($files.Count -eq 0) {
    Write-Host "No files found matching: *$TypeName*.cs" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Searching within file contents..." -ForegroundColor Cyan
    
    $contentMatches = Get-ChildItem $decompiledDir -Recurse -Filter "*.cs" | 
        Select-String "class\s+$TypeName\b|interface\s+$TypeName\b|struct\s+$TypeName\b|enum\s+$TypeName\b" -List
    
    if ($contentMatches) {
        Write-Host ""
        Write-Host "Found in:" -ForegroundColor Green
        foreach ($match in $contentMatches) {
            $relativePath = $match.Path.Replace($PWD.Path, ".")
            Write-Host "  $relativePath" -ForegroundColor White
            Write-Host "    Line $($match.LineNumber): $($match.Line.Trim())" -ForegroundColor Gray
            
            if ($Open) {
                code $match.Path
            }
        }
    } else {
        Write-Host "Type not found in any file" -ForegroundColor Red
    }
    
    exit
}

Write-Host "Found $($files.Count) file(s):" -ForegroundColor Green
Write-Host ""

foreach ($file in $files) {
    $relativePath = $file.FullName.Replace($PWD.Path, ".")
    $size = [math]::Round($file.Length / 1KB, 2)
    
    Write-Host "  📄 $($file.Name)" -ForegroundColor Cyan
    Write-Host "     Path: $relativePath" -ForegroundColor Gray
    Write-Host "     Size: $size KB" -ForegroundColor Gray
    
    # Show first few lines to identify the type
    $content = Get-Content $file.FullName -TotalCount 30
    $typeDeclaration = $content | Where-Object { $_ -match "(class|interface|struct|enum)\s+$TypeName" } | Select-Object -First 1
    
    if ($typeDeclaration) {
        Write-Host "     Type: $($typeDeclaration.Trim())" -ForegroundColor Green
    }
    
    Write-Host ""
    
    if ($Open) {
        Write-Host "  Opening in VS Code..." -ForegroundColor Yellow
        code $file.FullName
    }
}

if (-not $Open) {
    Write-Host "Tip: Use -Open flag to automatically open files in VS Code" -ForegroundColor Yellow
    Write-Host "Example: .\FindDecompiledType.ps1 -TypeName MapBuilder -Open" -ForegroundColor Gray
}
