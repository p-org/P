from re import *;

def parseMSBuildOutput(out):

    buildSucceeded = search('Build succeeded.', out) != None;
    buildFailed = search('Build FAILED.', out) != None;
    assert(buildSucceeded or buildFailed and (not buildSucceeded or not buildFailed))
    sep = 'Build succeeded.' if buildSucceeded else 'Build FAILED.';
    out = out.split(sep)[1]

    lines = out.split('\n')
    linkerErrs = list(filter(lambda l:  search('error LNK[0-9]*', l), lines))
    linkerWarnings = list(filter(lambda l:  search('warning LNK[0-9]*', l), lines))
    warnings = int(search('([0-9]*) Warning\(s\)', out).groups()[0]);
    errors = int(search('([0-9]*) Error\(s\)', out).groups()[0]);
    return (buildSucceeded, errors, warnings, linkerErrs, linkerWarnings)

def buildSucceeded(out):
    result=parseMSBuildOutput(out);
    return result[0] and result[1] == 0 and result[2] == len(result[4]);
