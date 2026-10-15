$taskProject='E:\El Gam3a\JayCareerFinal20261013'
$taskLog=Join-Path $taskProject 'final-windows-build.log'
if ((Get-Date)-(Get-Item -LiteralPath $taskLog).LastWriteTime -lt [TimeSpan]::FromMinutes(5)) {throw 'Build log recently changed; leaving build running'}
$taskProcesses=Get-CimInstance Win32_Process
$taskRoots=@($taskProcesses | Where-Object {$_.Name -eq 'Unity.exe' -and $_.CommandLine -like ('*"'+$taskProject+'"*')})
$taskIds=@($taskRoots | ForEach-Object {[int]$_.ProcessId})
do {$taskChildren=@($taskProcesses | Where-Object {$_.ParentProcessId -in $taskIds -and $_.ProcessId -notin $taskIds});$taskIds+=@($taskChildren | ForEach-Object {[int]$_.ProcessId})}while($taskChildren.Count -gt 0)
$taskOutput='E:\El Gam3a\Semester 9\Video Game Design\Assignment\Jay A Career\Deliverables\Evidence\final-source-build-status.json'
@{status='Stalled; stopped after over five minutes without build-log progress';stage='ScriptAssembliesAndTypeDB / BuildPlayerDataGenerator';project=$taskProject;log=$taskLog;stoppedProcessIds=$taskIds;exactFinalSourceBuildCertified=$false;earlierCleanBuildSucceeded=$true} | ConvertTo-Json | Set-Content -Encoding UTF8 -LiteralPath $taskOutput
foreach($taskId in $taskIds){Stop-Process -Id $taskId -ErrorAction SilentlyContinue}
Write-Output ('Stopped '+$taskIds.Count+' processes belonging to the stalled final build')
