$tempRoot='C:\Users\LENOVO\AppData\Local\Temp\JayCareerBuild20261013'
$snapshot=Get-CimInstance Win32_Process
$targets=@($snapshot | Where-Object {$_.Name -in @('cmd.exe','netcorerun.exe') -and $_.CommandLine -match 'BuildPlayerDataGenerator.exe' -and $_.CommandLine.Contains($tempRoot)})
$ids=[Collections.Generic.List[int]]::new();foreach($target in $targets){$ids.Add([int]$target.ProcessId)}
for($i=0;$i -lt $ids.Count;$i++){foreach($child in $snapshot | Where-Object ParentProcessId -eq $ids[$i]){if(-not $ids.Contains([int]$child.ProcessId)){$ids.Add([int]$child.ProcessId)}}}
foreach($taskId in $ids){Stop-Process -Id $taskId -Force -ErrorAction SilentlyContinue}
Write-Output ('Stopped '+$ids.Count+' stalled temporary-build processes.')
