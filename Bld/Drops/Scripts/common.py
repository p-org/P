from re import *;
from os.path import *;
from subprocess import *
import platform

ignoredCompWarnings = [ 4244, 4018 ]

def parseMSBuildOutput(out):

    def getFlags(pattern, lines):
        matches = filter(lambda p: p[0] != None,
            map(lambda l:   (search(pattern, l), l), lines))
        return list(map(lambda p:    (int(p[0].groups()[0]), p[1]), matches))

    buildSucceeded = search('Build succeeded.', out) != None;
    buildFailed = search('Build FAILED.', out) != None;
    assert(buildSucceeded or buildFailed and (not buildSucceeded or not buildFailed))
    sep = 'Build succeeded.' if buildSucceeded else 'Build FAILED.';
    out = out.split(sep)[1]

    lines = out.split('\n')
    linkerErrs = getFlags('error LNK([0-9]*)', lines)
    linkerWarnings = getFlags('warning LNK([0-9]*)', lines)
    compilerErrors = getFlags('error C([0-9]*)', lines)
    compilerWarnings = getFlags('warning C([0-9]*)', lines)

    warnings = int(search('([0-9]*) Warning\(s\)', out).groups()[0]);
    errors = int(search('([0-9]*) Error\(s\)', out).groups()[0]);
    return (buildSucceeded, errors, warnings, linkerErrs, linkerWarnings, compilerErrors, compilerWarnings)

def buildSucceeded(out):
    result=parseMSBuildOutput(out);
    signCmpWarnings = len(list(filter(lambda p:  p[0] in ignoredCompWarnings, result[6])))
    return result[0] and result[1] == 0 and result[2] == (len(result[4]) + signCmpWarnings)

def zingerSucceeded(out):
    return search('Check passed', out) != None

def zcSucceeded(out):
    return search('error', out) == None

def getMyDir():
    return dirname(realpath(__file__));

def get_output(*args, **kwargs):
    outp = str(check_output(*args, **kwargs))
    if (platform.system() == 'Linux'):
        return outp;
    else:
        return '\n'.join(outp.split('\\r\\n'))
