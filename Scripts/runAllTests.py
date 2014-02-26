#! /usr/bin/python
import os;
import functools;
import fnmatch
from os.path import *;
import ntpath;
import sys;
import shutil;
from re import *;
import argparse;
import generate_project;
from common import *
from subprocess import *

parser = argparse.ArgumentParser(description='Execute the specified P Tests');
parser.add_argument("out", type=str, nargs=1, help="output dir");
parser.add_argument("--fail", action='store_const', dest='fail', const=True, default=False, help="specify this is a failing test");
parser.add_argument("files", type=str, nargs="+", help="the P files to test, or directories with P files to recursively walk and test");

args = parser.parse_args();
out=args.out[0]

scriptDir = getMyDir(); 
baseDir = realpath(join(scriptDir, ".."));
zc=join(baseDir, "Ext", "Zing", "zc");
zinger=join(baseDir, "Ext", "Zing", "Zinger");
pc=join(baseDir, "Src", "Compilers", "PCompiler", "bin", "Debug", "PCompiler");

zingRT=join(baseDir, "Runtime", "Zing", "SMRuntime.zing");
cInclude=join(baseDir, "Runtime", "Include");
cLib=join(baseDir, "Runtime", "Libraries");
pData=join(baseDir, "Src", "Formula", "Domains");
stateCoverage=join(baseDir, "Ext", "Zing", "StateCoveragePlugin.dll");
sched=join(baseDir, "Ext", "Zing", "RandomDelayingScheduler.dll")
cc="MSBuild.exe"

try:
    shutil.rmtree(out);
except OSError: pass;

try:
    os.mkdir(out);
except OSError: pass;

def fmt(s):
    return s.format(**globals());

def cat(f):
    return open(f).read();

def die(s):
    print(s);
    sys.exit(-1);

def find_files(directory, pattern):
    lst = []
    for root, dirs, files in os.walk(directory):
        for basename in files:
            if fnmatch.fnmatch(basename, pattern):
                filename = os.path.join(root, basename)
                lst.append(filename)

    return lst

def elaborateFiles(files):
    return functools.reduce(lambda acc, el:  acc + el, \
        map(lambda f:   find_files(f, "*.p") if isdir(f) else [f], files),\
        []);

nondeterministicallyFailing = [ "BangaloreToRedmond", "WhileNondet", "WhileNondet2" ]

okToExceedMaxInstance = [ "Elevator", "OSR" ]

okToStackOverflow = [ "PingPongWithCall" ]

okToTimeout = [ "TokenRing", "BangaloreToRedmond_Liveness", "PingPong", "PingPongDingDong" ]

okToFail = [ ]

for f in elaborateFiles(args.files):
    name = os.path.splitext(os.path.basename(f))[0]
    print("================= TEST: " + f + "=================");
    pFile = join(out, name + ".p")
    zingFile = "output.zing"
    zingDll = name + ".dll"
    zcOut = join(out, "zc.out")
    trace = join(out, "trace.txt")
    zingerOut = join(out, "zinger.out")
    ccOut = join(out, "compiler.out")
    proj = join(out, name + ".vcxproj")
    binary = join(out, "Debug", name+ ".exe")

    shutil.copy(f, pFile);

    print("Running PCompiler")
    ret = os.system(fmt("{pc} /doNotErase {pFile} {pData} /outputDir:{out}"))

    if ret != 0:
        if (args.fail):
            continue;

        die("PCompiler failed.")

    print("Running zc");
    zcOutFile = open(zcOut, "w");
    shutil.copy(zingRT, join(out, "SMRuntime.zing"));
    ret = check_call([zc, "-nowarning:292", zingFile, "SMRuntime.zing", '/out:' + zingDll], \
        cwd=out, stdout=zcOutFile, stderr=zcOutFile);
    os.remove(join(out, "SMRuntime.zing"));
    zcOutFile.close();
    if not (ret == 0 and zcSucceeded(cat(zcOut))):
        die("Compiling of Zing model failed:\n" + cat(zcOut))

    print("Running Zinger")
    zingerOutFile = open(zingerOut, "w");
    shutil.copy(sched, join(out, 'sched.dll'));
    shutil.copy(stateCoverage, join(out, 'stateCov.dll'));
    ret = check_call([zinger, '-s', '-eo', '-p', '-delayc:100', \
       '-et:trace.txt', '-plugin:stateCov.dll', '-sched:sched.dll', zingDll], \
                     cwd=out, stdout=zingerOutFile, stderr=zingerOutFile);
    zingerOutFile.close();
    if not (ret == 0 and zingerSucceeded(cat(zingerOut))) and not args.fail:
        die("Zingering of Zing model failed:\n" + cat(zingerOut))

    mainM = search("main machine ([\w]*)", \
        open(pFile).read()).groups()[0]

    print(fmt("Main machine is {mainM}"))
    print("Generating VS project...")

    generate_project.generateVSProject(out, name, cInclude, cLib, mainM, False);

    print("Building Generated C...")
    print(proj)
    ret = os.system(fmt("{cc} {proj} > {ccOut}"))

    compilerOut = cat(ccOut)

    if (ret != 0 or not buildSucceeded(compilerOut)):
        die("Failed Building the C code:\n" + compilerOut)

    try:
        check_output([binary])
    except CalledProcessError as err:
        ret = err.returncode;

        if (name in okToFail):
            continue;

        if (ret == 139):
            die("C Binary Segfaulted");

        if (ret == 5):  # TIMEOUT
            if (name in okToTimeout):
                continue;
            die("Binary timed out!");

        if (search("MaxInstance of Event Exceeded Exception", str(err.output)) \
            and name in okToExceedMaxInstance):
            continue;

        if (search("Call Stack Overflow", str(err.output)) \
            and name in okToStackOverflow):
            continue;
        
        if (args.fail):
            if (search("Failed an assertion", str(err.output))):
                continue;
            if (search("Unhandled Event Exception", str(err.output))):
                continue;
            if (search("Call Statement terminated with an unhandled event", str(err.output))):
                continue;

        print(str(err.output))
        die("Binary failed unexpectedly!")

    if (args.fail and name not in nondeterministicallyFailing):
        die("Binary didn't fail when we expected it");

print("ALL TESTS RAN SUCCESSFULLY");
