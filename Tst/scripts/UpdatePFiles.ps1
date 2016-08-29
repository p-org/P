param([string]$folder, [string]$string_1, [string]$string_2)


##########################
# Replacement functions
##########################

function ReplaceString()
{
    $file = $args[0]
    Write-Host "$file -- Done"
    (get-content $file) | foreach-object {$_ -replace $string_1, $string_2} | set-content $file

}

#current folder for restoring folder after executing the function
$currFolder = Get-Location

#set location
Set-Location $folder

$allPFiles = ls -Recurse *.p

foreach ($pfile in  $allPFiles)
{
    ReplaceString($pfile)
}

#restore folder
Set-Location $currFolder




