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

cd ../SymbolicRuntime

mvn clean

mvn install