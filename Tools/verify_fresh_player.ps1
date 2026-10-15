$ErrorActionPreference='Stop'
$rootPath='E:\El Gam3a\Semester 9\Video Game Design\Assignment\Jay A Career'
$folder=Join-Path $rootPath 'Deliverables\Windows Fresh 20261015'
$exe=Join-Path $folder 'Jay A Career.exe'
$evidence=Join-Path $rootPath 'Deliverables\Evidence'
$summary=Get-Content -Raw -LiteralPath (Join-Path $evidence 'fresh-windows-build-20261015.json') | ConvertFrom-Json
if($summary.result -ne 'Succeeded' -or $summary.errors -ne 0){throw 'Fresh full build is not successful.'}
$validation=Start-Process -FilePath $exe -ArgumentList @('-jay-validation','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+(Join-Path $evidence 'fresh-player-input-20261015.log')+'"')) -WindowStyle Normal -PassThru
if(-not $validation.WaitForExit(90000)){Stop-Process -Id $validation.Id -Force;throw 'Fresh player input checks timed out.'}
$inputFile=Join-Path $folder 'Deliverables\Evidence\input-physics-checks.json'
$inputResult=Get-Content -Raw -LiteralPath $inputFile | ConvertFrom-Json
if(-not $inputResult.passed -or $inputResult.checks -ne 40){throw ('Fresh player input checks failed: '+$inputResult.error)}
Copy-Item -LiteralPath $inputFile -Destination (Join-Path $evidence 'windows-fresh-input-checks.json')
Write-Output 'Fresh full-build player: 40 input/physics checks passed.'
$benchmark=Start-Process -FilePath $exe -ArgumentList @('-jay-benchmark','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+(Join-Path $evidence 'fresh-player-benchmark-20261015.log')+'"')) -WindowStyle Normal -PassThru
Write-Output ('Visible two-day benchmark started: '+$benchmark.Id)
if(-not $benchmark.WaitForExit(360000)){Stop-Process -Id $benchmark.Id -Force;throw 'Fresh player two-day route timed out.'}
$routeFile=Join-Path $folder 'Evidence\Benchmark\route-report.json'
$route=Get-Content -Raw -LiteralPath $routeFile | ConvertFrom-Json
if(-not $route.completed -or $route.days -ne 3 -or $route.cash -ne 150 -or $route.width -ne 1920 -or $route.height -ne 1080){throw ('Fresh player route failed: '+$route.error)}
Copy-Item -LiteralPath $routeFile -Destination (Join-Path $evidence 'windows-fresh-route.json')
$route | Select-Object completed,days,cash,width,height,averageFps,percentile1Fps | ConvertTo-Json
