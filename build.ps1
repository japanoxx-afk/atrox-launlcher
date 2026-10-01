param([string]$Version = '1.3.0.0')
$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$msbuild = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe'
if (!(Test-Path -LiteralPath $msbuild)) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $msbuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
}
$packageDll = Join-Path $repoRoot 'packages\Newtonsoft.Json.12.0.2\lib\net45\Newtonsoft.Json.dll'
if (!(Test-Path -LiteralPath $packageDll)) {
    $cache = Join-Path $repoRoot 'packages'
    New-Item -ItemType Directory -Force $cache | Out-Null
    $download = Join-Path $cache 'newtonsoft.zip'
    Invoke-WebRequest 'https://api.nuget.org/v3-flatcontainer/newtonsoft.json/12.0.2/newtonsoft.json.12.0.2.nupkg' -OutFile $download
    Expand-Archive -LiteralPath $download -DestinationPath (Join-Path $cache 'Newtonsoft.Json.12.0.2') -Force
}
$arguments = @((Join-Path $repoRoot 'src\AtroxLauncher.csproj'), '/p:Configuration=Release', '/verbosity:minimal', '/nologo')
# A .NET Framework runtime is sufficient on this host. CI uses installed targeting assemblies.
if (!(Test-Path "${env:ProgramFiles(x86)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2")) {
    $arguments += '/p:FrameworkPathOverride=C:\Windows\Microsoft.NET\Framework\v4.0.30319'
}
& $msbuild @arguments
if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed' }
$destination = Join-Path $repoRoot 'dist'
New-Item -ItemType Directory -Force $destination | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'src\bin\Release\AtroxLauncher.exe') -Destination $destination -Force
$exePath = Join-Path $destination 'AtroxLauncher.exe'
$actualVersion = [Reflection.AssemblyName]::GetAssemblyName($exePath).Version.ToString()
if ($actualVersion -ne $Version) { throw "Expected $Version, built $actualVersion. Update AssemblyInfo.cs first." }
(Get-FileHash -LiteralPath $exePath -Algorithm SHA256).Hash.ToLowerInvariant() | Set-Content (Join-Path $destination 'AtroxLauncher.exe.sha256') -Encoding ascii
Write-Host "Built $exePath ($actualVersion)"
