#!/usr/bin/env bash

NPROC=$(nproc)
DEPSPATH="../../../../Bld/Deps"

mkdir -p $DEPSPATH

## Following dependencies might be needed (if not installed already)
#sudo yum install readline-devel -y
#sudo yum install zlib-devel -y
#sudo yum install libcurl-devel -y
#sudo yum install gperf -y
#sudo yum install gmp-devel -y
#/usr/bin/python3 -m pip install toml

## Updating/upgrading maven might be needed
#sudo yum install maven -y
#wget https://dlcdn.apache.org/maven/maven-3/3.8.5/binaries/apache-maven-3.8.5-bin.tar.gz -P /tmp
#sudo tar xf /tmp/apache-maven-3.8.5-bin.tar.gz -C /opt
#sudo ln -s /opt/apache-maven-3.8.5 /opt/maven
#echo "export M2_HOME=/opt/maven" >> ~/.bash_profile
#echo "export M2=\${M2_HOME}/bin" >> ~/.bash_profile
#echo "export PATH=\${M2}:\${PATH}" >> ~/.bash_profile

## Updating/upgrading cmake might be needed
#pushd .
#wget https://github.com/Kitware/CMake/releases/download/v3.23.0/cmake-3.23.0.tar.gz
#tar xf cmake-3.23.0.tar.gz
#cd cmake-3.23.0
#./bootstrap --system-curl -- -DCMAKE_BUILD_TYPE:STRING=Release
#make -j$NPROC
#sudo make install
#popd

## install cvc5
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
