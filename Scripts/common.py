from re import *;
from os.path import *;

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
    signCmpWarnings = len(list(filter(lambda p:  p[0] == 4018, result[6])))
    return result[0] and result[1] == 0 and result[2] == (len(result[4]) + signCmpWarnings)

def getMyDir():
    return dirname(realpath(__file__));
