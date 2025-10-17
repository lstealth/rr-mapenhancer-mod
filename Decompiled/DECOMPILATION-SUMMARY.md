# Decompilation Summary

## Successfully Decompiled Assemblies

### ✅ Assembly-CSharp.dll

The main game assembly (`Assembly-CSharp.dll`) has been successfully decompiled to:
```
.\Decompiled\Assembly-CSharp\
```

**Statistics:**
- **Types**: 2,485
- **Methods**: 11,897  
- **Namespaces**: 113

**Key Files Decompiled:**

| File | Location | Status |
|------|----------|--------|
| MapBuilder.cs | `.\Decompiled\Assembly-CSharp\UI.Map\MapBuilder.cs` | ✅ |
| MapWindow.cs | `.\Decompiled\Assembly-CSharp\UI.Map\MapWindow.cs` | ✅ |
| MapIcon.cs | `.\Decompiled\Assembly-CSharp\UI.Map\MapIcon.cs` | ✅ |
| MapLabel.cs | `.\Decompiled\Assembly-CSharp\UI.Map\MapLabel.cs` | ✅ |
| Car.cs | `.\Decompiled\Assembly-CSharp\Model\Car.cs` | ✅ |
| TrainController.cs | `.\Decompiled\Assembly-CSharp\TrainController.cs` | ✅ |
| TrackSegment.cs | `.\Decompiled\Assembly-CSharp\Track\TrackSegment.cs` | ✅ |
| TrackNode.cs | `.\Decompiled\Assembly-CSharp\Track\TrackNode.cs` | ✅ |
| Graph.cs | `.\Decompiled\Assembly-CSharp\Track\Graph.cs` | ✅ |
| CTCBlock.cs | `.\Decompiled\Assembly-CSharp\Track.Signals\CTCBlock.cs` | ✅ |
| CTCSignal.cs | `.\Decompiled\Assembly-CSharp\Track.Signals\CTCSignal.cs` | ✅ |
| FlareManager.cs | `.\Decompiled\Assembly-CSharp\Game\FlareManager.cs` | ✅ |
| IndustryComponent.cs | `.\Decompiled\Assembly-CSharp\Model\IndustryComponent.cs` | ✅ |
| PassengerStop.cs | `.\Decompiled\Assembly-CSharp\Model\PassengerStop.cs` | ✅ |

## Quick Start

### Browse Decompiled Code

All decompiled code is organized by namespace:

```
Decompiled/
└── Assembly-CSharp/
    ├── Analytics/
    ├── Audio/
    ├── Character/
    ├── Game/
    │   ├── Events/
    │   ├── Messages/
    │   └── State/
    ├── Helpers/
    ├── Model/
    │   ├── Definition/
    │   └── Ops/
    ├── RollingStock/
    ├── Track/
    │   └── Signals/
    ├── UI/
    │   ├── Builder/
    │   ├── CarInspector/
    │   ├── Console/
    │   │   └── Commands/
    │   └── Map/
    └── ...
```

### Viewing Decompiled Files

You can open any `.cs` file in Visual Studio, VS Code, or any text editor:

```powershell
# Open MapBuilder in VS Code
code ".\Decompiled\Assembly-CSharp\UI.Map\MapBuilder.cs"

# Open entire decompiled directory
code ".\Decompiled\Assembly-CSharp"
```

### Search for Types

```powershell
# Find all files containing "MapIcon"
Get-ChildItem ".\Decompiled\Assembly-CSharp" -Recurse -Filter "*MapIcon*.cs"

# Search within files
Get-ChildItem ".\Decompiled\Assembly-CSharp" -Recurse -Filter "*.cs" | 
    Select-String "public class.*MapIcon" -List | 
    Select-Object Path, LineNumber
```

## Additional Assemblies

To decompile other referenced DLLs, use the provided scripts:

### PowerShell Script
```powershell
.\DecompileAll.ps1
```

### Batch Script
```batch
DecompileAllDLLs.bat
```

### Manual Decompilation
```powershell
# Decompile Core.dll
ilspycmd -p -o ".\Decompiled\Core" "C:\games\Steam\steamapps\common\Railroader\Railroader_Data\Managed\Core.dll"

# Decompile Definition.dll
ilspycmd -p -o ".\Decompiled\Definition" "C:\games\Steam\steamapps\common\Railroader\Railroader_Data\Managed\Definition.dll"

# Decompile Map.Runtime.dll
ilspycmd -p -o ".\Decompiled\Map.Runtime" "C:\games\Steam\steamapps\common\Railroader\Railroader_Data\Managed\Map.Runtime.dll"
```

## Integration with MapEnhancer

The decompiled code can be used as reference when:

1. **Creating Harmony Patches** - Understanding original implementation
2. **Accessing Internal Members** - Using publicizer to expose internals
3. **Understanding Game Flow** - Tracing method calls and dependencies
4. **Debugging** - Comparing expected vs actual behavior
5. **Feature Development** - Finding extension points

### Example Usage

```csharp
// In MapEnhancer.cs, you patch MapBuilder.TrackColorMainline
// Reference: .\Decompiled\Assembly-CSharp\UI.Map\MapBuilder.cs

[HarmonyPatch(typeof(MapBuilder), nameof(MapBuilder.TrackColorMainline), MethodType.Getter)]
private static class TrackColorMainlinePatch
{
    private static bool Prefix(ref Color __result)
    {
        // Your custom color logic
        __result = Instance?.Settings.TrackColorMainline ?? 
                   Loader.MapEnhancerSettings.TrackColorMainlineOrig;
        return false;
    }
}
```

## Tools Used

- **ILSpy** (v9.1.0.7988) - .NET assembly decompiler
- **ilspycmd** - Command-line version of ILSpy

## Notes

- ⚠️ Decompiled code is for reference only
- ⚠️ Do not redistribute game code
- ⚠️ Some code may be obfuscated or compiler-generated
- ⚠️ Unity engine assemblies are proprietary
- ✅ Use for mod development and debugging
- ✅ Helpful for understanding game APIs

## File Locations

| Item | Path |
|------|------|
| Game Install | `C:\games\Steam\steamapps\common\Railroader\` |
| Managed DLLs | `C:\games\Steam\steamapps\common\Railroader\Railroader_Data\Managed\` |
| Decompiled Code | `.\Decompiled\` |
| MapEnhancer Mod | `.\MapEnhancer\` |

## Support

For issues with decompilation:
1. Ensure ILSpy is installed: `dotnet tool list -g`
2. Update ILSpy: `dotnet tool update -g ilspycmd`
3. Check DLL exists in game directory
4. Verify sufficient disk space for decompiled output

## References

- ILSpy: https://github.com/icsharpcode/ILSpy
- Harmony: https://harmony.pardeike.net/
- BepInEx Publicizer: https://github.com/BepInEx/BepInEx.AssemblyPublicizer
