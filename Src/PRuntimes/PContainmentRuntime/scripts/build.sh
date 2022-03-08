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

cd ../PContainmentRuntime

mvn clean

mvn install
