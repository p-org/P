import os, sys
import argparse

projectName=str(sys.argv[1])
inputFile=str(sys.argv[2])
if not os.path.isfile(inputFile):
    raise Exception("Unable to find input file %s" % inputFile)

keys = []
table = {}

def set_keys():
    keys.append("project-name:")
    keys.append("mode:")
    keys.append("status:")
    keys.append("time-seconds:")
    keys.append("max-depth-explored:")
    keys.append("#-executions:")

def reset_values():
    for key in keys:
        table[key] = "-1"
    table["project-name:"] = projectName

def get_values(statF):
    with open(statF) as f:
        for line in f.readlines()[0:]:
            entry = line.split(' ')

            if "strategy" in line:
                assert (len(entry) > 5)
                table["mode:"] = entry[5].lstrip("'").rstrip("'")
            elif "Found 0 bugs." in line:
                table["status:"] = "timeout"
            elif "found a bug." in line:
                table["status:"] = "cex"
            elif "Elapsed" in line:
                assert (len(entry) == 4)
                table["time-seconds:"] = entry[2]
            elif "Number of scheduling points" in line:
                assert (len(entry) > 12)
                table["max-depth-explored:"] = entry[-2]
            elif "Explored" in line:
                assert (len(entry) >= 3)
                table["#-executions:"] = entry[2]

def print_keys():
    for key in keys:
        print("%s," % key)

def print_values():
    for key in keys:
        print('{:40s}'.format(key), end="")
        print("%s" % table[key])

set_keys()
reset_values()
get_values(inputFile)
print_values()
