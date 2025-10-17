# Decompiled Assembly Reference

This directory contains decompiled C# source code from the Railroader game assemblies.

## Assembly Information

- **Game Directory**: `C:\games\Steam\steamapps\common\Railroader`
- **Managed DLLs**: `Railroader_Data\Managed\`
- **Primary Assembly**: `Assembly-CSharp.dll` (MVID: 46622132c30f44cd9d6673b5a0a88f47)
- **Assembly Stats**:
  - 2,485 Types
  - 11,897 Methods
  - 113 Namespaces

## Referenced Assemblies

### Game Assemblies
- **Assembly-CSharp.dll** - Main game logic
- **Core.dll** - Core game systems
- **Definition.dll** - Game definitions and data
- **Map.Runtime.dll** - Map runtime systems

### Unity Engine
- **UnityEngine.dll** - Main Unity engine
- **UnityEngine.CoreModule.dll** - Core Unity functionality
- **UnityEngine.IMGUIModule.dll** - Immediate mode GUI
- **UnityEngine.InputModule.dll** - Input system
- **UnityEngine.PhysicsModule.dll** - Physics engine
- **UnityEngine.UI.dll** - Unity UI system
- **UnityEngine.UIModule.dll** - UI module
- **UnityEngine.ImageConversionModule.dll** - Image conversion
- **UnityEngine.InputLegacyModule.dll** - Legacy input
- **Unity.InputSystem.dll** - New input system
- **Unity.RenderPipelines.Universal.Runtime.dll** - URP rendering
- **Unity.TextMeshPro.dll** - TextMeshPro UI

## Key Types Used in MapEnhancer

### UI.Map Namespace
- **MapBuilder** - Main map building and rendering logic
- **MapWindow** - Map window UI controller
- **MapIcon** - Icon display on map
- **MapLabel** - Label display on map
- **MapDrag** - Map dragging interaction
- **MapSwitchStand** - Switch stand visualization

### Track Namespace
- **TrackSegment** - Individual track segments
- **TrackNode** - Track junction nodes
- **TrackObjectManager** - Manages track objects
- **Graph** - Track graph/network

### Track.Signals Namespace
- **CTCBlock** - CTC (Centralized Traffic Control) blocks
- **CTCSignal** - Signal objects
- **SignalAspect** - Signal state/aspect enum

### Model Namespace
- **Car** - Train car base class
- **Location** - Track location
- **IndustryComponent** - Industry locations
- **PassengerStop** - Passenger stations
- **ProgressionIndustryComponent** - Progression-based industries

### Model.Definition Namespace
- **CarArchetype** - Car type definitions

### Model.Ops Namespace
- **OpsController** - Operations controller
- **Area** - Geographic areas
- **OpsCarPosition** - Car position in ops system

### RollingStock Namespace
- **BaseLocomotive** - Locomotive base class
- **CarPickable** - Pickable car interaction
- **TrainController** - Main train controller

### Game Namespace
- **StateManager** - Game state management
- **GameStorage** - Persistent storage
- **MapManager** - Map management
- **FlareManager** - Flare management
- **FlarePickable** - Pickable flare interaction

### Game.Events Namespace
- **MapDidLoadEvent** - Map loaded event
- **MapWillUnloadEvent** - Map unloading event
- **WorldDidMoveEvent** - World movement event
- **SwitchThrownDidChange** - Switch state change event
- **CanvasScaleChanged** - Canvas scale change event

### Game.Messages Namespace
- **FlareAddUpdate** - Flare add message

### Helpers Namespace
- **WorldTransformer** - Coordinate transformation
- **GameInput** - Input handling
- **CameraSelector** - Camera selection

### Character Namespace
- **SpawnPoint** - Spawn point locations

### UI.Builder Namespace
- **UIPanelBuilder** - UI panel builder
- **UIPanel** - UI panel base
- **ProgrammaticWindowCreator** - Window creation utility

### UI.CarInspector Namespace
- **CarInspector** - Car inspector UI

### UI.Console.Commands Namespace
- **TeleportCommand** - Teleport console command

## How to Decompile

### Method 1: Using ILSpy Command Line
```powershell
# Install ILSpy
dotnet tool install -g ilspycmd

# Decompile a single DLL
ilspycmd -p -o .\Decompiled\Assembly-CSharp "C:\games\Steam\steamapps\common\Railroader\Railroader_Data\Managed\Assembly-CSharp.dll"

# Or use the provided script
.\DecompileAll.ps1
```

### Method 2: Using ILSpy GUI
1. Download ILSpy from https://github.com/icsharpcode/ILSpy/releases
2. Open ILSpy
3. File -> Open -> Navigate to `C:\games\Steam\steamapps\common\Railroader\Railroader_Data\Managed\`
4. Select `Assembly-CSharp.dll`
5. Right-click assembly -> Save Code -> Choose output directory

### Method 3: Using dnSpy
1. Download dnSpy from https://github.com/dnSpy/dnSpy/releases
2. Open dnSpy
3. File -> Open -> Navigate to `C:\games\Steam\steamapps\common\Railroader\Railroader_Data\Managed\`
4. Select `Assembly-CSharp.dll`
5. File -> Export to Project -> Choose output directory

## Directory Structure

```
Decompiled/
├── Assembly-CSharp/
│   ├── UI/
│   │   ├── Map/
│   │   │   ├── MapBuilder.cs
│   │   │   ├── MapWindow.cs
│   │   │   ├── MapIcon.cs
│   │   │   └── ...
│   │   ├── Builder/
│   │   ├── CarInspector/
│   │   └── Console/
│   ├── Track/
│   │   ├── TrackSegment.cs
│   │   ├── TrackNode.cs
│   │   ├── Graph.cs
│   │   └── Signals/
│   ├── Model/
│   │   ├── Car.cs
│   │   ├── Location.cs
│   │   ├── Ops/
│   │   └── Definition/
│   ├── Game/
│   │   ├── StateManager.cs
│   │   ├── Events/
│   │   └── Messages/
│   └── ...
├── Core/
├── Definition/
├── Map.Runtime/
└── Unity.../
```

## Notes

- The decompiled code is for reference only
- Some features may use obfuscation or code generation
- Unity engine code is proprietary and should not be redistributed
- Game code is copyrighted by the game developers

## Usage in MapEnhancer

The MapEnhancer mod uses Harmony patches to modify game behavior at runtime. Key patch targets include:

1. **MapBuilder** - Track color customization, zoom limits
2. **Car** - Map icon positioning, marker addition
3. **TrackObjectManager** - Junction rebuilding
4. **MapWindow** - Prevent rebuild camera movement
5. **FlareManager** - Flare placement protection
6. **MapIcon** - Text rotation fixes

See `MapEnhancer.cs` for full patch implementation details.
