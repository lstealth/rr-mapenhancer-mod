@echo off
REM DecompileAllDLLs.bat
REM Batch script to decompile all referenced DLLs from the Railroader game

SETLOCAL ENABLEDELAYEDEXPANSION

SET GAME_DIR=C:\games\Steam\steamapps\common\Railroader
SET MANAGED_DIR=%GAME_DIR%\Railroader_Data\Managed
SET OUTPUT_DIR=%~dp0Decompiled

echo ===================================
echo Railroader Assembly Decompiler
echo ===================================
echo.
echo Game Directory: %GAME_DIR%
echo Managed Directory: %MANAGED_DIR%
echo Output Directory: %OUTPUT_DIR%
echo.

REM Check if ILSpy is installed
where ilspycmd >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [INFO] ILSpy command line tool not found. Installing...
    dotnet tool install -g ilspycmd
    if %ERRORLEVEL% NEQ 0 (
        echo [ERROR] Failed to install ILSpy. Please install manually:
        echo dotnet tool install -g ilspycmd
        pause
        exit /b 1
    )
    echo [OK] ILSpy installed successfully
    echo.
)

REM Create output directory
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

REM List of DLLs to decompile
SET DLLS=Assembly-CSharp.dll Core.dll Definition.dll Map.Runtime.dll Unity.InputSystem.dll Unity.RenderPipelines.Universal.Runtime.dll Unity.TextMeshPro.dll

echo ===================================
echo Decompiling DLLs...
echo ===================================
echo.

for %%D in (%DLLS%) do (
    SET DLL_PATH=%MANAGED_DIR%\%%D
    SET DLL_NAME=%%~nD
    SET DLL_OUTPUT=%OUTPUT_DIR%\!DLL_NAME!
    
    echo [DECOMPILING] %%D
    echo   Source: !DLL_PATH!
    echo   Output: !DLL_OUTPUT!
    
    if exist "!DLL_PATH!" (
        ilspycmd -p -o "!DLL_OUTPUT!" "!DLL_PATH!"
        if !ERRORLEVEL! EQU 0 (
            echo   [OK] Decompiled successfully
        ) else (
            echo   [ERROR] Decompilation failed
        )
    ) else (
        echo   [SKIP] DLL not found
    )
    echo.
)

echo ===================================
echo Unity Engine Modules
echo ===================================
echo.
echo [INFO] Unity Engine DLLs are proprietary and large.
echo [INFO] Decompiling only if needed...
echo.

SET /P DECOMPILE_UNITY="Decompile Unity Engine DLLs? (y/N): "
if /I "!DECOMPILE_UNITY!"=="Y" (
    SET UNITY_DLLS=UnityEngine.dll UnityEngine.CoreModule.dll UnityEngine.IMGUIModule.dll UnityEngine.InputModule.dll UnityEngine.PhysicsModule.dll UnityEngine.UI.dll UnityEngine.UIModule.dll UnityEngine.ImageConversionModule.dll UnityEngine.InputLegacyModule.dll
    
    for %%U in (!UNITY_DLLS!) do (
        SET DLL_PATH=%MANAGED_DIR%\%%U
        SET DLL_NAME=%%~nU
        SET DLL_OUTPUT=%OUTPUT_DIR%\!DLL_NAME!
        
        echo [DECOMPILING] %%U
        
        if exist "!DLL_PATH!" (
            ilspycmd -p -o "!DLL_OUTPUT!" "!DLL_PATH!"
            if !ERRORLEVEL! EQU 0 (
                echo   [OK] Decompiled successfully
            ) else (
                echo   [ERROR] Decompilation failed
            )
        ) else (
            echo   [SKIP] DLL not found
        )
        echo.
    )
)

echo ===================================
echo Decompilation Complete!
echo ===================================
echo.
echo Output directory: %OUTPUT_DIR%
echo.
echo You can now reference the decompiled code in your project.
echo.

pause
