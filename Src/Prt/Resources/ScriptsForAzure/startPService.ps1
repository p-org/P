

$psExec = "./PsExec.exe"
"Reading the Configuration File"

[xml] $xdox = Get-Content ".\PrtDMConfiguration.xml"
$nodes = $xdox.configuration.AllNodes.ChildNodes
$jobid = (Get-Content "job.txt")[0]
[string]$deploymentFolder = (Get-Content "job.txt")[1]
[string]$localFolder = $xdox.configuration.PServiceFolder
foreach($n in $xdox.configuration.AllNodes.ChildNodes)
{
     [string]$nn = $n.InnerText
     $command = $psExec + " -d -u planguser -p Pldi2015 \\$nn Robocopy $deploymentFolder $localFolder /E /PURGE"
     Invoke-Expression $command | Add-Content "StartPServiceLog.txt"
     Write-host -ForegroundColor Green "Pushed Pservice on $nn"
     $startservice = $psExec + " -d -u planguser -p Pldi2015 \\$nn $localFolder\PrtDService.exe"
     Invoke-Expression $startservice | Add-Content "StartPServiceLog.txt"
}



