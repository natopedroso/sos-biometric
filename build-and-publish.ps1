param(
  [string]$Configuration = "Release",
  [string]$InstallRoot = "C:\sos-biometric"
)

$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "UISupportSample CS.csproj"
$targetExe = Join-Path $InstallRoot "biometric.exe"

function Resolve-MSBuild {
  $msbuild = Get-Command msbuild -ErrorAction SilentlyContinue
  if ($msbuild) {
    return $msbuild.Source
  }

  $candidates = @(
    "C:\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
    "C:\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\amd64\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe"
  )

  foreach ($candidate in $candidates) {
    if (Test-Path $candidate) {
      return $candidate
    }
  }

  throw "MSBuild nao encontrado. Instale Visual Studio Build Tools com workload .NET desktop."
}

$msbuildExe = Resolve-MSBuild
Write-Host "MSBuild:" $msbuildExe

& $msbuildExe $project "/p:Configuration=$Configuration"
if ($LASTEXITCODE -ne 0) {
  throw "Falha no build do sos-biometric."
}

$builtExe = Join-Path $PSScriptRoot "bin\$Configuration\biometric.exe"
if (!(Test-Path $builtExe)) {
  throw "Executavel nao encontrado em $builtExe"
}

New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null
Copy-Item -Path $builtExe -Destination $targetExe -Force

$templatesDir = Join-Path $InstallRoot "data\templates"
New-Item -ItemType Directory -Force -Path $templatesDir | Out-Null

Write-Host "Publicado em:" $targetExe
