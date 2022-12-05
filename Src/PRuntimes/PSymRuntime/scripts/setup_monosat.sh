#!/usr/bin/env bash

NPROC=$(nproc)
DEPSPATH="../../../../Bld/Deps"

mkdir -p $DEPSPATH

## install monosat
pushd .
cd $DEPSPATH
git clone https://github.com/sambayless/monosat.git
cd monosat
cmake -DJAVA=ON .
make -j${NPROC}
echo "export LD_LIBRARY_PATH="$PWD:\$LD_LIBRARY_PATH"" >> ~/.bash_profile
source ~/.bash_profile
popd
