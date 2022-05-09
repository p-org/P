#!/usr/bin/env bash

NPROC=$(nproc)
DEPSPATH="../../../../Bld/deps"

# install cvc5
pushd .
cd $DEPSPATH
git clone https://github.com/cvc5/cvc5.git
cd cvc5
git checkout 6c0a7c8e3457a2fa54f7b8a592adcad3be1ee5d3
./configure.sh production --java-bindings --auto-download --prefix=build/install
cd build
make -j${NPROC}
sudo make install
echo "export LD_LIBRARY_PATH="$PWD/install/lib64:\$LD_LIBRARY_PATH"" >> ~/.bash_profile
source ~/.bash_profile
popd
