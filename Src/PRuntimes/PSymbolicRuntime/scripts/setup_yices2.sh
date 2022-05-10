#!/usr/bin/env bash

NPROC=$(nproc)
DEPSPATH="../../../../Bld/Deps"

mkdir -p $DEPSPATH

## Following dependencies might be needed
#sudo yum install readline-devel -y
#sudo yum install zlib-devel -y
#sudo yum install libcurl-devel -y
#sudo yum install gperf -y
#sudo yum install gmp-devel -y
#/usr/bin/python3 -m pip install toml

## install yices2
pushd .
cd $DEPSPATH
git clone https://github.com/SRI-CSL/yices2.git
cd yices2
autoconf
./configure
make -j${NPROC}
sudo make install
echo "export LD_LIBRARY_PATH="/usr/local/lib:\$LD_LIBRARY_PATH"" >> ~/.bash_profile
source ~/.bash_profile
popd

# install yices2_java_bindings
pushd .
cd $DEPSPATH
git clone https://github.com/aman-goel/yices2_java_bindings.git
cd yices2_java_bindings
./ant.sh
echo "export LD_LIBRARY_PATH="$PWD/dist/lib:\$LD_LIBRARY_PATH"" >> ~/.bash_profile
source ~/.bash_profile
popd
