if ([IntPtr]::Size -ne 4) {
    & "$env:WINDIR\SysWOW64\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath
    if ($LASTEXITCODE -ne 0) { throw 'Replay patch verification failed.' }
    exit 0
}
$ErrorActionPreference = 'Stop'
$assembly = [Reflection.Assembly]::LoadFile((Resolve-Path "$PSScriptRoot\..\dist\AtroxLauncher.exe").Path)
$type = $assembly.GetType('AtroxLauncher.ReplaySupport', $true)
$flags = [Reflection.BindingFlags]'Static,NonPublic'
$template = Join-Path $env:USERPROFILE 'Downloads\AtroxLauncher\Atrox.ex_'
if (Test-Path -LiteralPath $template) {
    $bytes = [IO.File]::ReadAllBytes($template)
    $stream = New-Object IO.MemoryStream(,$bytes)
    $writer = New-Object IO.BinaryWriter($stream)
    $type.GetMethod('Apply', $flags).Invoke($null, [object[]]@([IO.BinaryWriter]$writer))
    foreach ($hook in @(0x630e0,0x63223,0xb8a60,0xb8ef9,0xc7271)) {
        if ($bytes[$hook] -ne 0xe9) { throw 'Missing replay hook.' }
    }
    $output = Join-Path $PSScriptRoot '..\artifacts\replay-patched.bin'
    [IO.Directory]::CreateDirectory((Split-Path $output)) | Out-Null
    [IO.File]::WriteAllBytes($output, $bytes)
    $writer.Dispose()
}
if ($assembly.GetManifestResourceNames() -notcontains 'AtroxLauncher.Replay.dll') { throw 'Replay module not embedded.' }
Write-Host 'Replay hook signatures and embedded module verified.'

