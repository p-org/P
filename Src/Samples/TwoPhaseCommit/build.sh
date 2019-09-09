#!/usr/bin/env bash

SCRIPT_DIR="$(dirname "${0}")"
PC_PATH="${SCRIPT_DIR}/../../../Bld/Drops/Release/Binaries/Pc.dll"

dotnet "${PC_PATH}" ./PSrc/Client.p ./PSrc/Coordinator.p ./PSrc/Participant.p ./PSrc/Events.p ./PSrc/Spec.p ./PSrc/TestDriver.p ./PSrc/Timer.p -generate:P# -t:Main

dotnet publish -f netcoreapp2.1 ./TwoPhaseCommit.csproj
