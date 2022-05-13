#!/usr/bin/env bash

NPROC=$(nproc)
DEPSPATH="../../../../Bld/Deps"

mkdir -p $DEPSPATH

## install z3
pushd .
cd $DEPSPATH
git clone https://github.com/Z3Prover/z3.git
cd z3
git checkout df8f9d7dcb8b9f9b3de1072017b7c2b7f63f0af8
python scripts/mk_make.py --java
cd build
make -j${NPROC}
sudo make install
echo "export LD_LIBRARY_PATH="$PWD:\$LD_LIBRARY_PATH"" >> ~/.bash_profile
source ~/.bash_profile
popd
