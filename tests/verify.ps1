if ([IntPtr]::Size -ne 4) {
    & "$env:WINDIR\SysWOW64\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath
    if ($LASTEXITCODE -ne 0) { throw '32-bit launcher verification failed.' }
    exit 0
}
$ErrorActionPreference = 'Stop'
$exe = Join-Path $PSScriptRoot '..\dist\AtroxLauncher.exe'
$assembly = [Reflection.Assembly]::LoadFile((Resolve-Path $exe).Path)
$flags = [Reflection.BindingFlags]'Static,NonPublic'
$type = $assembly.GetType('AtroxLauncher.GameFiles', $true)
$resolve = $type.GetMethod('ResolvePath', $flags)
$base = Join-Path ([IO.Path]::GetTempPath()) 'AtroxLauncherVerify'
$actual = $resolve.Invoke($null, [object[]]@('.\Atrox', [string]$base))
$expected = Join-Path $base 'Atrox'
if ($actual -ne $expected) { throw 'Relative game path was not resolved against the config directory.' }
$absolute = $resolve.Invoke($null, [object[]]@([string]$expected, 'C:\unrelated'))
if ($absolute -ne $expected) { throw 'Absolute game path changed.' }
$hash = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant()
if ($hash -ne (Get-Content "$exe.sha256" -Raw).Trim()) { throw 'Release checksum mismatch.' }
if (!($assembly.GetManifestResourceNames() -contains 'AtroxLauncher.Newtonsoft.Json.dll')) { throw 'Portable JSON dependency missing.' }
$legacy = Join-Path $env:USERPROFILE 'Downloads\AtroxLauncher'
if (Test-Path (Join-Path $legacy 'Atrox.ex_')) {
    $find = $type.GetMethod('FindTemplate', $flags)
    $template = $find.Invoke($null, [object[]]@([string](Join-Path $legacy 'Atrox')))
    if ((Get-FileHash $template).Hash.ToLowerInvariant() -ne $type.GetField('TemplateHash', $flags).GetRawConstantValue()) { throw 'Unsupported template selected.' }
    Write-Host "Existing game template discovered: $template"
}
Write-Host 'Launcher verification passed.'


