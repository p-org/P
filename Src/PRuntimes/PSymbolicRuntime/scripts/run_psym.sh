#!/usr/bin/env bash

projectPath=${1}
shift
projectName=${1}
shift
args=$@

outPath="output/${projectName}"
runPath=$(pwd)

echo -e "--------------------"
echo -e "Project Name: ${projectName}"
echo -e "Input Path  : ${projectPath}"
echo -e "Output Path : ${outPath}"

projectPath=$(realpath ${projectPath})
if [ -d "${outPath}" ]; then rm -Rf ${outPath}; fi
mkdir -p ${outPath}

echo -e "--------------------"
echo -e "Compiling P Model into Symbolic IR"
count=`ls -1 ${projectPath}/*.pproj 2>/dev/null | wc -l`
if [ $count != 0 ]
then
    echo -e "\tIgnoring .pproj File"
fi
inputFiles=$(find ${projectPath} -not -path "*/.*" -not -name ".*" -type f -name "*.p")
inputJavaFiles=$(find ${projectPath} -not -path "*/.*" -not -name ".*" -type f -name "*.java")
if [[ ! -z "$inputJavaFiles" ]]
then
    echo -e "\tFound Java Foreign Functions"
    cp ${inputJavaFiles} ${outPath}
fi

dotnet ../../../Bld/Drops/Release/Binaries/netcoreapp3.1/P.dll -generate:PSym ${inputFiles} -t:${projectName} -outputDir:${outPath} > ${outPath}/compile.out
if grep -q "Build succeeded." ${outPath}/compile.out; then
  echo -e "  Done"
else
  echo -e "  Compilation fail. Check  ${outPath}/compile.out for details"
  tail -50 ${outPath}/compile.out
  exit
fi

echo -e "--------------------"
echo -e "Running PSym"
cd ${outPath}
java -ea -jar -Xms12G target/${projectName}-jar-with-dependencies.jar \
    -p ${projectName} ${args} > >(tee -a run.out) 2>> >(tee -a run.err >&2)

cd ${runPath}

#mkdir -p ${outPath}/plots
#python3 scripts/psym_plots.py ${projectName} ${outPath}/run.out ${outPath}/plots
