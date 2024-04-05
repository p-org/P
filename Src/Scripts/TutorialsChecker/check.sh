#!/bin/bash

SCHEDULES=25000

cd $1

# Get list of subfolders
folders=$(ls -d */)
errorCount=0

# Loop through each subfolder
for folder in $folders; do
  # Check if folder contains a .pproj file
  pprojFiles=$(ls $folder/*.pproj 2> /dev/null)

  # If so, change into folder and compile
  if [ -n "$pprojFiles" ]; then
    cd $folder

    echo "------------------------------------------------------"
    echo "Checking $folder!"
    echo "------------------------------------------------------"

    checkLog="check.log"
    p check -i ${SCHEDULES} 2>&1 | tee ${checkLog}
    if grep -q "Possible options are:" ${checkLog}; then
      beginFlag=false
      while IFS=" " read firstWord _; do
        if  [[ "${beginFlag}" = false ]] && [[ ${firstWord} == "Possible" ]]; then
          beginFlag=true
        elif [[ "${beginFlag}" = true ]] && [[ ${firstWord} ]]; then
          if [[ "${firstWord}" = "~~" ]]; then
            break;
          fi
          echo "Smoke testing for test case ${firstWord}";
          p check -i ${SCHEDULES} -tc ${firstWord}
          if [ $? -ne 0 ]; then
            let "errorCount=errorCount + 1"
          fi
        fi
      done < ${checkLog}
    elif grep -q "Cannot detect a P test case" ${checkLog}; then
      echo "Skipping smoke testing as no test case found";
    fi

    cd ..
  else
    echo "------------------------------------------------------"
    echo "Skipping $folder as it does not contain a .pproj file!"
    echo "------------------------------------------------------"
  fi
done

echo "Error Count:" $errorCount

if [[ "$errorCount" = 3 ]]; then
  exit 0
else
  exit 1
fi
