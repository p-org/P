import matplotlib.pyplot as plt
import sys
from matplotlib.ticker import FormatStrFormatter

projectName=str(sys.argv[1])
inputFile=str(sys.argv[2])
outPrefix=str(sys.argv[3])+"/"+projectName

time = []
coverage = []
execution = []
memory = []
finished = []
remaining = []
depth = []
states = []
distinctStates = []
repetitionRatio = []

timeLimit=0
coverageLimit=100
executionLimit=0
memoryLimit=0
finishedLimit=0
remainingLimit=0
depthLimit=0
statesLimit=0
distinctStatesLimit=0
repetitionRatioLimit=1

stepStatus = False

def read_input():
    global stepStatus
    global time, coverage, execution, memory, finished, remaining, depth, states, distinctStates, repetitionRatio
    global timeLimit, coverageLimit, executionLimit, memoryLimit, finishedLimit, remainingLimit, depthLimit, statesLimit, distinctStatesLimit, repetitionRatioLimit

    time.append(0)
    coverage.append(0)
    execution.append(0)
    memory.append(0)
    finished.append(0)
    remaining.append(0)
    depth.append(0)
    states.append(0)
    distinctStates.append(0)
    repetitionRatio.append(1)

    f = open(inputFile, 'r')
    lines = f.readlines()
    i = 0
    while i < len(lines)-9:
        tVal = -1
        cVal = -1
        eVal = -1
        mVal = -1
        fVal = -1
        rVal = -1
        dVal = -1
        sVal = -1
        dsVal = -1

        line = lines[i].strip()
        i += 1
        if ("Statistics Report" in line):
            break

        if ("Status after" in line):
            if ("Step" in line):
                stepStatus = True
            tVal = line.split()[-2]
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
        if ("Schedules:" in line):
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
        if ("Finished:" in line):
            fVal = line.split()[1]
            line = lines[i].strip()
            i += 1
        else:
            continue
        if ("Remaining:" in line):
            rVal = line.split()[1]
            line = lines[i].strip()
            i += 1
        else:
            continue
        if ("Depth:" in line):
            dVal = line.split()[1]
            line = lines[i].strip()
            i += 1
        else:
            continue
        if ("States:" in line):
            sVal = line.split()[1]
            line = lines[i].strip()
            i += 1
        else:
            continue
        if ("DistinctStates:" in line):
            dsVal = line.split()[1]
            line = lines[i].strip()
            i += 1
        else:
            continue
        assert(tVal != -1)
        assert(cVal != -1)
        assert(eVal != -1)
        assert(mVal != -1)
        assert(fVal != -1)
        assert(rVal != -1)
        assert(dVal != -1)
        assert(sVal != -1)
        assert(dsVal != -1)

        time.append(float(tVal))
        coverage.append(float(cVal))
        execution.append(int(eVal))
        memory.append(float(mVal))
        finished.append(int(fVal))
        remaining.append(int(rVal))
        depth.append(float(dVal))
        states.append(float(sVal))
        distinctStates.append(float(dsVal))
        if (float(dsVal) != 0):
            repetitionRatio.append(float(sVal) / float(dsVal))
        else:
            repetitionRatio.append(1)

    timeLimit = max(time)
    coverageLimit = min(100, max(coverage))
    if (coverageLimit > 5):
        coverageLimit = 100
    executionLimit = max(execution)
    memoryLimit = max(memory)
    finishedLimit = max(finished)
    remainingLimit = max(remaining)
    depthLimit = max(depth)
    statesLimit = max(states)
    distinctStatesLimit = max(distinctStates)
    repetitionRatioLimit = max(repetitionRatio)
    if (timeLimit == 0):
        timeLimit = 1
    if (coverageLimit == 0):
        coverageLimit = 1
    if (executionLimit == 0):
        executionLimit = 1
    if (memoryLimit == 0):
        memoryLimit = 1
    if (finishedLimit == 0):
        finishedLimit = 1
    if (remainingLimit == 0):
        remainingLimit = 1
    if (depthLimit == 0):
        depthLimit = 1
    if (statesLimit == 0):
        statesLimit = 1
    if (distinctStatesLimit == 0):
        distinctStatesLimit = 1
    if (repetitionRatioLimit == 0):
        repetitionRatioLimit = 1

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

    ax.set_ylim(0, 100)
    ax.set_xlim(0, timeLimit)
    set_plot(ax, time, coverage)

    ax.set(xlabel='Time (s)', ylabel='Coverage (%)', title=projectName+": Coverage vs Time")
    ax.grid()
    fig.savefig(outPrefix+"_coverage.png", bbox_inches = "tight")

def coverage_vs_executions():
    fig, ax = plt.subplots()
    ax.plot(execution, coverage, color="green", alpha=0.5, clip_on=False)

    ax.set_ylim(0, coverageLimit)
    ax.set_xlim(0, executionLimit)
    ax.xaxis.set_major_formatter(FormatStrFormatter('%d'))
    set_plot(ax, execution, coverage)

    ax.set(xlabel='#Executions', ylabel='Coverage (%)', title=projectName+": Coverage vs #Executions")
    ax.grid()
    fig.savefig(outPrefix+"_coverage_vs_executions.png", bbox_inches = "tight")

