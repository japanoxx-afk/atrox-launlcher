if ([IntPtr]::Size -ne 4) {
    & "$env:WINDIR\SysWOW64\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath
    if ($LASTEXITCODE -ne 0) { throw 'Resolution verification failed.' }
    exit 0
}
$ErrorActionPreference = 'Stop'
$assembly = [Reflection.Assembly]::LoadFile((Resolve-Path "$PSScriptRoot\..\dist\AtroxLauncher.exe").Path)
$flags = [Reflection.BindingFlags]'Static,NonPublic'
$template = Join-Path $env:USERPROFILE 'Downloads\AtroxLauncher\Atrox.ex_'
if (!(Test-Path $template)) { Write-Host 'Local game template absent; resolution integration skipped.'; exit 0 }
foreach ($size in @(@(800,600),@(1024,768),@(1280,1024))) {
    $w = $size[0]; $h = $size[1]
    $bytes = [IO.File]::ReadAllBytes($template)
    $stream = New-Object IO.MemoryStream(,$bytes)
    $writer = New-Object IO.BinaryWriter($stream)
    foreach ($type in @('ReplaySupport','ConstructionSupport','GameplaySupport')) {
        $assembly.GetType('AtroxLauncher.' + $type,$true).GetMethod('Apply',$flags).Invoke($null,[object[]]@([IO.BinaryWriter]$writer))
    }
    $assembly.GetType('AtroxLauncher.GamePatches',$true).GetMethod('Apply',$flags).Invoke($null,[object[]]@([IO.BinaryWriter]$writer,[int]$w,[int]$h,[int]10))
    if ($w -gt 800) {
        foreach ($offset in @(0xdc862,0xdc876,0x195a9)) { if ([BitConverter]::ToInt32($bytes,$offset) -ne $w) { throw 'Wrong horizontal bounds' } }
        foreach ($offset in @(0xdc86e,0xdc87d,0x195ae,0x1d5c4)) { if ([BitConverter]::ToInt32($bytes,$offset) -ne $h) { throw 'Wrong vertical bounds' } }
        if ([BitConverter]::ToInt32($bytes,0x1d95f) -ne ($h-600)) { throw 'Menu centering differs from viewport' }
        if ([BitConverter]::ToInt32($bytes,0x23859) -ne ($h-77)) { throw 'HUD vertical anchor incorrect' }
        if ([BitConverter]::ToInt32($bytes,0x2384f) -ne ($w/2+120)) { throw 'HUD horizontal anchor incorrect' }
        $extra = ([Math]::Ceiling($h/40)+1)*40-$h
        if ($bytes[0x17c60f] -ne $extra -or $bytes[0x17561f] -ne $extra) { throw 'Terrain cache capacity incorrect' }
    } else {
        $original = [IO.File]::ReadAllBytes($template)
        foreach ($offset in @(0xdc862,0xdc86e,0xdc876,0xdc87d,0x195a9,0x195ae,0x1d5c4,0x23859,0x2384f)) {
            if ([BitConverter]::ToInt32($bytes,$offset) -ne [BitConverter]::ToInt32($original,$offset)) { throw 'Original resolution changed' }
        }
    }
    [IO.File]::WriteAllBytes((Join-Path $PSScriptRoot "..\artifacts\resolution-$w-$h.bin"),$bytes)
    $writer.Dispose()
    Write-Host "Full game/replay/construction integration passed: $w x $h"
}
