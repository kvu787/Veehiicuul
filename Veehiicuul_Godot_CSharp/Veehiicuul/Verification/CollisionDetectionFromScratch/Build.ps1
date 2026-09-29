param(
    [switch] $SkipNative
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

function Invoke-Checked {
    param([string] $Program, [string[]] $Arguments)
    & $Program @Arguments
    if ($LASTEXITCODE -ne 0) { throw "'$Program' exited with code $LASTEXITCODE." }
}

$transcriptStarted = $false
try {
    $logFolderPath = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
    New-Item -ItemType Directory -Path $logFolderPath -Force | Out-Null
    Start-Transcript -LiteralPath (Join-Path $logFolderPath 'Build.log') | Out-Null
    $transcriptStarted = $true
    Write-Host "Build logs: $logFolderPath"
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install the .NET 10 SDK.' }

    Invoke-Checked 'dotnet' @('build', (Join-Path $PSScriptRoot 'CollisionDetectionFromScratch.csproj'), '--configuration', 'Release', '--nologo', '-warnaserror')

    if (-not $SkipNative) {
        $godotDirectory = Join-Path $env:UserProfile 'Program\Godot_v4.7.2-stable_mono_win64'
        $godot = Join-Path $godotDirectory 'Godot_v4.7.2-stable_mono_win64_console.exe'
        if (-not (Test-Path -LiteralPath $godot -PathType Leaf)) { throw "Godot 4.7.2 .NET was not found at $godot" }
        if (-not (Test-Path -LiteralPath (Join-Path $godotDirectory '_sc_') -PathType Leaf)) {
            throw "The Godot installation must be self-contained: $godotDirectory"
        }
        $version = & $godot --version
        if ($LASTEXITCODE -ne 0 -or $version -notlike '4.7.2.stable.mono.*') {
            throw "Expected Godot 4.7.2 .NET; found '$version'."
        }
        $template = Join-Path $godotDirectory 'editor_data\export_templates\4.7.2.stable.mono\windows_release_x86_64.exe'
        if (-not (Test-Path -LiteralPath $template -PathType Leaf)) { throw "Matching .NET export templates are missing: $template" }

        $native = Join-Path $PSScriptRoot 'NativeProject'
        New-Item -ItemType Directory -Path (Join-Path $native 'Build') -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $native 'Build\.gdignore') -Value ''
        Invoke-Checked $godot @('--headless', '--editor', '--path', $native, '--import', '--log-file', (Join-Path $logFolderPath 'NativeImport.log'))
        if (Select-String -LiteralPath (Join-Path $logFolderPath 'NativeImport.log') -Pattern '^(?:SCRIPT )?ERROR:' -Quiet) {
            throw 'Godot reported errors during import. See NativeImport.log.'
        }
        Invoke-Checked 'dotnet' @('build', (Join-Path $native 'CollisionDetectionNative.slnx'), '--configuration', 'ExportRelease', '--nologo', '-warnaserror', '-p:Optimize=true')
        Invoke-Checked $godot @('--headless', '--path', $native, '--export-release', 'Windows Desktop', '--log-file', (Join-Path $logFolderPath 'NativeExport.log'))
        if (Select-String -LiteralPath (Join-Path $logFolderPath 'NativeExport.log') -Pattern '^(?:SCRIPT )?ERROR:' -Quiet) {
            throw 'Godot reported an export error. See NativeExport.log.'
        }
        foreach ($relativePath in @('Build\CollisionDetectionNative.exe', 'Build\CollisionDetectionNative.pck', 'Build\data_CollisionDetectionNative_windows_x86_64\CollisionDetectionNative.dll')) {
            if (-not (Test-Path -LiteralPath (Join-Path $native $relativePath) -PathType Leaf)) {
                throw "The engine-side export is incomplete ($relativePath). See NativeExport.log."
            }
        }
    }
    Write-Host 'Build completed.'
}
catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
finally {
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
