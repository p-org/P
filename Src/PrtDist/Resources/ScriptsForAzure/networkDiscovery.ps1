 Write-Output "Enabling Network Discovery"
 netsh advfirewall firewall set rule group="File and Printer Sharing" new enable=Yes
 netsh advfirewall firewall set rule group="Network Discovery" new enable=Yes
 if($Error.Count -ne 0)
 {
    Write-Host -ForegroundColor Red "Error in enabling network discovery"
 }
 else
 {
    Write-Host -ForegroundColor Green "Success"
 }


