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


$patches = $assembly.GetType('AtroxLauncher.GamePatches', $true)
$code = [byte[]]$patches.GetMethod('BuildHudClear', $flags).Invoke($null, $null)
if ($code.Length -gt 176 -or $code[0] -ne 0x9c -or $code[1] -ne 0x60) { throw 'HUD trampoline exceeds padding or does not save state.' }
$fixture = New-Object byte[] 0x64000
([byte[]]@(0x8b,0x4c,0x24,0x18,0x56,0xe8,0xfd,0x9d,0xfd,0xff)).CopyTo($fixture, 0x2d473)
for ($index=0x2d2b0; $index -lt 0x2d330; $index++) { $fixture[$index]=0xcc }
([byte[]]@(0x8b,0x0d,0x30,0x77,0xb2,0)).CopyTo($fixture, 0x63209)
for ($index=0x63610; $index -lt 0x636c0; $index++) { $fixture[$index]=0xcc }
$stream = New-Object IO.MemoryStream(,$fixture)
$writer = New-Object IO.BinaryWriter($stream)
$apply = $patches.GetMethod('Apply', $flags)
$apply.Invoke($null, [object[]]@([IO.BinaryWriter]$writer, $true, [int]40))
if (0x2d473 + 5 + [BitConverter]::ToInt32($fixture, 0x2d474) -ne 0x2d2b0) { throw 'Scroll convergence hook missing.' }
if ($fixture[0x63209] -ne 0xe9) { throw 'HUD draw hook missing.' }
if (0x63209 + 5 + [BitConverter]::ToInt32($fixture, 0x6320a) -ne 0x63610) { throw 'HUD hook jumps to wrong address.' }
if (0x63610 + $code.Length + [BitConverter]::ToInt32($code, $code.Length-4) -ne 0x6320f) { throw 'HUD trampoline does not resume before UI draw.' }
try { $apply.Invoke($null, [object[]]@([IO.BinaryWriter]$writer, $true, [int]40)); throw 'Modified template was accepted.' }
catch { if ($_.Exception -isnot [IO.InvalidDataException] -and $_.Exception.InnerException -isnot [IO.InvalidDataException]) { throw } }
$writer.Dispose()
Write-Host 'Native HUD and scroll patch verification passed.'
if ($env:ATROX_EXPORT_PATCH) {
    [IO.File]::WriteAllBytes($env:ATROX_EXPORT_PATCH, $code)
    foreach ($rate in @(5,10,20,40,60,80,100)) {
        $scrollCode = [byte[]]$patches.GetMethod('BuildScroll', $flags).Invoke($null, [object[]]@([int]$rate))
        if ($scrollCode.Length -gt 128) { throw 'Scroll code exceeds padding.' }
        [IO.File]::WriteAllBytes((Join-Path (Split-Path $env:ATROX_EXPORT_PATCH) "scroll-$rate.bin"), $scrollCode)
    }
}



$renderer = $assembly.GetType('AtroxLauncher.GameRenderer', $true)
$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ('AtroxRendererVerify-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $testDirectory | Out-Null
$ini = Join-Path $testDirectory 'ddraw.ini'
[IO.File]::WriteAllText($ini, "[UserSettings]`r`nKeep=unchanged`r`n")
$oldDll = Join-Path $testDirectory 'ddraw.dll'
[IO.File]::WriteAllText($oldDll, 'previous user renderer')
$oldHash = (Get-FileHash $oldDll).Hash.ToLowerInvariant()
$install = $renderer.GetMethod('Install', $flags)
$install.Invoke($null, [object[]]@([string]$testDirectory, $true))
if ((Get-FileHash $oldDll).Hash.ToLowerInvariant() -ne $renderer.GetField('RendererHash', $flags).GetRawConstantValue()) { throw 'Renderer resource checksum failed.' }
if ((Get-Content "$oldDll.before-launcher-$oldHash" -Raw) -ne 'previous user renderer') { throw 'Previous renderer was not preserved.' }
$settings = Get-Content $ini -Raw
if ($settings -notmatch 'maintas=false' -or $settings -notmatch 'boxing=false' -or $settings -notmatch 'Keep=unchanged' -or $settings -notmatch 'fullscreen=false' -or $settings -notmatch 'toggle_borderless=true') { throw 'Windowed settings or user preservation failed.' }
$install.Invoke($null, [object[]]@([string]$testDirectory, $false))
if ((Get-Content $ini -Raw) -notmatch 'fullscreen=true') { throw 'Initial fullscreen setting failed.' }
if (!(Test-Path (Join-Path $testDirectory 'cnc-ddraw-LICENSE.txt'))) { throw 'Renderer license was not installed.' }
Write-Host 'Renderer installation, settings preservation and fullscreen defaults passed.'
