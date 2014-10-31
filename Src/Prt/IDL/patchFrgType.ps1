echo "Patch the PrtTypes_IDl.h"
$fileContent = Get-Content ".\PrtTypes_IDL.h"
$result = $fileContent -replace "typedef struct PRT_FORGNTYPE", "/*typedef struct PRT_FORGNTYPE"
$result = $result -replace "PRT_FORGNTYPE;", "PRT_FORGNTYPE;*/"
Out-File -FilePath ".\PrtTypes_IDL.h" -inputobject $result
