<#
Usage:
  .\tools\get-managed-assembly-path.ps1
  .\tools\get-managed-assembly-path.ps1 -CopyTo .\build\managed

This script searches upward for `Directory.Build.props`, reads `RrInstallDir` and `RrManagedDir`, expands referenced properties, and prints the full path to `Assembly-CSharp.dll`.
If the `-CopyTo` parameter is provided the script will copy the assembly to the given folder.
#>
param(
    [string]$CopyTo = $null
)

function Find-DirectoryBuildProps {
    $dir = (Get-Location).Path
    while ($true) {
        $candidate = Join-Path $dir 'Directory.Build.props'
        if (Test-Path $candidate) { return (Get-Item $candidate).FullName }
        $parent = Split-Path $dir -Parent
        if ($parent -eq $dir -or [string]::IsNullOrEmpty($parent)) { break }
        $dir = $parent
    }
    return $null
}

$propsPath = Find-DirectoryBuildProps

if (-not $propsPath) {
    Write-Error "Directory.Build.props not found in current or parent directories. Run the script from the repo root or ensure Directory.Build.props exists."
    exit 2
}

try {
    [xml]$xml = Get-Content $propsPath -ErrorAction Stop
}
catch {
    Write-Error ("Failed to read XML from {0}: {1}" -f $propsPath, $_.ToString())
    exit 3
}

# Helper to read a property value by name (first match under any PropertyGroup)
function Get-PropertyValue([string]$name) {
    $node = $xml.SelectSingleNode("//PropertyGroup/*[local-name() = '$name']")
    if ($node -ne $null) { return $node.InnerText }
    return $null
}

$rrInstall = Get-PropertyValue 'RrInstallDir'
$rrManaged = Get-PropertyValue 'RrManagedDir'

if (-not $rrInstall) {
    Write-Error "RrInstallDir property not found in Directory.Build.props"
    exit 4
}
if (-not $rrManaged) {
    Write-Error "RrManagedDir property not found in Directory.Build.props"
    exit 5
}

# Expand any $(PropertyName) occurrences in RrManagedDir using values from the file.
$expanded = $rrManaged
$pattern = '\$\(([^)]+)\)'
while ($expanded -match $pattern) {
    $propName = $Matches[1]
    $val = Get-PropertyValue $propName
    if (-not $val) {
        Write-Error "Referenced property '$propName' not found while expanding RrManagedDir"
        exit 6
    }
    # Perform a literal replace of the matched token with the property value.
    $expanded = $expanded.Replace($Matches[0], $val)
}

# If expanded path is relative, make it absolute relative to Directory.Build.props folder
$propsDir = Split-Path $propsPath -Parent
if (-not ([System.IO.Path]::IsPathRooted($expanded))) {
    $expanded = Join-Path $propsDir $expanded
}

# Normalize path
$expanded = [System.IO.Path]::GetFullPath($expanded)

# Make sure path points to folder that contains Assembly-CSharp.dll
$assemblyName = 'Assembly-CSharp.dll'
$assemblyPath = if (Test-Path $expanded -PathType Leaf) { $expanded } elseif (Test-Path (Join-Path $expanded $assemblyName)) { Join-Path $expanded $assemblyName } else { Join-Path $expanded $assemblyName }

if (-not (Test-Path $assemblyPath)) {
    Write-Error "Assembly not found at: $assemblyPath"
    Write-Host "Expanded RrManagedDir: $expanded"
    exit 7
}

Write-Host $assemblyPath

if ($CopyTo) {
    try {
        $destDir = Resolve-Path -LiteralPath $CopyTo -ErrorAction SilentlyContinue
        if (-not $destDir) { New-Item -ItemType Directory -Path $CopyTo -Force | Out-Null; $destDir = Resolve-Path -LiteralPath $CopyTo }
        $dest = Join-Path $destDir.Path $assemblyName
        Copy-Item -Path $assemblyPath -Destination $dest -Force
        Write-Host "Copied assembly to: $dest"
    }
    catch {
        Write-Error ("Failed to copy assembly: {0}" -f $_.ToString())
        exit 8
    }
}

exit 0
