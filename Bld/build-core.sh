pushd $(dirname "${BASH_SOURCE[0]}")
cd ..

dotnet build -c Release P.sln

popd

pushd .
mkdir -p build; cd build; cmake ../../Src; make
popd
