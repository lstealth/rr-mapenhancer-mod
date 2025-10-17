# DecompileAll.ps1
# This script decompiles all the DLLs referenced in the MapEnhancer project

param(
    [string]$GameDir = "C:\games\Steam\steamapps\common\Railroader",
    [string]$OutputDir = ".\Decompiled"
)

$ManagedDir = Join-Path $GameDir "Railroader_Data\Managed"
$OutputDir = Resolve-Path $OutputDir -ErrorAction SilentlyContinue
if (-not $OutputDir) {
    $OutputDir = (New-Item -ItemType Directory -Path ".\Decompiled" -Force).FullName
}

Write-Host "Managed Directory: $ManagedDir" -ForegroundColor Cyan
Write-Host "Output Directory: $OutputDir" -ForegroundColor Cyan

# List of DLLs to decompile (from .csproj file)
$dllsToDecompile = @(
    "Assembly-CSharp.dll",
    "Core.dll",
    "Definition.dll",
    "Map.Runtime.dll",
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.IMGUIModule.dll",
    "UnityEngine.InputModule.dll",
    "UnityEngine.PhysicsModule.dll",
    "UnityEngine.UI.dll",
    "UnityEngine.UIModule.dll",
    "UnityEngine.ImageConversionModule.dll",
    "UnityEngine.InputLegacyModule.dll",
    "Unity.InputSystem.dll",
    "Unity.RenderPipelines.Universal.Runtime.dll",
    "Unity.TextMeshPro.dll"
)

# Key types to decompile from Assembly-CSharp.dll
$typesToDecompile = @(
    # UI.Map namespace
    "MapBuilder", "MapWindow", "MapIcon", "MapLabel", "MapDrag", "MapSwitchStand",
    
    # Track namespace
    "TrackSegment", "TrackNode", "TrackObjectManager", "Graph",
    
    # Train/Car related
    "TrainController", "Car", "BaseLocomotive", "CarArchetype",
    
    # CTC/Signal related
    "CTCBlock", "CTCSignal", "SignalAspect",
    
    # Game related
    "StateManager", "GameStorage", "MapManager", "FlareManager", "FlarePickable",
    
    # Industry/Passenger
    "IndustryComponent", "PassengerStop", "ProgressionIndustryComponent",
    
    # Helpers
    "WorldTransformer", "GameInput", "CameraSelector",
    
    # Ops
    "OpsController", "Area", "OpsCarPosition",
    
    # Character
    "SpawnPoint",
    
    # UI.Builder
    "UIPanelBuilder", "UIPanel", "ProgrammaticWindowCreator",
    
    # UI.CarInspector
    "CarInspector", "CarPickable",
    
    # UI.Console.Commands
    "TeleportCommand",
    
    # Model
    "Location",
    
    # Game.Messages
    "FlareAddUpdate",
    
    # Game.Events
    "MapDidLoadEvent", "MapWillUnloadEvent", "WorldDidMoveEvent", "SwitchThrownDidChange", "CanvasScaleChanged"
)

Write-Host "`nStep 1: Checking DLL existence..." -ForegroundColor Yellow
foreach ($dll in $dllsToDecompile) {
    $dllPath = Join-Path $ManagedDir $dll
    if (Test-Path $dllPath) {
        Write-Host "  [OK] $dll" -ForegroundColor Green
    } else {
        Write-Host "  [MISSING] $dll" -ForegroundColor Red
    }
}

Write-Host "`nStep 2: Installing ILSpy (if not already installed)..." -ForegroundColor Yellow
if (-not (Get-Command ilspycmd -ErrorAction SilentlyContinue)) {
    Write-Host "  Installing ILSpy command line tool..." -ForegroundColor Cyan
    dotnet tool install -g ilspycmd
}

Write-Host "`nStep 3: Decompiling DLLs..." -ForegroundColor Yellow
foreach ($dll in $dllsToDecompile) {
    $dllPath = Join-Path $ManagedDir $dll
    if (Test-Path $dllPath) {
        $dllName = [System.IO.Path]::GetFileNameWithoutExtension($dll)
        $outputPath = Join-Path $OutputDir $dllName
        
        Write-Host "  Decompiling $dll..." -ForegroundColor Cyan
        
        # Create output directory
        New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
        
        # Decompile using ilspycmd
        try {
            ilspycmd -p -o $outputPath $dllPath 2>&1 | Out-Null
            Write-Host "    [DONE] $dll -> $outputPath" -ForegroundColor Green
        } catch {
            Write-Host "    [ERROR] Failed to decompile $dll : $_" -ForegroundColor Red
        }
    }
}

Write-Host "`nDecompilation complete!" -ForegroundColor Green
Write-Host "Decompiled code is located in: $OutputDir" -ForegroundColor Cyan
