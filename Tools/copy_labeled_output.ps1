$ErrorActionPreference='Stop'
$taskSource=Split-Path -Parent $PSScriptRoot
$taskProject='C:\Users\LENOVO\AppData\Local\Temp\JayCareerBuild20261013'
$taskSummaryPath=Join-Path $taskProject 'Deliverables\Evidence\labeled-windows-build-20261015.json'
$taskSummary=Get-Content -Raw -LiteralPath $taskSummaryPath | ConvertFrom-Json
$taskLog=Join-Path $taskSource 'Deliverables\Evidence\labeled-full-build-20261015.log'
if($taskSummary.result -ne 'Succeeded' -or $taskSummary.errors -ne 0 -or -not (Select-String -LiteralPath $taskLog -Pattern 'Build Finished, Result: Success' -Quiet)){throw 'No successful completed full build to copy.'}
Copy-Item -LiteralPath (Join-Path $taskProject 'Assets\JayACareer\Scenes\JayACareer_Realism.unity') -Destination (Join-Path $taskSource 'Assets\JayACareer\Scenes\JayACareer_Realism.unity')
foreach($relative in @('Assets\JayACareer\Textures\ObjectNotes','Assets\JayACareer\Materials\ObjectNotes','Assets\JayACareer\Prefabs\Realism')){Copy-Item -LiteralPath (Join-Path $taskProject $relative) -Destination (Split-Path -Parent (Join-Path $taskSource $relative)) -Recurse -Force}
foreach($relative in @('Assets\JayACareer\Textures\ObjectNotes.meta','Assets\JayACareer\Materials\ObjectNotes.meta','Assets\JayACareer\Editor\UsabilityLightingUpgrade.cs.meta')){Copy-Item -LiteralPath (Join-Path $taskProject $relative) -Destination (Join-Path $taskSource $relative)}
foreach($file in Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets\JayACareer\Scripts') -Filter '*.meta' -File){$taskTarget=Join-Path $taskSource ('Assets\JayACareer\Scripts\'+$file.Name);if(-not (Test-Path -LiteralPath $taskTarget)){Copy-Item -LiteralPath $file.FullName -Destination $taskTarget}}
Copy-Item -LiteralPath (Join-Path $taskProject 'Deliverables\Windows Labeled 20261015') -Destination (Join-Path $taskSource 'Deliverables') -Recurse -Force
foreach($name in @('labeled-windows-build-20261015.json','labels-lighting-authoring.json')){Copy-Item -LiteralPath (Join-Path $taskProject ('Deliverables\Evidence\'+$name)) -Destination (Join-Path $taskSource 'Deliverables\Evidence')}
$taskCli=Get-Content -Raw -LiteralPath (Join-Path $taskSource 'Deliverables\Evidence\labeled-cli-provenance-20261015.json')|ConvertFrom-Json
@{fullBuildPipelineResult=$taskSummary.result;errors=$taskSummary.errors;warnings=$taskSummary.warnings;copyMethod='Complete fresh BuildPipeline output directory, no DLL transplant';cliWrapper=$taskCli.outcome;cliExitCode=$taskCli.exitCode;sourceLog=$taskLog}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskSource 'Deliverables\Evidence\labeled-copy-provenance.json') -Encoding UTF8
Write-Output 'Copied the completed full build and saved labeled scene; CLI outcome recorded.'
