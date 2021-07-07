@echo off

set SRC_ROOT=%~dp0..

java -cp "%SRC_ROOT%\lib\logicrepository.jar;%SRC_ROOT%\lib\plugins\*.jar;%SRC_ROOT%\lib\external\mysql-connector-java-3.0.9-stable-bin.jar" logicrepository.Main %*


