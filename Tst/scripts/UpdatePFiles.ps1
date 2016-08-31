param([string]$folder, [string]$string_1, [string]$string_2)


##########################
# Replacement functions
##########################

function ReplaceString()
{
    foreach ($pfile in  $allPFiles)
    {
        Write-Host "$pfile -- Done"
        (get-content $pfile) | foreach-object {$_ -replace $string_1, $string_2} | set-content $pfile
    }

}

function FindAndReplaceModelWithMachine()
{

    $modelfiles = (Select-String -Path $allPFiles -Pattern "model [a-z]* {").Path
    $modelfiles
    foreach ($pfile in  $modelfiles)
    {
        (get-content $pfile) | foreach-object {$_ -replace "model ", "machine "} | set-content $pfile
    }
    Write-Host "Changing contents"
    $modelfiles = (Select-String -Path $allPFiles -Pattern "model [a-z]* {").Path
    $modelfiles
    
}

### Script starting point

#current folder for restoring folder after executing the function
$currFolder = Get-Location

#set location
Set-Location $folder
$allPFiles = ls -Recurse *.p

#FindAndReplaceModelWithMachine($allPFiles)

ReplaceString($allPFiles)

#restore folder
Set-Location $currFolder




