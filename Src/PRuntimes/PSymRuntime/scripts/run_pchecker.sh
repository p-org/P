#!/usr/bin/env bash

projectPath=${1}
shift
projectName=${1}
shift
mode=${1}
shift
args=$@

PBIN="../../../Bld/Drops/Release/Binaries/net8.0/p.dll"
outPath="output/${projectName}"
runPath=$(pwd)

echo -e "--------------------"
echo -e "Project Name: ${projectName}"
echo -e "Input Path  : ${projectPath}"
echo -e "Output Path : ${outPath}"

PBIN=$(realpath ${PBIN})
projectPath=$(realpath ${projectPath})
outPath=$(realpath ${outPath})

if [ -d "${outPath}" ]; then rm -Rf ${outPath}; fi
mkdir -p ${outPath}

echo -e "--------------------"
echo -e "Compiling P Model for mode ${mode}"

cd ${projectPath}
dotnet ${PBIN} compile --mode ${mode} --projname ${projectName} --outdir ${outPath} > ${outPath}/compile.out
if grep -q "Build succeeded." ${outPath}/compile.out; then
  echo -e "  Done"
else
  echo -e "  Compilation fail. Check  ${outPath}/compile.out for details"
  tail -50 ${outPath}/compile.out
  exit
fi
cd -

echo -e "--------------------"
echo -e "Running PChecker in mode ${mode}"
cd ${outPath}
dotnet ${PBIN} check --mode ${mode} \
    ${args} > >(tee -a run.out) 2>> >(tee -a run.err >&2)
cd ${runPath}
