pushd .
echo "-------------------------"
echo "Building the P Compiler"
echo "-------------------------"

cd ../../../../Bld/
./build.sh

popd

cd ../..

cd PJavaRuntime

mvn install

cd ../PSymbolicRuntime

mvn clean

mvn install