$taskProject='E:\El Gam3a\JayCareerPackagedReview20261013'
$taskLog=Join-Path $taskProject 'packaged-open.log'
$taskStarted=Get-Date
$taskProcess=Start-Process -FilePath 'E:\Setups\Unity installs\6000.3.24f1\Editor\Unity.exe' -ArgumentList ('-batchmode -quit -projectPath "'+$taskProject+'" -executeMethod RealismReviewEntry.ValidateSaved -logFile "'+$taskLog+'"') -WorkingDirectory $taskProject -WindowStyle Hidden -PassThru
$taskStopped=$false
while(-not $taskProcess.WaitForExit(1000)){
 if(((Get-Date)-$taskStarted).TotalSeconds -gt 300){
  $taskAll=Get-CimInstance Win32_Process;$taskIds=@([int]$taskProcess.Id)
  do{$taskChildren=@($taskAll|Where-Object {$_.ParentProcessId -in $taskIds -and $_.ProcessId -notin $taskIds});$taskIds+=@($taskChildren|ForEach-Object {[int]$_.ProcessId})}while($taskChildren.Count -gt 0)
  foreach($taskId in $taskIds){Stop-Process -Id $taskId -ErrorAction SilentlyContinue}
  $taskStopped=$true;break
 }
}
$taskOpened=(-not $taskStopped -and (Select-String -LiteralPath $taskLog -Pattern 'Saved realism scene reopened successfully' -Quiet))
$taskResult=@{project=$taskProject;log=$taskLog;seconds=((Get-Date)-$taskStarted).TotalSeconds;timeLimitReached=$taskStopped;sceneReopenedInUnity=$taskOpened}
$taskResult|ConvertTo-Json|Set-Content -Encoding UTF8 -LiteralPath 'E:\El Gam3a\Semester 9\Video Game Design\Assignment\Jay A Career\Deliverables\Evidence\packaged-project-reopen.json'
$taskResult|ConvertTo-Json
