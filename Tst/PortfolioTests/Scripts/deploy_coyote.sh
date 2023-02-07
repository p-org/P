#!/usr/bin/env bash

toolName=${1}
shift
projectPath=${1}
shift
projectName=${1}
shift
timeMax=${1}
shift
memMax=${1}
shift
args=$@

shopt -s expand_aliases
source ~/.bash_profile

outPath="runs/${toolName}/${projectName}"

echo -e "--------------------"
echo -e "Project Name: ${projectName}"
echo -e "Input Path  : ${projectPath}"
echo -e "Output Path : ${outPath}"

projectPath=$(realpath ${projectPath})
if [ -d "${outPath}" ]; then rm -Rf ${outPath}; fi
mkdir -p ${outPath}

echo -e "--------------------"
echo -e "Compiling P Model into C#"
count=`ls -1 ${projectPath}/*.pproj 2>/dev/null | wc -l`
if [ $count != 0 ]
then
    echo -e "  Ignoring .pproj File"
fi
inputFiles=$(find ${projectPath} -not -path "*/.*" -not -name ".*" -type f -name "*.p")
inputCSharpFiles=""
if [ -d "${projectPath}/PForeign" ]; then
  inputCSharpFiles=$(find ${projectPath}/PForeign -not -path "*/.*" -not -name ".*" -type f -name "*.cs")
fi
if [[ ! -z "$inputCSharpFiles" ]]
then
    echo -e "  Found CSharp Foreign Functions"
    cp ${inputCSharpFiles} ${outPath}
fi

pcl ${inputFiles} -outputDir:${outPath} > ${outPath}/compile.out
if grep -q "Build succeeded." ${outPath}/compile.out; then
  if grep -q ".dll" ${outPath}/compile.out; then
    dllFile=$(grep ".dll" ${outPath}/compile.out | awk 'NF>1{print $NF}')
    echo -e "  Done"
  else
    echo -e "  .dll file not found. Check  ${outPath}/compile.out for details"
    tail -50 ${outPath}/compile.out
    exit
  fi
else
  echo -e "  Compilation fail. Check  ${outPath}/compile.out for details"
  tail -50 ${outPath}/compile.out
  exit
fi

echo -e "--------------------"
echo -e "Running PMC"
cd ${outPath}
pmc ${dllFile} -t ${timeMax} ${args} > >(tee -a run.out) 2>> >(tee -a run.err >&2)
cd -

mkdir -p ${outPath}/output
python3 Scripts/coyote_stats.py ${projectName} ${outPath}/run.out > ${outPath}/output/stats-${projectName}.log

mkdir -p stats
python3 Scripts/coyote_compile_results.py --tool ${toolName} ${outPath}/output/stats-${projectName}.log
