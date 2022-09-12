import matplotlib.pyplot as plt
import numpy as np
import sys
from decimal import Decimal
from matplotlib.ticker import FormatStrFormatter

projectName=str(sys.argv[1])
inputFile=str(sys.argv[2])
outPrefix=str(sys.argv[3])+"/"+projectName

time = []
coverage = []
execution = []
memory = []

timeLimit=0
coverageLimit=100
executionLimit=0
memoryLimit=0

def read_input():
	global time, coverage, execution, memory
	global timeLimit, coverageLimit, executionLimit, memoryLimit
	
	time.append(0);
	coverage.append(0);
	execution.append(0);
	memory.append(0);

	f = open(inputFile, 'r')
	lines = f.readlines()
	i = 0
	while i < len(lines)-4:
		tVal = -1
		cVal = -1
		eVal = -1
		mVal = -1

		line = lines[i].strip()
		i += 1
		if ("Statistics Report" in line):
			break

		if ("Status after" in line):
			tVal = line.split()[2]
			line = lines[i].strip()
			i += 1
		else:
			continue
		if ("Coverage:" in line):
			cVal = line.split()[1]
			line = lines[i].strip()
			i += 1
		else:
			continue
		if ("Executions:" in line):
			eVal = line.split()[1]
			line = lines[i].strip()
			i += 1
		else:
			continue
		if ("Memory:" in line):
			mVal = line.split()[1]
			line = lines[i].strip()
			i += 1
		else:
			continue
		assert(tVal != -1)
		assert(cVal != -1)
		assert(eVal != -1)
		assert(mVal != -1)

		time.append(float(tVal))
		coverage.append(float(cVal))
		execution.append(int(eVal))
		memory.append(float(mVal))

	timeLimit = max(time)
	coverageLimit = min(100, max(coverage))
	executionLimit = max(execution)
	memoryLimit = max(memory)
	if (timeLimit == 0):
		timeLimit = 1
	if (coverageLimit == 0):
		coverageLimit = 1
	if (executionLimit == 0):
		executionLimit = 1
	if (memoryLimit == 0):
		memoryLimit = 1

def set_plot(ax, x, y):
	xLast = max(x)
	yLast = max(y)
	if (xLast != 0):
		plt.figtext(0.01, 1.001, "y-max: "+str(yLast), ha="left")
	if (yLast != 0):
		plt.figtext(1.001, 0.01, "x-max: "+str(xLast), ha="right")


def coverage_vs_time():
	fig, ax = plt.subplots()
	ax.plot(time, coverage, color="blue", alpha=0.5, clip_on=False)

	ax.set_ylim(0, coverageLimit);
	ax.set_xlim(0, timeLimit);
	set_plot(ax, time, coverage)

	ax.set(xlabel='Time (s)', ylabel='Coverage (%)', title=projectName+": Coverage vs Time")
	ax.grid()
	fig.savefig(outPrefix+"_coverage.png", bbox_inches = "tight")

def coverage_vs_executions():
	fig, ax = plt.subplots()
	ax.plot(execution, coverage, color="green", alpha=0.5, clip_on=False)

	ax.set_ylim(0, coverageLimit);
	ax.set_xlim(0, executionLimit);
	ax.xaxis.set_major_formatter(FormatStrFormatter('%d'))
	set_plot(ax, execution, coverage)

	ax.set(xlabel='#Executions', ylabel='Coverage (%)', title=projectName+": Coverage vs #Executions")
	ax.grid()
	fig.savefig(outPrefix+"_coverage_vs_executions.png", bbox_inches = "tight")

def executions_vs_time():
	fig, ax = plt.subplots()
	ax.plot(time, execution, color="magenta", alpha=0.5, clip_on=False)

	ax.set_ylim(0, executionLimit);
	ax.set_xlim(0, timeLimit);
	ax.yaxis.set_major_formatter(FormatStrFormatter('%d'))
	set_plot(ax, time, execution)

	ax.set(xlabel='Time (s)', ylabel='#Executions', title=projectName+": #Executions vs Time")
	ax.grid()
	fig.savefig(outPrefix+"_executions.png", bbox_inches = "tight")

def memory_vs_time():
	fig, ax = plt.subplots()
	ax.plot(time, memory, color="olive", alpha=0.5, clip_on=False)

	ax.set_ylim(0, memoryLimit);
	ax.set_xlim(0, timeLimit);
	ax.yaxis.set_major_formatter(FormatStrFormatter('%d'))
	set_plot(ax, time, memory)

	ax.set(xlabel='Time (s)', ylabel='Memory (MB)', title=projectName+": Memory (MB) vs Time")
	ax.grid()
	fig.savefig(outPrefix+"_memory.png", bbox_inches = "tight")

read_input()
coverage_vs_time()
coverage_vs_executions()
executions_vs_time()
memory_vs_time()
