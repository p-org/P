import os
import argparse
import multiprocessing
from constants import num_traces, benchmarks

configurations = {
    '2PC': ['tcC2P3T3', 'tcC3P4T3', 'tcC3P5T3', 'tcSingleClientNoFailure'],
    'ClockBound': ['tcC1R3', 'tcC2R3', 'tcC3R3', 'tcC4R3', 'tcC3R5'],
    'consensus_epr': ['tcOneNode', 'tcTwoNodes', 'tcThreeNodes', 'tcFiveNodes', 'tcTenNodes'],
    'paxos': ['testBasicPaxos3on5', 'testBasicPaxos3on3', 'testBasicPaxos3on1', 'testBasicPaxos2on3', 'testBasicPaxos2on2'],
    'Raft': ['oneClientFiveServersReliable', 'oneClientFiveServersUnreliable', 'twoClientsThreeServersReliable', 'twoClientsThreeServersUnreliable'],
    'ring_leader': ['tcOneNode', 'tcTwoNodes', 'tcThreeNodes', 'tcFiveNodes', 'tcTenNodes']
}

event_combs = {
    'Raft': [
        ('eRequestVote', 'eRequestVoteReply'),
        ('eClientPutRequest', 'eRaftGetResponse', 'eRaftPutResponse', 'eClientGetRequest'),
        ('eAppendEntries', 'eClientGetRequest', 'eClientPutRequest'),
        ('eEntryApplied', 'eAppendEntries', 'eRaftPutResponse'),
        ('eNotifyLog',),
        ('eEntryApplied', 'eRequestVote', 'eClientPutRequest', 'eRequestVoteReply'),
        ('eAppendEntriesReply', 'eClientGetRequest'),
        ('eBecomeLeader', 'eRequestVoteReply'),
        ('eClientGetRequest', 'eClientPutRequest', 'eRequestVoteReply')
    ]
}

def generate_traces(name: str, trace_dir: str, n_traces: list):
    configs = configurations[name]
    os.chdir(name)
    os.system('p compile')
    for n in n_traces:
        num_schedules = n // len(configs)
        dest_dir = os.path.join(trace_dir, name, str(n))
        if os.path.exists(dest_dir):
            print(f'{dest_dir} already exists. Skipping...')
            continue
        os.makedirs(dest_dir, exist_ok=True)
        for config in configs:
            if name in event_combs:
                for combo in event_combs[name]:
                    os.system(f'p check --pinfer -tc {config} -ef {" ".join(combo)} -s {num_schedules} -tf {dest_dir}')
            else:
                os.system(f'p check --pinfer -tc {config} -s {num_schedules} -tf {dest_dir}')
    os.chdir('..')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--trace_dir', type=str, default=os.environ['PINFER_TRACE_DIR'])
    parser.add_argument('--benchmarks', type=str, nargs='+', default=benchmarks)
    parser.add_argument('--num_traces', type=int, nargs='+', default=num_traces)
    args = parser.parse_args()
    print(f'[Step 1] Generating traces for benchmarks: {args.benchmarks}')
    pool = multiprocessing.Pool(processes=len(args.benchmarks))
    pool.starmap(generate_traces, [(name, args.trace_dir, args.num_traces) for name in args.benchmarks])