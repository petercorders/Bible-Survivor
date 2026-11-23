$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$scratch = Join-Path ([IO.Path]::GetTempPath()) ("adam-win-display-" + [Guid]::NewGuid().ToString("N"))
$name = Split-Path $scratch -Leaf
$registry = "HKCU:\Software\$name"
$width = 'Screenmanager Resolution Width_h182942802'
$height = 'Screenmanager Resolution Height_h2627697771'
$defaultWidth = 'Screenmanager Resolution Width Default_h680557497'
$defaultHeight = 'Screenmanager Resolution Height Default_h1380706816'
$mode = 'Screenmanager Fullscreen mode_h3630240806'

function Invoke-Compiler {
    & cl.exe @args
    if ($LASTEXITCODE -ne 0) { throw "MSVC exited $LASTEXITCODE" }
}

function Invoke-Player([string] $options, [int] $expectedExit = 0) {
    $start = @{
        FilePath = (Join-Path $scratch 'Adam.exe')
        Wait = $true
        PassThru = $true
        WorkingDirectory = $root
    }
    if ($options) { $start.ArgumentList = $options }
    $run = Start-Process @start
    if ($run.ExitCode -ne $expectedExit) { throw "The UnityMain probe exited $($run.ExitCode), expected $expectedExit" }
}

New-Item -ItemType Directory $scratch | Out-Null
Push-Location $scratch
try {
    @"
#define ADAM_DEFAULT_WIDTH 1920
#define ADAM_DEFAULT_HEIGHT 1080
#define ADAM_PREFS_PATH L"Software\\$name"
"@ | Set-Content -Encoding ascii 'AdamDisplayDefaults.h'

    # Unity's ABI receives the command-line tail. The probe checks a spaced and quoted value.
    @'
#include <windows.h>
#include <cwchar>
extern "C" __declspec(dllexport) int UnityMain(HINSTANCE, HINSTANCE, PWSTR args, int)
{
    if (*args == L'\0') return 0;
    return std::wcscmp(args, L"-screen-width 660 -screen-height 440 -screen-fullscreen 0 -logFile \"space name.log\"") == 0 ? 55 : 5;
}
'@ | Set-Content -Encoding ascii 'UnityPlayerProbe.cpp'

    Invoke-Compiler /nologo /std:c++17 /W4 /MT /LD UnityPlayerProbe.cpp /link /OUT:UnityPlayer.dll
    $launcher = Join-Path $root 'UnityGame/EditorNative/WindowsDisplayStartup.cpp'
    Invoke-Compiler /nologo /std:c++17 /utf-8 /W4 /EHsc /O2 /MT /I. /FeAdam.exe $launcher /link /SUBSYSTEM:WINDOWS advapi32.lib user32.lib

    Invoke-Player ''
    if (Test-Path $registry) { throw 'Fresh launch created a registry key' }

    New-Item -Path $registry -Force | Out-Null
    $saved = @{
        $width = 1280
        $height = 800
        $defaultWidth = 1100
        $defaultHeight = 720
        $mode = 1
        'unrelated' = 7755
    }
    foreach ($key in $saved.Keys) {
        New-ItemProperty -Path $registry -Name $key -Value $saved[$key] -PropertyType DWord | Out-Null
    }
    $saved[$defaultWidth] = 1920
    $saved[$defaultHeight] = 1080
    $cases = @(
        @{ Options = ''; ExpectedExit = 0; Interrupted = $false }
        @{ Options = ''; ExpectedExit = 0; Interrupted = $true }
        @{ Options = '-screen-width 660 -screen-height 440 -screen-fullscreen 0 -logFile "space name.log"'; ExpectedExit = 55; Interrupted = $false }
    )
    foreach ($case in $cases) {
        if ($case.Interrupted) {
            Set-ItemProperty -Path $registry -Name $defaultHeight -Value 720
        }
        Invoke-Player $case.Options $case.ExpectedExit
        foreach ($key in $saved.Keys) {
            if ((Get-ItemPropertyValue -Path $registry -Name $key) -ne $saved[$key]) {
                throw "Unexpected value for $key"
            }
        }
    }
    Write-Host 'PASS: Windows launcher fresh/update/retry and exact command-line forwarding through the UnityMain probe.'
}
finally {
    Pop-Location
    if (Test-Path $registry) { Remove-Item -Path $registry -Recurse -Force }
    Remove-Item -Path $scratch -Recurse -Force
}
