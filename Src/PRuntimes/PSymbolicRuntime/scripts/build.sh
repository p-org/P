pushd .
echo "-------------------"
echo "Building P Compiler"
echo "-------------------"
cd ../../../Bld/
./build.sh
popd

pushd .
echo "---------------------"
echo "Building PSym Runtime"
echo "---------------------"
mvn clean initialize
mvn install
popd
