$ErrorActionPreference='Stop'
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(-not (Test-Path -LiteralPath $taskCompiler)){throw '.NET Framework C# compiler is required.'}
& $taskCompiler /nologo /target:exe /r:System.Web.Extensions.dll ('/out:'+(Join-Path $PSScriptRoot 'BeeResponseLauncher.exe')) (Join-Path $PSScriptRoot 'BeeResponseLauncher.cs')
if($LASTEXITCODE -ne 0){throw 'Could not compile the response-file launcher.'}
Write-Output 'Compiled Tools/BeeResponseLauncher.exe from the included source.'
