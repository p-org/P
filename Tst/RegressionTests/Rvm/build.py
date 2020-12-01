#!/usr/bin/env python

import os
import os.path
import tools

def initGitModules():
    """
    Runs "git submodule update --init --recursive".

    Raises:
        SubprocessError if the git command returns an error code.
    """
    tools.progress("Updating the RV-Monitor submodules...")
    tools.runNoError(["git", "submodule", "update", "--init", "--recursive"])

def buildRvMonitor():
    """
    Builds and installs the RV-Monitor tool in the local Maven repository.

    Raises:
        SubprocessError if the mvn command returns an error code.
    """
    tools.progress("Building RV-Monitor...")
    tools.runNoError(["mvn", "-B", "clean", "install", "-DskipTests"])

def build():
    """
    Initializes the RV-Monitor submodule and builds the tool.

    Raises:
        SubprocessError if one of the shell subcommands (git and mvn)
            returns an error.
    """
    script_dir = os.path.dirname(os.path.abspath(__file__))

    ext_dir = os.path.join(script_dir, "ext")
    if not os.path.exists(ext_dir):
        os.makedirs(ext_dir)
    tools.runInDirectory(ext_dir, initGitModules)

    rv_monitor_dir = os.path.join(ext_dir, "rv-monitor")
    if not os.path.exists(os.path.join(rv_monitor_dir, "target", "release", "rv-monitor", "lib", "rv-monitor.jar")):
        tools.runInDirectory(rv_monitor_dir, buildRvMonitor)

def main():
    build()

if __name__ == "__main__":
    main()
