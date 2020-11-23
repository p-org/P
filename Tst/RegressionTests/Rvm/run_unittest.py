#!/usr/bin/env python

import glob
import os
import os.path
import re
import shutil
import sys
import tempfile
import tools

def readFile(name):
    with open(name, "rt") as f:
        return ''.join(f)

def writeFile(name, contents):
    with open(name, "wt") as f:
        f.write(contents)

def runPc(root_dir, arguments):
    tools.runNoError(
        ["dotnet", os.path.join(root_dir, "Bld", "Drops", "Release", "Binaries", "Pc.dll")]
        + arguments
    )

def translate(script_dir, root_dir, gen_monitor_setup_dir):
    tools.progress("Run the PCompiler...")
    runPc(root_dir, [os.path.join(script_dir, "spec.p"), "-g:RVM", "-o:%s" % gen_monitor_setup_dir])

def fillAspect(aspectj_setup_dir, monitor_setup_dir, gen_monitor_setup_dir):
    tools.progress("Fill in AspectJ template")
    aspect_file_name = "unittestMonitorAspect.aj"
    aspect_file_path = os.path.join(gen_monitor_setup_dir, aspect_file_name)
    aspectContent = readFile(aspect_file_path)
    aspectContent = aspectContent.replace(
        "// add your own imports.",
        readFile(os.path.join(monitor_setup_dir, "import.txt")))
    aspectContent = aspectContent.replace(
        "// Implement your code here.",
        readFile(os.path.join(monitor_setup_dir, "ajcode.txt")))
    writeFile(os.path.join(aspectj_setup_dir, aspect_file_name), aspectContent)

def addRvmExceptions(rvm_file_path):
    rvm = re.sub(
        r"([ ]*)private void ([a-zA-Z_0-9]+)_getState\(\) throws GotoStmtException, RaiseStmtException \{",
        r"""\1private void \2_getState() throws GotoStmtException, RaiseStmtException, StateNameException {
\1    throw new StateNameException(state.getName());""",
        readFile(rvm_file_path)
    )
    writeFile(rvm_file_path, rvm)

def createRvm(rvmonitor_bin, gen_monitor_setup_dir, java_dir):
    tools.progress("Run RVMonitor")
    monitor_binary = os.path.join(rvmonitor_bin, "rv-monitor")
    rvm_file = os.path.join(gen_monitor_setup_dir, "unittest.rvm")
    addRvmExceptions(rvm_file)
    tools.runNoError([monitor_binary, "-merge", "-d", java_dir, rvm_file])

def setupTests(test_dir, script_dir, framework_dir, setup_dir, test_name):
    tools.progress("Setup for test %s..." % test_name)
    if not os.path.exists(setup_dir):
        os.makedirs(setup_dir)

    shutil.copy(os.path.join(framework_dir, "pom.xml"), setup_dir)

    src_setup_dir = os.path.join(setup_dir, "src")
    if os.path.exists(src_setup_dir):
        shutil.rmtree(src_setup_dir)
    shutil.copytree(os.path.join(framework_dir, "src"), src_setup_dir)

    test_setup_dir = os.path.join(src_setup_dir, "test", "java", "unittest")
    if not os.path.exists(test_setup_dir):
        os.makedirs(test_setup_dir)
    for f in glob.glob(os.path.join(test_dir, "*.java")):
        shutil.copy(f, test_setup_dir)

    shutil.copy(os.path.join(test_dir, "spec.p"), setup_dir)

    monitor_setup_dir = os.path.join(setup_dir, "monitor")
    if not os.path.exists(monitor_setup_dir):
        os.makedirs(monitor_setup_dir)
    for f in glob.glob(os.path.join(framework_dir, "monitor", "*.txt")):
        shutil.copy(f, monitor_setup_dir)

    root_dir = os.path.dirname(os.path.dirname(os.path.dirname(script_dir)))

    gen_monitor_setup_dir = os.path.join(monitor_setup_dir, "generated")
    if not os.path.exists(gen_monitor_setup_dir):
        os.makedirs(gen_monitor_setup_dir)
    translate(test_dir, root_dir, gen_monitor_setup_dir)

    mop_setup_dir = os.path.join(src_setup_dir, "main", "java", "mop")
    aspectj_setup_dir = os.path.join(mop_setup_dir, "aspectJ")
    if not os.path.exists(aspectj_setup_dir):
        os.makedirs(aspectj_setup_dir)
    fillAspect(aspectj_setup_dir, monitor_setup_dir, gen_monitor_setup_dir)

    rvmonitor_bin = os.path.join(script_dir, "ext", "rv-monitor", "target", "release", "rv-monitor", "bin")
    createRvm(rvmonitor_bin, gen_monitor_setup_dir, mop_setup_dir)

    for f in glob.glob(os.path.join(gen_monitor_setup_dir, "*.java")):
        shutil.copy(f, mop_setup_dir)

def runTests(test_name):
    tools.progress("Running the %s test(s)..." % test_name)
    tools.runNoError(["mvn", "-B", "clean", "test"])
    tools.runNoError(["mvn", "-B", "clean"])

def usageError():
    raise Exception(
        "Expected exactly one or two command line arguments: "
        "the test name and, perhaps, the temporary directory.\n"
        "Usage: run_unittest.py test_name [temp_dir]"
    )

def main(argv):
    if len(argv) == 0:
        usageError()
    test_name = argv[0]
    script_dir = os.path.dirname(os.path.abspath(__file__))
    unittest_dir = os.path.join(script_dir, "Unit")
    framework_dir = os.path.join(unittest_dir, "Framework")
    test_dir = os.path.join(unittest_dir, "Test", test_name)

    if len(argv) == 1:
        setup_dir = tempfile.mkdtemp(suffix=".test", prefix=".tmp.", dir=test_dir)
        cleanup = lambda: shutil.rmtree(setup_dir)
    elif len(argv) > 2:
        usageError()
    else:
        setup_dir = argv[1]
        cleanup  = lambda: ()

    # Not using the fancier temporary file tools because we want the
    # directory to be available if the test fails.

    setupTests(test_dir, script_dir, framework_dir, setup_dir, test_name)
    tools.runInDirectory(setup_dir, lambda: runTests(test_name))

    cleanup()

if __name__ == "__main__":
    main(sys.argv[1:])
