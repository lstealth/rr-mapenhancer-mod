# DecompileTargetedTypes.ps1
# This script uses a REST API to decompile specific types from the loaded assembly

param(
    [string]$OutputDir = ".\Decompiled\Targeted"
)

$OutputDir = Resolve-Path $OutputDir -ErrorAction SilentlyContinue
if (-not $OutputDir) {
    $OutputDir = (New-Item -ItemType Directory -Path ".\Decompiled\Targeted" -Force).FullName
}

Write-Host "Output Directory: $OutputDir" -ForegroundColor Cyan

# API base URL (assuming local decompiler service is running)
$apiBase = "http://localhost:5000/api/decompiler"

# Function to search for types
function Search-Type {
    param([string]$typeName)
    
    $uri = "$apiBase/search/types?query=$typeName&limit=10"
    try {
        $response = Invoke-RestMethod -Uri $uri -Method Get
        return $response.data.items
    } catch {
        Write-Host "  [ERROR] Failed to search for type: $typeName" -ForegroundColor Red
        return $null
    }
}

# Function to get decompiled source
function Get-DecompiledSource {
    param([string]$memberId)
    
    $uri = "$apiBase/source/$memberId"
    try {
        $response = Invoke-RestMethod -Uri $uri -Method Get
        return $response.data
    } catch {
        Write-Host "  [ERROR] Failed to decompile member: $memberId" -ForegroundColor Red
        return $null
    }
}

# Function to save source to file
function Save-SourceToFile {
    param(
        [string]$namespace,
        [string]$typeName,
        [array]$lines
    )
    
    $namespaceDir = Join-Path $OutputDir $namespace.Replace(".", "\")
    New-Item -ItemType Directory -Path $namespaceDir -Force | Out-Null
    
    $filePath = Join-Path $namespaceDir "$typeName.cs"
    $content = $lines -join "`n"
    Set-Content -Path $filePath -Value $content -Encoding UTF8
    
    Write-Host "    [SAVED] $filePath" -ForegroundColor Green
}

# Key types referenced in MapEnhancer
$typesToDecompile = @{
    "UI.Map" = @("MapBuilder", "MapWindow", "MapIcon", "MapLabel", "MapDrag", "MapSwitchStand")
    "Track" = @("TrackSegment", "TrackNode", "TrackObjectManager", "Graph")
    "Track.Signals" = @("CTCBlock", "CTCSignal", "SignalAspect")
    "RollingStock" = @("Car", "BaseLocomotive", "CarArchetype", "TrainController")
    "Game" = @("StateManager", "GameStorage", "MapManager", "FlareManager", "FlarePickable")
    "Model" = @("Location", "IndustryComponent", "PassengerStop", "ProgressionIndustryComponent")
    "Model.Ops" = @("OpsController", "Area", "OpsCarPosition")
    "Helpers" = @("WorldTransformer", "GameInput", "CameraSelector")
    "Character" = @("SpawnPoint")
    "UI.Builder" = @("UIPanelBuilder", "UIPanel", "ProgrammaticWindowCreator")
    "UI.CarInspector" = @("CarInspector", "CarPickable")
    "UI.Console.Commands" = @("TeleportCommand")
    "Game.Messages" = @("FlareAddUpdate")
    "Game.Events" = @("MapDidLoadEvent", "MapWillUnloadEvent", "WorldDidMoveEvent", "SwitchThrownDidChange", "CanvasScaleChanged")
}

Write-Host "`nDecompiling targeted types..." -ForegroundColor Yellow

foreach ($ns in $typesToDecompile.Keys) {
    Write-Host "`nNamespace: $ns" -ForegroundColor Cyan
    
    foreach ($typeName in $typesToDecompile[$ns]) {
        Write-Host "  Searching for type: $typeName" -ForegroundColor White
        
        $types = Search-Type -typeName $typeName
        
        if ($types -and $types.Count -gt 0) {
            # Find exact match or best match
            $type = $types | Where-Object { $_.name -eq $typeName -and $_.namespace -eq $ns } | Select-Object -First 1
            
            if (-not $type) {
                $type = $types | Where-Object { $_.name -eq $typeName } | Select-Object -First 1
            }
            
            if ($type) {
                Write-Host "    Found: $($type.fullName)" -ForegroundColor Gray
                
                $source = Get-DecompiledSource -memberId $type.memberId
                
                if ($source) {
                    Save-SourceToFile -namespace $type.namespace -typeName $type.name -lines $source.lines
                }
            } else {
                Write-Host "    [NOT FOUND] No exact match for $typeName in namespace $ns" -ForegroundColor Yellow
            }
        } else {
            Write-Host "    [NOT FOUND] $typeName" -ForegroundColor Yellow
        }
    }
}

Write-Host "`nDecompilation complete!" -ForegroundColor Green
Write-Host "Decompiled code is located in: $OutputDir" -ForegroundColor Cyan
