$taskSource='E:\El Gam3a\Semester 9\Video Game Design\Assignment\Jay A Career'
$taskProject='C:\Users\LENOVO\AppData\Local\Temp\JayCareerBuild20261013'
Copy-Item -LiteralPath (Join-Path $taskSource 'Assets\JayACareer\Editor\RealismBuildEntry.cs') -Destination (Join-Path $taskProject 'Assets\JayACareer\Editor\RealismBuildEntry.cs') -Force
$taskLog=Join-Path $taskProject 'menu-check.log';$taskStarted=Get-Date
$taskProcess=Start-Process -FilePath 'E:\Setups\Unity installs\6000.3.24f1\Editor\Unity.exe' -ArgumentList ('-batchmode -quit -refreshImportMode InProcess -job-worker-count 2 -projectPath "'+$taskProject+'" -executeMethod RealismReviewEntry.ValidateSaved -logFile "'+$taskLog+'"') -WorkingDirectory $taskProject -WindowStyle Hidden -PassThru
$taskStopped=$false
while(-not $taskProcess.WaitForExit(1000)){
 if(((Get-Date)-$taskStarted).TotalSeconds -gt 120){
  $taskAll=Get-CimInstance Win32_Process;$taskIds=@([int]$taskProcess.Id)
  do{$taskChildren=@($taskAll|Where-Object {$_.ParentProcessId -in $taskIds -and $_.ProcessId -notin $taskIds});$taskIds+=@($taskChildren|ForEach-Object {[int]$_.ProcessId})}while($taskChildren.Count -gt 0)
  foreach($taskId in $taskIds){Stop-Process -Id $taskId -ErrorAction SilentlyContinue};$taskStopped=$true;break
 }
}
$taskResult=@{sceneReopened=(-not $taskStopped -and (Select-String -LiteralPath $taskLog -Pattern 'Saved realism scene reopened successfully' -Quiet));menuSourceCompiled=(-not $taskStopped -and (Select-String -LiteralPath $taskLog -Pattern 'Saved realism scene reopened successfully' -Quiet));timedOut=$taskStopped;seconds=((Get-Date)-$taskStarted).TotalSeconds;log=$taskLog}
$taskResult|ConvertTo-Json|Set-Content -Encoding UTF8 -LiteralPath (Join-Path $taskSource 'Deliverables\Evidence\build-menu-check.json');$taskResult|ConvertTo-Json
