pushd $(dirname "${BASH_SOURCE[0]}")
cd ..

git submodule foreach --recursive git reset --hard
git submodule foreach --recursive git clean -fdx
git clean -fdx

git submodule update --init --recursive

dotnet build -c Release P.sln

popd

pushd .
mkdir -p build; cd build; cmake ../../Src; make
popd