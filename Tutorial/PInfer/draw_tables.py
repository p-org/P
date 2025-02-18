import os
import json
import subprocess
from tabulate import tabulate

benchmarks = ['ring_leader', 'consensus', '2PC', 'sharded_kv', 'paxos', 'paxos_hint', 'distributed_lock', 'Raft', 'vertical_paxos', 'ChainReplication', 'lockserver', 'ClockBound']
# benchmarks = ['consensus']
# merge with the following
merge = {'Raft': 'Raft_hint'}
no_smt = {}

NumInvsTotal = 'NumInvsTotal'
NumInvsPrunedBySubsumption = 'NumInvsPrunedBySubsumption'
NumInvsPrunedByTauto = 'NumInvsPrunedByTauto'
NumInvsPrunedByGrammar = 'NumInvsPrunedByGrammar'
NumInvsPrunedBySymmetry = 'NumInvsPrunedBySymmetry'
NumInvsPrunedBySanitizing = 'NumInvsPrunedBySanitizing'
TimeElapsed = 'TimeElapsed'
TimeMining = 'TimeMining'
TimePruning = 'TimePruning'
TimeSearchEventCombination = 'TimeSearchEventCombination'
TimeCandidateTemplateGen = 'TimeCandidateTemplateGen'
NumGoalsLearnedWithHints = 'NumGoalsLearnedWithHints'
NumGoalsLearnedWithoutHints = 'NumGoalsLearnedWithoutHints'
NumGoals = 'NumGoals'
NumDaikonInvocations = 'NumDaikonInvocations'
NumEventCombinations = 'NumEventCombinations'
NumActivatedGuards = 'NumActivatedGuards'
NumAllGuards = 'NumAllGuards'


def get_pruning_stats(benchmark):
    stats = {}
    if os.path.exists(benchmark):
        os.chdir(benchmark)
        if os.path.exists('PInferOutputs'):
            files = os.listdir('PInferOutputs')
            files.sort()
            latest = files[-1]
            if os.path.isdir(os.path.join('PInferOutputs', latest)):
                # syntactic pruning
                _ = subprocess.run(['p', 'infer', '--action', 'pruning', '-pi', os.path.join('PInferOutputs', latest)])
                stats['syn'] = json.load(open('pruned_stats.json', 'r'))
                # smt-based pruning
                if benchmark not in no_smt:
                    process = subprocess.run(['p', 'infer', '--action', 'pruning', '-pi', os.path.join('PInferOutputs', latest), '-z3'])
                    # if process.returncode == 0:
                    stats['smt'] = json.load(open('pruned_stats.json', 'r'))
                os.chdir('..')
                return stats
    os.chdir('..')
    return None

def load_data():
    stats = {}
    for benchmark in benchmarks:
        stats[benchmark] = {}

def merge_stats(lhs, rhs):
    lhs[NumInvsTotal] += rhs[NumInvsTotal]
    lhs[NumInvsPrunedBySubsumption] += rhs[NumInvsPrunedBySubsumption]
    lhs[NumInvsPrunedByGrammar] += rhs[NumInvsPrunedByGrammar]
    lhs[NumInvsPrunedByTauto] += rhs[NumInvsPrunedByTauto]
    lhs[NumInvsPrunedBySymmetry] += rhs[NumInvsPrunedBySymmetry]
    lhs[NumInvsPrunedBySanitizing] += rhs[NumInvsPrunedBySanitizing]
    lhs[TimeElapsed] += rhs[TimeElapsed]
    lhs[TimeMining] += rhs[TimeMining]
    lhs[TimePruning] += rhs[TimePruning]
    lhs[TimeSearchEventCombination] += rhs[TimeSearchEventCombination]
    lhs[TimeCandidateTemplateGen] += rhs[TimeCandidateTemplateGen]
    lhs[NumGoalsLearnedWithHints] += rhs[NumGoalsLearnedWithHints]
    lhs[NumGoalsLearnedWithoutHints] += rhs[NumGoalsLearnedWithoutHints]
    lhs[NumGoals] += rhs[NumGoals]
    lhs[NumDaikonInvocations] += rhs[NumDaikonInvocations]
    lhs[NumEventCombinations] += rhs[NumEventCombinations]
    lhs[NumActivatedGuards] += rhs[NumActivatedGuards]
    lhs[NumAllGuards] += rhs[NumAllGuards]
    return lhs

