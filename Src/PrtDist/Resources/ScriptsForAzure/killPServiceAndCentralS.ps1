

$psKill = "./pskill.exe"
"Reading the Configuration File"

[xml] $xdox = Get-Content ".\PrtDistManConfiguration.xml"
$jobid = (Get-Content "job.txt")[0]
[string]$deploymentFolder = (Get-Content "job.txt")[1]
foreach($n in $xdox.configuration.AllNodes.ChildNodes)
{
     [string]$nn = $n.InnerText
     $command = $psKill + " -t -u planguser -p Pldi2015 \\$nn PrtDistService.exe"
     Invoke-Expression $command
     Write-host -ForegroundColor Green "Killed PrtDistservice on $nn"
}

[string]$centralserver = $xdox.configuration.CentralServer
$command = $psKill + " -t -u planguser -p Pldi2015 \\plangdist$centralserver PrtDistCentralServer.exe"
Invoke-Expression $command
Write-host -ForegroundColor Green "Killed PrtDistCentralServer on plangdist$centralserver"

