Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Godot {
    param([string[]] $Arguments, [string] $Name)
    $process = New-Object System.Diagnostics.Process
    try {
        $process.StartInfo.FileName = $godot
        $process.StartInfo.Arguments = ($Arguments | ForEach-Object { '"' + $_ + '"' }) -join ' '
        $process.StartInfo.WorkingDirectory = $projectPath
        $process.StartInfo.UseShellExecute = $false
        $process.StartInfo.CreateNoWindow = $true
        $process.StartInfo.WindowStyle = 'Hidden'
        $process.StartInfo.RedirectStandardOutput = $true
        $process.StartInfo.RedirectStandardError = $true
        if (-not $process.Start()) { throw 'Could not start Godot.' }
        $output = $process.StandardOutput.ReadToEndAsync()
        $errors = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $combined = $output.Result + "`n" + $errors.Result
        Set-Content -LiteralPath (Join-Path $logFolderPath ($Name + 'Console.log')) -Value $combined
        if ($process.ExitCode -ne 0 -or $combined -match '(?m)^(SCRIPT ERROR:|ERROR:)') {
            Write-Host $combined
            throw "Godot $Name failed. See $logFolderPath"
        }
        return $combined.Trim()
    } finally {
        $process.Dispose()
    }
}

$transcriptStarted = $false
$resultCode = 0
try {
    $logFolderPath = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
    New-Item -ItemType Directory -Path $logFolderPath -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $PSScriptRoot 'MyLogOutput\.gdignore') -Value ''
    Start-Transcript -LiteralPath (Join-Path $logFolderPath 'Build.log') | Out-Null
    $transcriptStarted = $true
    Write-Host "Build logs: $logFolderPath"

    $projectPath = Join-Path $PSScriptRoot 'Veehiicuul'
    $godotDirectory = Join-Path $env:UserProfile 'Program\Godot_v4.7.2-stable_win64.exe'
    $godot = Join-Path $godotDirectory 'Godot_v4.7.2-stable_win64_console.exe'
    if (-not (Test-Path -LiteralPath $godot -PathType Leaf)) { throw "Standard Godot 4.7.2 is missing: $godot" }
    if (-not (Test-Path -LiteralPath (Join-Path $godotDirectory '_sc_') -PathType Leaf)) { throw 'The Godot installation must be self-contained.' }
    $template = Join-Path $godotDirectory 'editor_data\export_templates\4.7.2.stable\windows_release_x86_64.exe'
    if (-not (Test-Path -LiteralPath $template -PathType Leaf)) { throw "Standard Godot 4.7.2 release templates are missing: $template" }
    $version = Invoke-Godot -Arguments @('--version') -Name 'Version'
    if ($version -notmatch '^4\.7\.2\.stable\.' -or $version -match '\.mono\.') { throw "Expected standard Godot 4.7.2; found '$version'." }
    $managedFiles = Get-ChildItem -LiteralPath $projectPath -Recurse -File | Where-Object { $_.Extension -in @('.cs', '.csproj', '.sln', '.slnx') }
    if ($managedFiles) { throw 'The GDScript project must not contain C# or .NET project files.' }

    Write-Host 'Importing assets and checking GDScript...'
    Invoke-Godot -Arguments @('--headless', '--editor', '--path', $projectPath, '--import', '--log-file', (Join-Path $logFolderPath 'Import.log')) -Name 'Import' | Out-Null
    $buildPath = Join-Path $PSScriptRoot 'Build'
    New-Item -ItemType Directory -Path $buildPath -Force | Out-Null
    $executable = Join-Path $buildPath 'Veehiicuul_Godot_GDScript.exe'
    Write-Host 'Exporting the optimized Windows x64 release...'
    Invoke-Godot -Arguments @('--headless', '--path', $projectPath, '--export-release', 'Windows Desktop', $executable, '--log-file', (Join-Path $logFolderPath 'Export.log')) -Name 'Export' | Out-Null
    foreach ($name in @('Veehiicuul_Godot_GDScript.exe', 'Veehiicuul_Godot_GDScript.pck')) {
        if (-not (Test-Path -LiteralPath (Join-Path $buildPath $name) -PathType Leaf)) { throw "Application export is incomplete: $name" }
    }
    foreach ($name in @('Import.log', 'Export.log')) {
        if (Select-String -LiteralPath (Join-Path $logFolderPath $name) -Pattern '^(SCRIPT ERROR:|ERROR:)' -Quiet) { throw "Godot reported errors in $name" }
    }
    Write-Host "Built application: $executable"
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    $resultCode = 1
} finally {
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
exit $resultCode