def draw_pruning_steps():
    data = {}
    for benchmark in benchmarks:
        stats = get_pruning_stats(benchmark)
        if stats is None:
            print(f'{benchmark} not found')
            continue
        if benchmark in merge:
            to_merge = get_pruning_stats(merge[benchmark])
            hinted = f'{benchmark}_hint'
            hinted_stats = {}
            hinted_stats['syn'] = stats['syn'].copy()
            hinted_stats['syn'] = merge_stats(hinted_stats['syn'], to_merge['syn'])
            hinted_stats['smt'] = stats['smt'].copy()
            hinted_stats['smt'] = merge_stats(hinted_stats['smt'], to_merge['smt'])
            data[hinted] = hinted_stats
        data[benchmark] = stats

    headers = ['Benchmark', '#Total Invs', 'Sanitization', 'Grammar', 'Tautology (Syn/SMT)', 'Subsumption (Syn/SMT)', 'Symmetry (Syn/SMT)', '#Remaining (Syn/SMT)', 'SMT Time (ms)']
    table = []

    for benchmark in data:
        stats = data[benchmark]
        entry = [benchmark]
        # print(f"=================={benchmark}==================")
        # print(f"Num. Invs Total: {stats['syn'][NumInvsTotal]}")
        entry.append(stats['syn'][NumInvsTotal])
        # print(f"Num. Pruned by Sanitization: {stats['syn'][NumInvsPrunedBySanitizing]}")
        entry.append(stats['syn'][NumInvsPrunedBySanitizing])
        # print(f"Num. Pruned by Grammar: {stats['syn'][NumInvsPrunedByGrammar]}")
        entry.append(stats['syn'][NumInvsPrunedByGrammar])
        # print(f"Num. Pruned by Tautology (Syn/SMT): {stats['syn'][NumInvsPrunedByTauto]}/{stats['smt'][NumInvsPrunedByTauto] if 'smt' in stats else '[N/A]'}")
        entry.append(f"{stats['syn'][NumInvsPrunedByTauto]}/{stats['smt'][NumInvsPrunedByTauto] if 'smt' in stats else '[N/A]'}")
        # print(f"Num. Pruned by Subsumption (Syn/SMT): {stats['syn'][NumInvsPrunedBySubsumption]}/{stats['smt'][NumInvsPrunedBySubsumption] if 'smt' in stats else '[N/A]'}")
        entry.append(f"{stats['syn'][NumInvsPrunedBySubsumption]}/{stats['smt'][NumInvsPrunedBySubsumption] if 'smt' in stats else '[N/A]'}")
        # print(f"Num. Pruned by Symmetry (Syn/SMT): {stats['syn'][NumInvsPrunedBySymmetry]}/{stats['smt'][NumInvsPrunedBySymmetry] if 'smt' in stats else '[N/A]'}")
        entry.append(f"{stats['syn'][NumInvsPrunedBySymmetry]}/{stats['smt'][NumInvsPrunedBySymmetry] if 'smt' in stats else '[N/A]'}")
        numRemainingSyn = stats['syn'][NumInvsTotal] - stats['syn'][NumInvsPrunedBySanitizing] - stats['syn'][NumInvsPrunedByGrammar] - stats['syn'][NumInvsPrunedByTauto] - stats['syn'][NumInvsPrunedBySubsumption] - stats['syn'][NumInvsPrunedBySymmetry]
        if 'smt' in stats:
            numRemainingSMT = stats['smt'][NumInvsTotal] - stats['smt'][NumInvsPrunedBySanitizing] - stats['smt'][NumInvsPrunedByGrammar] - stats['smt'][NumInvsPrunedByTauto] - stats['smt'][NumInvsPrunedBySubsumption] - stats['smt'][NumInvsPrunedBySymmetry]
        else:
            numRemainingSMT = '[N/A]'
        # print(f"Num. Remaining Invs (Syn/SMT): {numRemainingSyn}/{numRemainingSMT}")
        entry.append(f"{numRemainingSyn}/{numRemainingSMT}")
        # print(f"SMT Time: {stats['smt'][TimePruning] if 'smt' in stats else '[N/A]'}")
        entry.append(stats['smt'][TimePruning] if 'smt' in stats else '[N/A]')
        table.append(entry)
    print(tabulate(table, headers=headers, tablefmt='grid'))

if __name__ == '__main__':
    draw_pruning_steps()
