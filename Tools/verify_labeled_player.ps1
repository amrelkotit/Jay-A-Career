$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskFolder=Join-Path $taskRoot 'Deliverables\Windows Labeled 20261015'
$taskExe=Join-Path $taskFolder 'Jay A Career.exe'
$taskEvidence=Join-Path $taskRoot 'Deliverables\Evidence'
$taskSummary=Get-Content -Raw -LiteralPath (Join-Path $taskEvidence 'labeled-windows-build-20261015.json') | ConvertFrom-Json
if($taskSummary.result -ne 'Succeeded' -or $taskSummary.errors -ne 0){throw 'The labeled full build is not successful.'}
$taskDll=Join-Path $taskFolder 'Jay A Career_Data\Managed\Assembly-CSharp.dll'
if(-not [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($taskDll)).Contains('JayLightingReview')){throw 'The player DLL is stale: the current opt-in review class is missing.'}
$taskInitializers=Get-Content -Raw -LiteralPath (Join-Path $taskFolder 'Jay A Career_Data\RuntimeInitializeOnLoads.json')|ConvertFrom-Json
if(-not ($taskInitializers.root|Where-Object {$_.className -eq 'JayLightingReview'})){throw 'The player startup metadata is stale.'}
@{dllSha256=(Get-FileHash -LiteralPath $taskDll).Hash;reviewClassPresent=$true;reviewStartupPresent=$true}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskEvidence 'labeled-player-script-inventory.json') -Encoding UTF8
$taskReview=Start-Process -FilePath $taskExe -ArgumentList @('-jay-lighting-review','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+(Join-Path $taskEvidence 'labeled-player-lighting-review.log')+'"')) -WindowStyle Normal -PassThru
if(-not $taskReview.WaitForExit(90000)){Stop-Process -Id $taskReview.Id -Force;throw 'Player visual review timed out.'}
$taskReviewPath=Join-Path $taskFolder 'Evidence\LabelsLighting'
$taskVisualResult=Get-Content -Raw -LiteralPath (Join-Path $taskReviewPath 'review-completed.json')|ConvertFrom-Json
if(-not $taskVisualResult.completed){throw 'Player visual review did not complete.'}
Copy-Item -LiteralPath $taskReviewPath -Destination $taskEvidence -Recurse -Force
Write-Output 'Six actual Windows player lighting/label captures completed.'
$taskValidation=Start-Process -FilePath $taskExe -ArgumentList @('-jay-validation','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+(Join-Path $taskEvidence 'labeled-player-input.log')+'"')) -WindowStyle Normal -PassThru
if(-not $taskValidation.WaitForExit(90000)){Stop-Process -Id $taskValidation.Id -Force;throw 'Player input checks timed out.'}
$taskInputFile=Join-Path $taskFolder 'Deliverables\Evidence\input-physics-checks.json'
$taskInput=Get-Content -Raw -LiteralPath $taskInputFile | ConvertFrom-Json
if(-not $taskInput.passed -or $taskInput.checks -ne 40){throw ('Player input checks failed: '+$taskInput.error)}
Copy-Item -LiteralPath $taskInputFile -Destination (Join-Path $taskEvidence 'windows-labeled-input-checks.json')
Write-Output 'Labeled player: all 40 automated checks passed.'
$taskBenchmark=Start-Process -FilePath $taskExe -ArgumentList @('-jay-benchmark','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+(Join-Path $taskEvidence 'labeled-player-benchmark.log')+'"')) -WindowStyle Normal -PassThru
Write-Output ('Visible benchmark started: '+$taskBenchmark.Id)
if(-not $taskBenchmark.WaitForExit(360000)){Stop-Process -Id $taskBenchmark.Id -Force;throw 'Two-day route timed out.'}
$taskRouteFile=Join-Path $taskFolder 'Evidence\Benchmark\route-report.json'
$taskRoute=Get-Content -Raw -LiteralPath $taskRouteFile | ConvertFrom-Json
if(-not $taskRoute.completed -or $taskRoute.days -ne 3 -or $taskRoute.cash -ne 150 -or $taskRoute.width -ne 1920 -or $taskRoute.height -ne 1080 -or $taskRoute.percentile1Fps -lt 30){throw ('Two-day route failed: '+$taskRoute.error)}
Copy-Item -LiteralPath $taskRouteFile -Destination (Join-Path $taskEvidence 'windows-labeled-route.json')
$taskRoute | Select-Object completed,days,cash,width,height,averageFps,percentile1Fps | ConvertTo-Json
