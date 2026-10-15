$ErrorActionPreference='Stop'
$taskSource=Split-Path -Parent $PSScriptRoot
$taskProject='C:\Users\LENOVO\AppData\Local\Temp\JayCareerBuild20261013'
if(Get-CimInstance Win32_Process | Where-Object {$_.Name -eq 'Unity.exe' -and $_.CommandLine.Contains($taskProject)}){throw 'Wait for the build editor to finish.'}
$taskReviewSource=Join-Path $taskSource 'Assets\JayACareer\Editor\UsabilityLightingUpgrade.cs';$taskReviewTarget=Join-Path $taskProject 'Assets\JayACareer\Editor\UsabilityLightingUpgrade.cs'
if((Get-FileHash -LiteralPath $taskReviewSource).Hash -ne (Get-FileHash -LiteralPath $taskReviewTarget).Hash){Copy-Item -LiteralPath $taskReviewSource -Destination $taskReviewTarget}
& unity run $taskProject --editor-path 'E:\Setups\Unity installs\6000.3.24f1\Editor\Unity.exe' --timeout 240 --log-file (Join-Path $taskSource 'Deliverables\Evidence\labels-graphics-review.log') --no-tail --format json -- -executeMethod UsabilityLightingUpgrade.ReviewOnly -force-d3d11
if($LASTEXITCODE -ne 0){throw 'Graphics lighting review failed.'}
Copy-Item -LiteralPath (Join-Path $taskProject 'Deliverables\Evidence\LabelsLighting') -Destination (Join-Path $taskSource 'Deliverables\Evidence') -Recurse -Force
