#!/usr/bin/env python

import glob
import os.path
import shutil
import sys

if __name__ == "__main__":
    sys.path.append(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

import tools

def runPc(pcompiler_dir, arguments):
    """
    Compiles p files.

    Args:
        pcompiler_dir (str): The root directory for the P compiler
            repository.

        arguments (str): The arguments for the P compiler arguments.

    Raises:
        SubprocessError if the P compiler returned an error code.
    """
    tools.runNoError(["dotnet", os.path.join(pcompiler_dir, "Bld", "Drops", "Release", "Binaries", "Pc.dll")] + arguments)

def translate(pcompiler_dir, p_spec_dir, gen_monitor_dir):
    """
    Translates a P spec to RVM code.

    Args:
        pcompiler_dir (str): The root directory for the P compiler
            repository.

        p_spec_dir (str): The directory containing the P spec.
            It should contain exactly one .p file.

        gen_monitor_dir (str): The directory in which to place
            the Rvm files.

    Raises:
        SubprocessError if the P compiler returned an error code.

        Exception if the p_spec_dir contained no .p file or more
            than one .p file
    """
    tools.progress("Run the PCompiler...")
    p_spec_paths = glob.glob(os.path.join(p_spec_dir, "*.p"))
    if len(p_spec_paths) != 1:
        raise Exception("Expected a single p spec")
    p_spec_path = p_spec_paths[0]
    runPc(pcompiler_dir, [p_spec_path, "-g:RVM", "-o:%s" % gen_monitor_dir])

def fillAspect(aspectj_dir, gen_monitor_dir):
    """
    Fills the user-defined parts of a generated .aj file.

    The current directory should contain two files:
        * import.txt: should contain the code that replaces the
          "// add your own imports." comment in the .aj file.
        * ajcode.txt: should contain the code that replaces the
          "// Implement your code here." comment in the .aj file.
    
    The fillAspect function replaces the comments mentioned above in
    the .aj file, and copies the result to the destination directory.

    Args:
        aspectj_dir (str): The destination directory for the
            filled .aj file.

        gen_monitor_dir (str): The input directory, which must contain
            a single .aj file

    Raises:
        Exception if the input directory does not contain exactly
            one .aj file.
    """
    tools.progress("Fill in AspectJ template")
    aspect_file_paths = glob.glob(os.path.join(gen_monitor_dir, "*.aj"))
    if len(aspect_file_paths) != 1:
        raise Exception("Expected a single aspectJ template")
    aspect_file_path = aspect_file_paths[0]
    aspectContent = tools.readFile(aspect_file_path)
    aspectContent = aspectContent.replace("// add your own imports.", tools.readFile("import.txt"))
    aspectContent = aspectContent.replace("// Implement your code here.", tools.readFile("ajcode.txt"))
    aspect_file_name = os.path.basename(aspect_file_path)
    tools.writeFile(os.path.join(aspectj_dir, aspect_file_name), aspectContent)

def runMonitor(rvmonitor_bin, gen_monitor_dir, java_dir):
    """
    Translates a .rvm file to Java.

    Args:
        rvmonitor_bin (str): The directory containing the rv-monitor tool.
        
        gen_monitor_dir (str): The directory containing the .rvm file.
            It must contain exactly one .rvm file and it must contain
            an .aj file associated with it.

            This directory is treated as a temporary directory (i.e. the
            Java files are generated here before being copied to the
            java_dir).
        
        java_dir (str): The directory in which to place the generated
            Java files.

    Raises:
        SubprocessError if the rv-monitor tool returned an error.

        Exception if the input directory contains no rvm file or more
            than one.
    """
    tools.progress("Run RVMonitor")
    monitor_binary = os.path.join(rvmonitor_bin, "rv-monitor")
    rvm_file_paths = glob.glob(os.path.join(gen_monitor_dir, "*.rvm"))
    if len(rvm_file_paths) != 1:
        raise Exception("Expected a single rvm spec")
    rvm_file_path = rvm_file_paths[0]
    tools.runNoError([monitor_binary, "-merge", rvm_file_path])
    for f in glob.glob(os.path.join(gen_monitor_dir, "*.java")):
        shutil.copy(f, java_dir)

def build(pcompiler_dir, gen_monitor_dir, rvmonitor_bin, p_spec_dir, aspectj_dir, java_dir):
    """
    Compiles a .p file into an AspectJ instrumentation.

    Produces one aspectJ file and several accompanying Java files.

    Args:
        pcompiler_dir (str): The root directory for the P compiler
            repository.

        gen_monitor_dir (str): Temporary directory in which to generate
            source files.

        rvmonitor_bin (str): The directory containing the rv-monitor tool.

        p_spec_dir (str): Input directory containing the P spec.
            It should contain exactly one .p file.

        aspectj_dir (str): Output directory in which the .aj file
            will be placed.

        java_dir (str): Output directory in which the generated
            Java files will be placed.

    Raises:
        SubprocessError if one of the P compiler and rv-monitor tools
            returns an error code.

        Exception
            * if the p_spec_dir contained no .p file or more
              than one .p file
    """
    translate(pcompiler_dir, p_spec_dir, gen_monitor_dir)
    fillAspect(aspectj_dir, gen_monitor_dir)
    runMonitor(rvmonitor_bin, gen_monitor_dir, java_dir)

def removeAll(pattern):
    """
    Removes files.

    Args:
        pattern (str): The shell pattern for the files to remove.
    """
    for f in glob.glob(pattern):
        os.remove(f)

def main():
    """
    Compiles a .p file into an AspectJ instrumentation.

    Produces one aspectJ file and several accompanying Java files.
    The files are placed into directories as expected by the `pom.xml`
    file in this directory.
    """
    script_dir = os.path.dirname(os.path.abspath(__file__))
    pcompiler_dir = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(script_dir)))))
    rvmonitor_bin = os.path.join(os.path.dirname(os.path.dirname(script_dir)), "ext", "rv-monitor", "target", "release", "rv-monitor", "bin")
    gen_src_dir = os.path.join(script_dir, "target", "generated-sources")
    p_spec_dir = os.path.join(script_dir, "monitor")

    aspectj_dir = os.path.join(gen_src_dir, "aspectJ")
    if not os.path.exists(aspectj_dir):
        os.makedirs(aspectj_dir)

    java_dir = os.path.join(gen_src_dir, "java")
    if not os.path.exists(java_dir):
        os.makedirs(java_dir)

    gen_monitor_dir = os.path.join(p_spec_dir, "generated")
    if not os.path.exists(gen_monitor_dir):
        os.makedirs(gen_monitor_dir)

    try:
        tools.runInDirectory(
            p_spec_dir,
            lambda: build(pcompiler_dir, gen_monitor_dir, rvmonitor_bin, p_spec_dir, aspectj_dir, java_dir))
    except BaseException as e:
        removeAll(os.path.join(aspectj_dir, "*.aj"))
        removeAll(os.path.join(java_dir, "*.java"))
        raise e

if __name__ == "__main__":
    main()
