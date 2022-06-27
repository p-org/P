pushd .
echo "-------------------------"
echo "Building the P Compiler"
echo "-------------------------"

cd ../../../../Bld/
./build.sh

popd

pushd .

cd ..

mvn clean

mvn install

popd