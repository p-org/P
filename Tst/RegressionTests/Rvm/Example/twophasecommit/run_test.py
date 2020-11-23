#!/usr/bin/env python

import os
import os.path
import subprocess
import sys

if __name__ == "__main__":
    sys.path.append(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

import tools

def runTwoPhaseCommit():
    tools.progress("Running the twophasecommit tests...")
    tools.runNoError(["mvn", "clean", "test", "-Dtest=twophasecommit.PrepareSuccessTest"])
    tools.runNoError(["mvn", "clean", "test", "-Dtest=twophasecommit.PrepareFailureTest"])
    tools.runNoError(["mvn", "clean", "test", "-Dtest=twophasecommit.BuggyPrepareFailureTest"])
    tools.runNoError(["mvn", "clean"])

def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    tools.runInDirectory(script_dir, runTwoPhaseCommit)

if __name__ == "__main__":
    main()
