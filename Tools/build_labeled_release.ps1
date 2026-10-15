$ErrorActionPreference='Stop'
$taskSource=Split-Path -Parent $PSScriptRoot
$taskProject='C:\Users\LENOVO\AppData\Local\Temp\JayCareerBuild20261013'
if(-not (Test-Path -LiteralPath (Join-Path $taskProject 'Library\ScriptAssemblies'))){throw 'The verified cached build copy is missing.'}
if(Get-CimInstance Win32_Process | Where-Object {$_.Name -eq 'Unity.exe' -and $_.CommandLine.Contains($taskProject)}){throw 'The cached project already has an open editor.'}
$env:BEE_BUILD_THREADS='1'
function Copy-ChangedLabelFile([string]$source,[string]$destination){if(-not (Test-Path -LiteralPath $destination) -or (Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $destination).Hash){Copy-Item -LiteralPath $source -Destination $destination}}
foreach($file in Get-ChildItem -LiteralPath (Join-Path $taskSource 'Assets\JayACareer\Scripts') -File){Copy-ChangedLabelFile $file.FullName (Join-Path $taskProject ('Assets\JayACareer\Scripts\'+$file.Name))}
foreach($name in @('ResponseFileFullBuild.cs','UsabilityLightingUpgrade.cs')){Copy-ChangedLabelFile (Join-Path $taskSource ('Assets\JayACareer\Editor\'+$name)) (Join-Path $taskProject ('Assets\JayACareer\Editor\'+$name))}
New-Item -ItemType Directory -Path (Join-Path $taskProject 'Assets\JayACareer\Textures\ObjectNotes') -Force | Out-Null
foreach($file in Get-ChildItem -LiteralPath (Join-Path $taskSource 'Assets\JayACareer\Textures\ObjectNotes') -File){Copy-ChangedLabelFile $file.FullName (Join-Path $taskProject ('Assets\JayACareer\Textures\ObjectNotes\'+$file.Name))}
Copy-Item -LiteralPath (Join-Path $taskSource 'Tools\BeeResponseLauncher.exe') -Destination (Join-Path $taskProject 'Tools')
$logPath=Join-Path $taskSource 'Deliverables\Evidence\labeled-full-build-20261015.log'
& unity build $taskProject --target StandaloneWindows64 --execute-method UsabilityLightingUpgrade.ApplyAndBuild --editor-path 'E:\Setups\Unity installs\6000.3.24f1\Editor\Unity.exe' --output-path (Join-Path $taskProject 'Deliverables\Windows Labeled 20261015\Jay A Career.exe') --log-file $logPath --timeout 900 --provenance-path (Join-Path $taskSource 'Deliverables\Evidence\labeled-cli-provenance-20261015.json') --format json --allow-dirty-build --no-tail
if($LASTEXITCODE -ne 0){
 $taskCompletedReport=Join-Path $taskProject 'Deliverables\Evidence\labeled-windows-build-20261015.json'
 if((Test-Path -LiteralPath $taskCompletedReport) -and ((Get-Content -Raw -LiteralPath $taskCompletedReport|ConvertFrom-Json).result -eq 'Succeeded') -and (Select-String -LiteralPath $logPath -Pattern 'Build Finished, Result: Success' -Quiet)){& (Join-Path $PSScriptRoot 'copy_labeled_output.ps1');exit 0}
 throw ('Unity labeled build failed with exit '+$LASTEXITCODE)
}
# Copy only the saved authoring changes and the new output back; earlier players remain recoverable.
Copy-Item -LiteralPath (Join-Path $taskProject 'Assets\JayACareer\Scenes\JayACareer_Realism.unity') -Destination (Join-Path $taskSource 'Assets\JayACareer\Scenes\JayACareer_Realism.unity')
foreach($relative in @('Assets\JayACareer\Textures\ObjectNotes','Assets\JayACareer\Materials\ObjectNotes','Assets\JayACareer\Prefabs\Realism')){
 $parent=Split-Path -Parent (Join-Path $taskSource $relative);Copy-Item -LiteralPath (Join-Path $taskProject $relative) -Destination $parent -Recurse -Force
}
foreach($relative in @('Assets\JayACareer\Textures\ObjectNotes.meta','Assets\JayACareer\Materials\ObjectNotes.meta','Assets\JayACareer\Editor\UsabilityLightingUpgrade.cs.meta')){if(Test-Path -LiteralPath (Join-Path $taskProject $relative)){Copy-Item -LiteralPath (Join-Path $taskProject $relative) -Destination (Join-Path $taskSource $relative)}}
foreach($file in Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets\JayACareer\Scripts') -Filter '*.meta' -File){$taskTarget=Join-Path $taskSource ('Assets\JayACareer\Scripts\'+$file.Name);if(-not (Test-Path -LiteralPath $taskTarget)){Copy-Item -LiteralPath $file.FullName -Destination $taskTarget}}
Copy-Item -LiteralPath (Join-Path $taskProject 'Deliverables\Windows Labeled 20261015') -Destination (Join-Path $taskSource 'Deliverables') -Recurse -Force
foreach($name in @('labeled-windows-build-20261015.json','labels-lighting-authoring.json')){Copy-Item -LiteralPath (Join-Path $taskProject ('Deliverables\Evidence\'+$name)) -Destination (Join-Path $taskSource 'Deliverables\Evidence')}
if(Test-Path -LiteralPath (Join-Path $taskProject 'Deliverables\Evidence\LabelsLighting')){Copy-Item -LiteralPath (Join-Path $taskProject 'Deliverables\Evidence\LabelsLighting') -Destination (Join-Path $taskSource 'Deliverables\Evidence') -Recurse -Force}
Write-Output 'Saved scene, labeled prefabs, daylight materials and fresh full Windows player copied back.'
