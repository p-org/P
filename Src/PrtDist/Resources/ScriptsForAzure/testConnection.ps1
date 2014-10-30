Write-Output "Testing Connection"
$servers = "plangdist1", "plangdist2", "plangdist3", "plangdist4"

foreach($s in $servers)
{
    if(!(Test-Connection -CN $s -BufferSize 16 -Count 1 -ea 0 -Quiet))
    {
        Write-Host -ForegroundColor Red "Problem Connecting to Machine $s, Please try turning on the Network Discovery Feature"
    }
    else
    {
        "Machine $s is Successfully Pinged"
    }
}