$psExec = "./PsExec.exe"
"Reading the Configuration File"

[xml] $xdox = Get-Content ".\PrtDMConfiguration.xml"
$nodes = $xdox.configuration.AllNodes.ChildNodes
foreach($n in $nodes)
{
    $psExec -u planguser -p Pldi2015 $n Robocopy $xdox.configuration.DeploymentFolder $xdox.configuration.PServiceFolder /E | Out-File "./startPserviceLog.txt"
}



