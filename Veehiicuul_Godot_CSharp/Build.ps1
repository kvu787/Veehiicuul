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
    Set-Content -LiteralPath (Join-Path $PSScriptRoot 'MyLogOutput\.gdignore') -Value ''
    Start-Transcript -LiteralPath (Join-Path $logFolderPath 'Build.log') | Out-Null
    $transcriptStarted = $true
    Write-Host "Build logs: $logFolderPath"

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
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install the .NET 10 SDK.' }
    $template = Join-Path $godotDirectory 'editor_data\export_templates\4.7.2.stable.mono\windows_release_x86_64.exe'
    if (-not (Test-Path -LiteralPath $template -PathType Leaf)) { throw "Matching .NET export templates are missing: $template" }

    New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'Build') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $PSScriptRoot 'Build\.gdignore') -Value ''
    # Import first so the exporter knows about C# scripts and scene resources.
    Invoke-Checked $godot @('--headless', '--editor', '--path', $PSScriptRoot, '--import', '--log-file', (Join-Path $logFolderPath 'Import.log'))
    if (Select-String -LiteralPath (Join-Path $logFolderPath 'Import.log') -Pattern '^ERROR:' -Quiet) {
        throw 'Godot reported an import error. See Import.log.'
    }
    Invoke-Checked 'dotnet' @('build', 'Veehiicuul_Godot_CSharp.slnx', '--configuration', 'ExportRelease', '--nologo', '-warnaserror')
    Invoke-Checked $godot @('--headless', '--path', $PSScriptRoot, '--export-release', 'Windows Desktop', '--log-file', (Join-Path $logFolderPath 'Export.log'))
    # Some export-plugin failures are logged even when Godot returns success.
    if (Select-String -LiteralPath (Join-Path $logFolderPath 'Export.log') -Pattern '^ERROR:' -Quiet) {
        throw 'Godot reported an export error. See Export.log.'
    }
    foreach ($relativePath in @('Build\Veehiicuul_Godot_CSharp.exe', 'Build\Veehiicuul_Godot_CSharp.pck', 'Build\data_Veehiicuul_Godot_CSharp_windows_x86_64\Veehiicuul_Godot_CSharp.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot $relativePath) -PathType Leaf)) {
            throw "Application export is incomplete ($relativePath). See Export.log."
        }
    }
    Write-Host "Built application: $(Join-Path $PSScriptRoot 'Build\Veehiicuul_Godot_CSharp.exe')"
}
catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
finally {
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
