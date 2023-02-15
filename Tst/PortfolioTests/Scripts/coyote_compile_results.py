#!/usr/bin/env python

import os, sys
import subprocess
import argparse
import tempfile
import shutil
import ntpath
from distutils import spawn
import re
from distutils.spawn import find_executable

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

def get_values(statF):
	with open(statF) as f:
		for line in f.readlines()[0:]:
			entry = line.split(':', 1)
			if len(entry) >= 1:
				lhs = entry[0]+":"
				table[lhs] = "-1"
				assert (len(entry) == 2)
				rhs = entry[1].lstrip().rstrip()
				rhs = rhs.replace(" ", "_").replace(",", "|")
				table[lhs] = rhs

def print_keys(outF):
	for key in keys:
		outF.write("%s," % key)
	outF.write("\n")

def print_values(outF):
	for key in keys:
		outF.write("%s," % table[key])
	outF.write("\n")


DEFAULT_TOOL="pmc"
DEFAULT_OUT="stats"
DEFAULT_SEED="1"

def getopts(header):
	p = argparse.ArgumentParser(description=str(header), formatter_class=argparse.RawDescriptionHelpFormatter)
	p.add_argument('file', help='input file name', type=str)
	p.add_argument('-t', '--tool', help='tool name (default: %s)' % DEFAULT_TOOL, type=str, default=DEFAULT_TOOL)
	p.add_argument('--seed', help='solver seed (default: %r)' % DEFAULT_SEED, type=int, default=DEFAULT_SEED)
	args, leftovers = p.parse_known_args()
	return args, p.parse_args()

def main():
	known, opts = getopts("")
	top_dir = "output"

	stat_file = opts.file
	if not os.path.isfile(stat_file):
		raise Exception("Unable to find stats file %s" % stat_file)

	set_keys()

	outName = "%s/%s.csv" % (DEFAULT_OUT, opts.tool)
	outF = None
	if os.path.isfile(outName):
		outF = open(outName, "a")
	else:
		outF = open(outName, "x")
		print_keys(outF)
	
	reset_values()
	get_values(stat_file)
	print_values(outF)	
	outF.close()

if __name__ == '__main__':
	main()
