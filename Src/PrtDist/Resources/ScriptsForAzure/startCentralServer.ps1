

$psExec = "./PsExec.exe"
"Reading the Configuration File"

[xml] $xdox = Get-Content ".\PrtDistManConfiguration.xml"
$jobid = (Get-Content "job.txt")[0]
[string]$deploymentFolder = (Get-Content "job.txt")[1]
[string]$localFolder = $xdox.configuration.localFolder + $jobid

[string]$nn = $xdox.configuration.CentralServer
$startCentralServer = $psExec + " -d -u planguser -p Pldi2015 \\$nn $localFolder\PrtDistCentralServer.exe"
Invoke-Expression $startCentralServer




