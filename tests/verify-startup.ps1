$ErrorActionPreference = 'Stop'
$source = (Resolve-Path (Join-Path $PSScriptRoot '..\dist\AtroxLauncher.exe')).Path
$version = [Reflection.AssemblyName]::GetAssemblyName($source).Version.ToString(3)
foreach ($collision in @($false, $true)) {
    $directory = Join-Path ([IO.Path]::GetTempPath()) ('AtroxStartupVerify-' + [Guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory($directory) | Out-Null
    $original = Join-Path $directory 'AtroxLauncher.exe'
    $versioned = Join-Path $directory ('AtroxLauncher_v' + $version + '.exe')
    $result = Join-Path $directory 'startup.txt'
    [IO.File]::Copy($source, $original)
    if ($collision) { [IO.File]::WriteAllText($versioned, 'existing file must survive') }
    $process = Start-Process -FilePath $original -ArgumentList @('--startup-check', ('"' + $result + '"')) -WorkingDirectory $directory -WindowStyle Hidden -PassThru
    if (!$process.WaitForExit(30000)) { throw "Standalone startup timed out: $directory" }
    if ($process.ExitCode -ne 0 -or !(Test-Path $result) -or !(Get-Content $result -Raw).StartsWith('OK')) {
        throw "Standalone startup failed: $directory"
    }
    if ($collision -and (Get-Content $versioned -Raw) -ne 'existing file must survive') { throw 'Startup rename overwrote an existing file.' }
    if (!$collision -and !(Test-Path $versioned)) { throw 'Running executable was not renamed.' }
    # The directory contains only the copied EXE(s) and diagnostic output.
    foreach ($file in [IO.Directory]::GetFiles($directory)) { [IO.File]::Delete($file) }
    [IO.Directory]::Delete($directory)
}
Write-Host 'Standalone process startup passed: EXE-only dependency loading, live rename and rename collision fallback.'
