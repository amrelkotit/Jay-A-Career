$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskFolder=Join-Path $taskRoot 'Deliverables\Windows Labeled 20261015'
$taskVideo=Join-Path $taskRoot 'Deliverables\Jay A Career - Demonstration.mp4'
$taskBackup=Join-Path (Split-Path -Parent $taskRoot) 'Jay A Career Backups\Demonstration before labels 20261015.mp4'
if(-not (Test-Path -LiteralPath $taskBackup)){Copy-Item -LiteralPath $taskVideo -Destination $taskBackup}
$taskPlayer=Start-Process -FilePath (Join-Path $taskFolder 'Jay A Career.exe') -ArgumentList @('-jay-demo','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+(Join-Path $taskRoot 'Deliverables\Evidence\labeled-demo-player.log')+'"')) -WindowStyle Normal -PassThru
Write-Output ('Recording updated scripted player demonstration: '+$taskPlayer.Id)
if(-not $taskPlayer.WaitForExit(360000)){Stop-Process -Id $taskPlayer.Id -Force;throw 'Updated demonstration timed out.'}
& 'C:\Users\LENOVO\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' (Join-Path $PSScriptRoot 'finish_evidence.py') --labeled --encode --contacts
if($LASTEXITCODE -ne 0){throw 'Demo encoding or route verification failed.'}
