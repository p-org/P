import matplotlib as mpl
mpl.use('Agg')
import matplotlib.pyplot as plt
from brokenaxes import brokenaxes
import os
import json

benchmarks = ['ClockBound', 'paxos', 'Raft', 'ring_leader']
name_dict = {
    'ClockBound': 'ClockBound',
    'paxos': 'Paxos',
    'Raft': 'Raft',
    'ring_leader': 'Ring Leader'
}
num_traces = [500, 1000, 2000, 4000, 8000, 10000, 20000, 30000, 40000, 50000]
xticks = ['500', '1K', '2K', '4K', '8K', '10K', '20K', '30K', '40K', '50K']
marker = ['o', 'v', '*', 's', 'x']

NUM_INV_TOTAL = 'NumInvsTotal'
NUM_PRUNED_BY_SUBSUME = 'NumInvsPrunedBySubsumption'
NUM_PRUNED_BY_GRAMMAR = 'NumInvsPrunedByGrammar'
NUM_PRUNED_BY_SYMM = 'NumInvsPrunedBySymmetry'
NUM_PRUNED_BY_SANITIZATION = 'NumInvsPrunedBySanitizing'

def draw_plot(normalised=False):
    pref = 'pruned_stats'
    y_axis = {}
    num_invs = {}
    num_total_guards = {}
    for benchmark in benchmarks:
        name = name_dict[benchmark]
        y_axis[name] = []
        num_invs[name] = []
        num_total_guards[name] = []
        for num_trace in num_traces:
            filename = os.path.join(benchmark, f'{pref}_{num_trace}.json')
            if not os.path.exists(filename):
                y_axis[name].append(None)
                num_invs[name].append(None)
                continue
            with open(filename, 'r') as f:
                data = json.load(f)
                y_axis[name].append(data['NumActivatedGuards'] / data['NumAllGuards'] * 100.0 if normalised else data['NumActivatedGuards'])
                num_invs[name].append(data[NUM_INV_TOTAL] - data[NUM_PRUNED_BY_SANITIZATION] - data[NUM_PRUNED_BY_GRAMMAR])
                if num_trace == num_traces[-1]:
                    num_total_guards[name] = data['NumAllGuards']

    print(num_invs)
    plt.figure(figsize=(7, 5))
    if normalised:
        baxes = brokenaxes(ylims=((0, 100),), hspace=.15)
    else:
        baxes = brokenaxes(ylims=((0, 20), (120, 250)), hspace=.15)
    # Plot each line with a different color
    it = iter(marker)
    for label, values in y_axis.items():
        baxes.plot(num_traces, values, label=label, marker=next(it))
        if not normalised:
            prevY = None
            for x, y in zip(num_traces, values):
                if y is not None and prevY != y:
                    if y != values[-1]:
                        baxes.annotate(str(y), (x, y), textcoords="offset points", xytext=(0, 10), ha='center', fontsize=10)
                    else:
                        baxes.annotate(f"{y}/{num_total_guards[label]}", (x, y), textcoords="offset points", xytext=(5, 10), ha='center', fontsize=10)
                    prevY = y

    baxes.set_xscale('log')
    # baxes.grid(axis='y', which='major', ls='-')
    baxes.set_xticks(num_traces)
    for ax in baxes.axs:
        ax.set_xticks(num_traces)
        ax.set_xticklabels(xticks, rotation=30)

    # Add labels and title
    baxes.set_xlabel("Number of Traces", labelpad=35, fontsize=13)
    baxes.set_ylabel("Number of activated guards", fontsize=13, labelpad=30)
    baxes.set_title("Number of activated guards vs. number of traces", fontsize=14)
    # baxes.tick_params(axis='y', which='major', labelsize=12)

    # Add legends at the bottom of the figure
    baxes.legend(
        fontsize=12,
        bbox_to_anchor=(0.5, 1.05),  # Position the legend above the plot
        loc='lower center',          # Anchor point for the legend
        borderaxespad=0.5,           # Padding around the legend
        ncol=len(y_axis)             # Number of columns in the legend
    )
    # plt.subplots_adjust(bottom=0.3)
    # plt.tight_layout()
    plt.savefig('num_activated_guards.pdf')
    draw_num_invs(num_invs)

def draw_num_invs(num_invs):
    plt.clf()

    plt.figure(figsize=(7, 5))
    baxes = brokenaxes(ylims=((0, 100), (200, 700)), hspace=.15)
    it = iter(marker)
    for label, values in num_invs.items():
        baxes.plot(num_traces, values, label=label, marker=next(it))
        prevY = None
        for x, y in zip(num_traces, values):
            if y is not None and prevY != y:
                baxes.annotate(str(y), (x, y), textcoords="offset points", xytext=(0, 5), ha='center', fontsize=10)
                prevY = y
    # baxes.set_xscale('log')
    # baxes.grid(axis='y', which='major', ls='-')
    baxes.set_xticks(num_traces)
    for ax in baxes.axs:
        ax.set_xticks(num_traces)
        ax.set_xticklabels(xticks, rotation=30)

    baxes.set_xscale('log')
    # baxes.grid(axis='y', which='major', ls='-')
    # baxes.grid(axis='x', which='minor', ls='--')
    baxes.set_xticks(num_traces)
    for ax in baxes.axs:
        ax.set_xticks(num_traces)
        ax.set_xticklabels(xticks, rotation=30)

    # Add labels and title
    baxes.set_xlabel("Number of Traces", labelpad=35, fontsize=13)
    baxes.set_ylabel("Number of Mined Invariants", fontsize=13)
    baxes.set_title("Number of Mined Invariants vs. number of traces", fontsize=14)
    # baxes.tick_params(axis='y', which='major', pad=15)

    # Add legends at the bottom of the figure
    baxes.legend(
        fontsize=12,
        bbox_to_anchor=(0.5, 1.05),  # Position the legend above the plot
        loc='lower center',          # Anchor point for the legend
        borderaxespad=0.5,           # Padding around the legend
        ncol=len(num_invs)             # Number of columns in the legend
    )
    # plt.subplots_adjust(bottom=0.3)
    # plt.tight_layout()
    plt.savefig('num_invs.pdf')

if __name__ == '__main__':
    draw_plot()