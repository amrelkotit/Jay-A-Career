$ErrorActionPreference='Stop'
$taskSource=Split-Path -Parent $PSScriptRoot
$taskProject='C:\Users\LENOVO\AppData\Local\Temp\JayCareerBuild20261013'
if(Get-CimInstance Win32_Process | Where-Object {$_.Name -eq 'Unity.exe' -and $_.CommandLine.Contains($taskProject)}){throw 'Wait for the previous build editor to finish.'}
$taskBee=[IO.Path]::GetFullPath((Join-Path $taskProject 'Library\Bee'))
if(-not $taskBee.StartsWith(($taskProject+'\'),[StringComparison]::OrdinalIgnoreCase)){throw 'Unexpected build-cache location.'}
$taskBackup=Join-Path $taskBee ('LabeledGraphBackup-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $taskBackup -Force | Out-Null
foreach($name in (Get-ChildItem -LiteralPath $taskBee -File | Where-Object {$_.Name -match '\.dag(\.json)?$'} | Select-Object -ExpandProperty Name)){
 $taskFile=[IO.Path]::GetFullPath((Join-Path $taskBee $name))
 if(-not $taskFile.StartsWith(($taskBee+'\'),[StringComparison]::OrdinalIgnoreCase)){throw 'Graph target escaped its generated cache.'}
 if(Test-Path -LiteralPath $taskFile){Move-Item -LiteralPath $taskFile -Destination (Join-Path $taskBackup $name)}
}
& (Join-Path $PSScriptRoot 'compile_response_launcher.ps1')
& (Join-Path $PSScriptRoot 'build_labeled_release.ps1')
