# Install Instructions for Amazon Linux

## Get Java 11
```sh
sudo rpm --import https://yum.corretto.aws/corretto.key
sudo curl -L-o /etc/yum.repos.d/corretto.repo https://yum.corretto.aws/corretto.repo 
sudo yum install java-11-amazon-corretto-devel
```

## Get SBT
```sh
sudo rm -f /etc/yum.repos.d/bintray-rpm.repo || true
curl -L https://www.scala-sbt.org/sbt-rpm.repo > sbt-rpm.repo
sudo mv sbt-rpm.repo /etc/yum.repos.d/
sudo yum install sbt
```

## Get Z3
```sh
cd ~
git clone https://github.com/Z3Prover/z3.git
cd z3
python scripts/mk_make.py --java 
cd build; make
```
Then add `export PATH=$PATH:$HOME/z3/build/` and `export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$HOME/z3/build/` to your .zshrc and run 
`source ~/.zshrc`.

## Get UCLID5
```sh
cd ~
git clone https://github.com/uclid-org/uclid.git
cd uclid
sbt update clean compile "set fork:=true" test # should fail some tests that use cvc5 and delphi 
sbt universal:packageBin
unzip target/universal/uclid-0.9.5.zip
```
Then add `export PATH=$PATH:$HOME/uclid/uclid-0.9.5/bin/` to your .zshrc and run `source ~/.zshrc`.

## Get PVerifier
```sh
cd ~
git clone https://github.com/FedericoAureliano/P.git
cd P
# follow regular P install instructions
root=$(pwd)
cd $root/Bld
./build.sh
dotnet tool uninstall --global P
cd $root/Src/PCompiler/PCommandLine
dotnet pack PCommandLine.csproj --configuration Release --output ./publish -p:PackAsTool=true -p:ToolCommandName=P -p:Version=2.1.3
dotnet tool install P --global --add-source ./publish
```