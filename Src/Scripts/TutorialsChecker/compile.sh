#!/bin/bash

cd $1

# Get list of subfolders
folders=$(ls -d */)

# Loop through each subfolder
for folder in $folders; do
  # Check if folder contains a .pproj file
  pprojFiles=$(ls $folder/*.pproj 2> /dev/null)

  # If so, change into folder and compile
  if [ -n "$pprojFiles" ]; then
    cd $folder
    rm -rf PGenerated

    echo "------------------------------------------------------"
    echo "Compiling $folder!"
    echo "------------------------------------------------------"

    p compile

    # Check and print any errors
    if [ $? -ne 0 ]; then
      echo "Error compiling $folder"
      exit 1
    fi

    cd ..
  else
    echo "------------------------------------------------------"
    echo "Skipping $folder as it does not contain a .pproj file!"
    echo "------------------------------------------------------"
  fi
done

exit 0