def executions_vs_time():
    fig, ax = plt.subplots()
    ax.plot(time, execution, color="magenta", alpha=0.5, clip_on=False)

    ax.set_ylim(0, executionLimit)
    ax.set_xlim(0, timeLimit)
    ax.yaxis.set_major_formatter(FormatStrFormatter('%d'))
    set_plot(ax, time, execution)

    ax.set(xlabel='Time (s)', ylabel='#Executions', title=projectName+": #Executions vs Time")
    ax.grid()
    fig.savefig(outPrefix+"_executions.png", bbox_inches = "tight")

def memory_vs_time():
    fig, ax = plt.subplots()
    ax.plot(time, memory, color="olive", alpha=0.5, clip_on=False)

    ax.set_ylim(0, memoryLimit)
    ax.set_xlim(0, timeLimit)
    ax.yaxis.set_major_formatter(FormatStrFormatter('%d'))
    set_plot(ax, time, memory)

    ax.set(xlabel='Time (s)', ylabel='Memory (MB)', title=projectName+": Memory (MB) vs Time")
    ax.grid()
    fig.savefig(outPrefix+"_memory.png", bbox_inches = "tight")

def finished_vs_time():
    fig, ax = plt.subplots()
    ax.plot(time, finished, color="olive", alpha=0.5, clip_on=False)

    ax.set_ylim(0, finishedLimit)
    ax.set_xlim(0, timeLimit)
    ax.yaxis.set_major_formatter(FormatStrFormatter('%d'))
    set_plot(ax, time, finished)

    ax.set(xlabel='Time (s)', ylabel='#Finished Tasks', title=projectName+": #Finished Tasks vs Time")
    ax.grid()
    fig.savefig(outPrefix+"_finished.png", bbox_inches = "tight")

def remaining_vs_time():
    fig, ax = plt.subplots()
    ax.plot(time, remaining, color="darkorange", alpha=0.5, clip_on=False)

    ax.set_ylim(0, remainingLimit)
    ax.set_xlim(0, timeLimit)
    ax.yaxis.set_major_formatter(FormatStrFormatter('%d'))
    set_plot(ax, time, remaining)

    ax.set(xlabel='Time (s)', ylabel='#Remaining Tasks', title=projectName+": #Remaining Tasks vs Time")
    ax.grid()
    fig.savefig(outPrefix+"_remaining.png", bbox_inches = "tight")

def depth_vs_time():
    fig, ax = plt.subplots()
    ax.plot(time, depth, color="olive", alpha=0.5, clip_on=False)

    ax.set_ylim(0, depthLimit)
    ax.set_xlim(0, timeLimit)
    ax.yaxis.set_major_formatter(FormatStrFormatter('%d'))
    set_plot(ax, time, depth)
    plt.gca().invert_yaxis()

    ax.set(xlabel='Time (s)', ylabel='Depth', title=projectName+": Depth vs Time")
    ax.grid()
    fig.savefig(outPrefix+"_depth.png", bbox_inches = "tight")

def states_vs_time():
    fig, ax = plt.subplots()
    ax.plot(time, states, color="olive", alpha=0.5, clip_on=False)

    ax.set_ylim(0, statesLimit)
    ax.set_xlim(0, timeLimit)
    ax.yaxis.set_major_formatter(FormatStrFormatter('%d'))
    set_plot(ax, time, states)

    ax.set(xlabel='Time (s)', ylabel='#States', title=projectName+": #States vs Time")
    ax.grid()
    fig.savefig(outPrefix+"_states.png", bbox_inches = "tight")

def distinct_states_vs_time():
    fig, ax = plt.subplots()
    ax.plot(time, distinctStates, color="olive", alpha=0.5, clip_on=False)

    ax.set_ylim(0, distinctStatesLimit)
    ax.set_xlim(0, timeLimit)
    ax.yaxis.set_major_formatter(FormatStrFormatter('%d'))
    set_plot(ax, time, distinctStates)

    ax.set(xlabel='Time (s)', ylabel='#DistinctStates', title=projectName+": #DistinctStates vs Time")
    ax.grid()
    fig.savefig(outPrefix+"_distinct_states.png", bbox_inches = "tight")

def repetition_vs_time():
    fig, ax = plt.subplots()
    ax.plot(time, repetitionRatio, color="olive", alpha=0.5, clip_on=False)

    ax.set_ylim(0, repetitionRatioLimit)
    ax.set_xlim(0, timeLimit)
    ax.yaxis.set_major_formatter(FormatStrFormatter('%.2f'))
    set_plot(ax, time, repetitionRatio)

    ax.set(xlabel='Time (s)', ylabel='#States / #DistinctStates', title=projectName+": Repetition vs Time")
    ax.grid()
    fig.savefig(outPrefix+"_repetition.png", bbox_inches = "tight")

read_input()
coverage_vs_time()
coverage_vs_executions()
executions_vs_time()
memory_vs_time()
finished_vs_time()
remaining_vs_time()
depth_vs_time()
states_vs_time()
distinct_states_vs_time()
repetition_vs_time()
