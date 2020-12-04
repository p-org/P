#!/usr/bin/env python

import glob
import os
import os.path
import re
import shutil
import sys
import tempfile
import tools

def runPc(root_dir, arguments):
    """
    Compiles p files.

    Args:
        root_dir (str): The root directory for the P compiler
            repository.

        arguments (str): The arguments for the P compiler arguments.

    Raises:
        SubprocessError if the P compiler returned an error code.
    """
    tools.runNoError(
        ["dotnet", os.path.join(root_dir, "Bld", "Drops", "Release", "Binaries", "netcoreapp3.1", "P.dll")]
        + arguments
    )

def translate(script_dir, root_dir, gen_monitor_setup_dir):
    """
    Translates a `spec.p` file to RVM code.

    Args:
        script_dir (str): Input directory containing the `spec.p` file.

        root_dir (str): The root directory for the P compiler
            repository.

        gen_monitor_setup_dir (str): Output directory that will contain
            the generated files.

    Raises:
        SubprocessError if the P compiler returned an error code.
    """
    tools.progress("Run the PCompiler...")
    runPc(root_dir, [os.path.join(script_dir, "spec.p"), "-g:RVM", "-o:%s" % gen_monitor_setup_dir])

def fillAspect(aspectj_setup_dir, monitor_setup_dir, gen_monitor_setup_dir):
    """
    Fills the user-defined parts of a generated .aj file.

    The file should be called unittestMonitorAspect.aj file.

    Args:
        aspectj_setup_dir (str): The destination directory for the
            filled .aj file.

        monitor_setup_dir (str): Input directory that contains two files:
            * import.txt: should contain the code that replaces the
              "// add your own imports." comment in the .aj file.
            * ajcode.txt: should contain the code that replaces the
              "// Implement your code here." comment in the .aj file.

        gen_monitor_dir (str): The input directory, which must a
            a single unittestMonitorAspect.aj file.
    """
    tools.progress("Fill in AspectJ template")
    aspect_file_name = "unittestMonitorAspect.aj"
    aspect_file_path = os.path.join(gen_monitor_setup_dir, aspect_file_name)
    aspectContent = tools.readFile(aspect_file_path)
    aspectContent = aspectContent.replace(
        "// add your own imports.",
        tools.readFile(os.path.join(monitor_setup_dir, "import.txt")))
    aspectContent = aspectContent.replace(
        "// Implement your code here.",
        tools.readFile(os.path.join(monitor_setup_dir, "ajcode.txt")))
    tools.writeFile(os.path.join(aspectj_setup_dir, aspect_file_name), aspectContent)

def addRvmExceptions(rvm_file_path):
    """
    Changes the getState functions to throw an exception with the state name.

    The input file will be changed in-place.

    The getState function should have the following format:
    private void somename_getState() throws GotoStmtException, RaiseStmtException {
    }

    The resulting getState function will look like this:
    private void somename_getState() throws GotoStmtException, RaiseStmtException {
        throw new StateNameException(state.getName());
    }


    Args:
        rvm_file_path (str): The path to the rvm file.
    """
    rvm = re.sub(
        r"([ ]*)private void ([a-zA-Z_0-9]+)_getState\(\) throws GotoStmtException, RaiseStmtException \{",
        r"""\1private void \2_getState() throws GotoStmtException, RaiseStmtException, StateNameException {
\1    throw new StateNameException(state.getName());""",
        tools.readFile(rvm_file_path)
    )
    tools.writeFile(rvm_file_path, rvm)

def createRvm(rvmonitor_bin, gen_monitor_setup_dir, java_dir):
    """
    Compiles a rvm file to java.

    Args:
        rvmonitor_bin (str): Directory containing the rv-monitor binary.
        
        gen_monitor_setup_dir (str): Input directory containing a file
            called "unittest.rvm". The file will be changed during the
            call.
        
        java_dir: Destination directory for the generated Java files

    Raises:
        SubprocessError if the rv-monitor tool returned an error code.
    """
    tools.progress("Run RVMonitor")
    monitor_binary = os.path.join(rvmonitor_bin, "rv-monitor")
    rvm_file = os.path.join(gen_monitor_setup_dir, "unittest.rvm")
    addRvmExceptions(rvm_file)
    tools.runNoError([monitor_binary, "-merge", "-d", java_dir, rvm_file])

def setupTests(test_dir, root_dir, rvmonitor_bin_dir, framework_dir, setup_dir, test_name):
    """
    Full setup for a RVM unit-test.

    Args:
        test_dir (str): Input directory containing the test.
            It should contain a P spec and one or more Java test
            files using that spec.

        root_dir (str): The root directory for the P compiler
            repository.

        rvmonitor_bin_dir (str): Directory containing the rv-monitor
            binary.

        framework_dir (str): Directory containing the test framework files.

        setup_dir (str): Output directory that will contain the test
            setup, ready for testing with Maven.

        test_name (str): The name of the test, used for user-friendly
            messages.

    Raises:
        SubprocessError if one of the tools used (the P compiler and
            rv-monitor) returns an error code.
    """
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

    gen_monitor_setup_dir = os.path.join(monitor_setup_dir, "generated")
    if not os.path.exists(gen_monitor_setup_dir):
        os.makedirs(gen_monitor_setup_dir)
    translate(test_dir, root_dir, gen_monitor_setup_dir)

    mop_setup_dir = os.path.join(src_setup_dir, "main", "java", "mop")
    aspectj_setup_dir = os.path.join(mop_setup_dir, "aspectJ")
    if not os.path.exists(aspectj_setup_dir):
        os.makedirs(aspectj_setup_dir)
    fillAspect(aspectj_setup_dir, monitor_setup_dir, gen_monitor_setup_dir)

    createRvm(rvmonitor_bin_dir, gen_monitor_setup_dir, mop_setup_dir)

    for f in glob.glob(os.path.join(gen_monitor_setup_dir, "*.java")):
        shutil.copy(f, mop_setup_dir)

def runTests(test_name):
    """
    Uses Maven to run a Rvm unit-test.

    Args:
        test_name (str): The name of the test, used for user-friendly
            messages.

    Raises:
        SubprocessError if mvn returns an error code.
    """
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
    """
    Runs a Rvm unit-test.

    Args:
        argv (list of str): see the "usageError" function.
    """
    if len(argv) == 0:
        usageError()
    test_name = argv[0]
    script_dir = os.path.dirname(os.path.abspath(__file__))
    unittest_dir = os.path.join(script_dir, "Unit")
    framework_dir = os.path.join(unittest_dir, "Framework")
    test_dir = os.path.join(unittest_dir, "Test", test_name)
    rvmonitor_bin_dir = os.path.join(script_dir, "ext", "rv-monitor", "target", "release", "rv-monitor", "bin")
    root_dir = os.path.dirname(os.path.dirname(os.path.dirname(script_dir)))

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

    setupTests(test_dir, root_dir, rvmonitor_bin_dir, framework_dir, setup_dir, test_name)
    tools.runInDirectory(setup_dir, lambda: runTests(test_name))

    cleanup()

if __name__ == "__main__":
    main(sys.argv[1:])
