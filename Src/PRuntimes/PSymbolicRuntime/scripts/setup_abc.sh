#!/usr/bin/env bash

NPROC=$(nproc)
DEPSPATH="../../../../Bld/Deps"

mkdir -p $DEPSPATH

## install abc
pushd .
cd $DEPSPATH
git clone https://github.com/berkeley-abc/abc.git
cd abc
ABC_USE_PIC=1 make -j${NPROC} libabc.a
popd

# install abc_java_bindings
pushd .
cd $DEPSPATH
git clone https://github.com/aman-goel/abc_java_bindings.git
cd abc_java_bindings
./ant.sh
echo "export LD_LIBRARY_PATH="$PWD/dist/lib:\$LD_LIBRARY_PATH"" >> ~/.bash_profile
source ~/.bash_profile
popd
