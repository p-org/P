#!/usr/bin/env python

import os
import os.path
import sys
import tools

def initGitModules():
    tools.progress("Updating the RV-Monitor submodules...")
    tools.runNoError(["git", "submodule", "update", "--init", "--recursive"])

def buildRvMonitor():
    tools.progress("Building RV-Monitor...")
    tools.runNoError(["mvn", "-B", "clean", "install", "-DskipTests"])

def build():
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
