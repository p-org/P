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
echo -e "Compiling P Model into PSym IR"
count=`ls -1 ${projectPath}/*.pproj 2>/dev/null | wc -l`
if [ $count != 0 ]
then
    echo -e "  Ignoring .pproj File"
fi
inputFiles=$(find ${projectPath} -not -path "*/.*" -not -name ".*" -type f -name "*.p")
inputJavaFiles=$(find ${projectPath} -not -path "*/.*" -not -name ".*" -type f -name "*.java")
if [[ ! -z "${inputJavaFiles}" ]]
then
    echo -e "  Found Java Foreign Functions"
    cp ${inputJavaFiles} ${outPath}
fi
configFile="${projectPath}/psym-config.json"
if test -f "${configFile}"; then
    echo -e "  Found PSym config file"
    cp ${configFile} ${outPath}
fi

pcl -generate:PSym ${inputFiles} -t:${projectName} -outputDir:${outPath} > ${outPath}/compile.out
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
java -jar -Xms64G target/${projectName}-jar-with-dependencies.jar -tl ${timeMax} -ml ${memMax} \
    -p ${projectName} ${args} > >(tee -a run.out) 2>> >(tee -a run.err >&2)
cd -

mkdir -p stats
python3 Scripts/psym_compile_results.py --tool ${toolName} ${outPath}/output/stats-${projectName}.log
