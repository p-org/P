cd %~dp0
cd ..
msbuild p.sln /p:Configuration=Debug /p:Platform=x86 
msbuild p.sln /p:Configuration=Release /p:Platform=x86
msbuild p.sln /p:Configuration=Debug /p:Platform=x64 
msbuild p.sln /p:Configuration=Release /p:Platform=x64 